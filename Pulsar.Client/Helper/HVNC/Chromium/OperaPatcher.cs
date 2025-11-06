using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pulsar.Client.Helper.HVNC.Chromium
{
    /// <summary>
    /// Opera memory patcher that patches GetCursorInfo function to always return success
    /// This prevents Opera from detecting HVNC environments by bypassing cursor detection
    /// 
    /// Usage:
    /// - Automatic: The patcher is automatically called when using ProcessController.StartOpera() or StartOperaGX()
    /// - Manual: Call OperaPatcher.PatchOperaProcesses() to patch all running Opera processes
    /// - Async: Use OperaPatcher.PatchOperaAsync() for non-blocking patching with retry logic
    /// 
    /// How it works:
    /// 1. Finds all running Opera processes with main windows
    /// 2. Locates the GetCursorInfo function in user32.dll within each process
    /// 3. Patches the function to return 1 (success) immediately using assembly: mov eax, 1; ret
    /// 4. This bypasses Opera's cursor detection used to identify HVNC environments
    /// 
    /// The patch is applied in memory and does not modify files on disk.
    /// </summary>
    public class OperaPatcher
    {
        #region Win32 API Imports

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll")]
        private static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, ref int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetLastError();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint dwSize, out int lpNumberOfBytesRead);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool EnumProcessModules(
            IntPtr hProcess,
            [Out] IntPtr[] lphModule,
            int cb,
            out int lpcbNeeded);

        [DllImport("psapi.dll")]
        private static extern uint GetModuleFileNameEx(
            IntPtr hProcess,
            IntPtr hModule,
            [Out] StringBuilder lpBaseName,
            int nSize);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        #endregion Win32 API Imports

        #region Structs and Constants

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            public LUID Luid;
            public uint Attributes;
        }

        private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const uint TOKEN_QUERY = 0x0008;
        private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        private const uint PROCESS_ALL_ACCESS = 0x001F0FFF;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;

        #endregion Structs and Constants

        /// <summary>
        /// Patches Opera processes to bypass HVNC detection
        /// </summary>
        /// <returns>True if at least one Opera process was successfully patched</returns>
        public static bool PatchOperaProcesses()
        {
            bool anyPatched = false;

            try
            {
                // Find all Opera processes
                Process[] operaProcesses = Process.GetProcessesByName("opera")
                    .Where(p => p.MainWindowHandle != IntPtr.Zero)
                    .ToArray();

                if (operaProcesses.Length == 0)
                {
                    Debug.WriteLine("No Opera processes found with main window");
                    return false;
                }

                foreach (var process in operaProcesses)
                {
                    try
                    {
                        Debug.WriteLine($"Attempting to patch Opera process PID: {process.Id}");
                        
                        if (EnableDebugPrivilege(process.Id))
                        {
                            Debug.WriteLine("Debug privilege enabled successfully");
                        }
                        else
                        {
                            Debug.WriteLine("Failed to enable debug privilege, continuing anyway");
                        }

                        if (PatchOperaProcess(process.Id))
                        {
                            Debug.WriteLine($"Successfully patched Opera PID: {process.Id}");
                            anyPatched = true;
                        }
                        else
                        {
                            Debug.WriteLine($"Failed to patch Opera PID: {process.Id}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error patching Opera process {process.Id}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in PatchOperaProcesses: {ex.Message}");
            }

            return anyPatched;
        }

        /// <summary>
        /// Patches a specific Opera process by PID
        /// </summary>
        /// <param name="pid">Process ID of the Opera process to patch</param>
        /// <returns>True if the process was successfully patched</returns>
        public static bool PatchOperaProcess(int pid)
        {
            try
            {
                IntPtr addr = RemoteGetProcAddress(pid, "user32.dll", "GetCursorInfo");
                if (addr == IntPtr.Zero)
                {
                    Debug.WriteLine("Failed to find GetCursorInfo function address");
                    return false;
                }

                Debug.WriteLine($"GetCursorInfo address: 0x{addr.ToInt64():X}");

                // Assembly: mov eax, 1; ret (returns 1/TRUE for success)
                byte[] patchBytes = new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 };

                IntPtr handle = OpenProcess(PROCESS_ALL_ACCESS, false, (uint)pid);
                if (handle == IntPtr.Zero)
                {
                    Debug.WriteLine($"Failed to open process. Error: {GetLastError()}");
                    return false;
                }

                try
                {
                    uint oldProtect = 0;
                    if (!VirtualProtectEx(handle, addr, (uint)patchBytes.Length, PAGE_EXECUTE_READWRITE, out oldProtect))
                    {
                        Debug.WriteLine($"Failed to change memory protection. Error: {GetLastError()}");
                        return false;
                    }

                    int bytesWritten = 0;
                    if (!WriteProcessMemory(handle, addr, patchBytes, patchBytes.Length, ref bytesWritten))
                    {
                        Debug.WriteLine($"Failed to write to process memory. Error: {GetLastError()}");
                        return false;
                    }

                    Debug.WriteLine($"Successfully wrote {bytesWritten} bytes");

                    byte[] verifyBuffer = new byte[patchBytes.Length];
                    int bytesRead = 0;
                    if (ReadProcessMemory(handle, addr, verifyBuffer, (uint)verifyBuffer.Length, out bytesRead))
                    {
                        bool patchVerified = bytesRead == patchBytes.Length;
                        for (int i = 0; i < bytesRead && patchVerified; i++)
                        {
                            if (verifyBuffer[i] != patchBytes[i])
                            {
                                patchVerified = false;
                            }
                        }

                        if (patchVerified)
                        {
                            Debug.WriteLine("Patch verified successfully");
                        }
                        else
                        {
                            Debug.WriteLine("Patch verification failed");
                        }
                    }

                    uint dummy;
                    VirtualProtectEx(handle, addr, (uint)patchBytes.Length, oldProtect, out dummy);

                    return bytesWritten == patchBytes.Length;
                }
                finally
                {
                    CloseHandle(handle);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in PatchOperaProcess: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the remote address of a function in another process
        /// </summary>
        private static IntPtr RemoteGetProcAddress(int processId, string dllName, string functionName)
        {
            IntPtr processHandle = IntPtr.Zero;

            try
            {
                processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, (uint)processId);
                if (processHandle == IntPtr.Zero)
                {
                    Debug.WriteLine($"Failed to open process with ID {processId}. Error code: {GetLastError()}");
                    return IntPtr.Zero;
                }

                IntPtr localModuleHandle = LoadLibrary(dllName);
                if (localModuleHandle == IntPtr.Zero)
                {
                    Debug.WriteLine($"Failed to load local module '{dllName}'. Error code: {GetLastError()}");
                    return IntPtr.Zero;
                }

                IntPtr localFunctionAddress = GetProcAddress(localModuleHandle, functionName);
                if (localFunctionAddress == IntPtr.Zero)
                {
                    Debug.WriteLine($"Function '{functionName}' not found in '{dllName}'. Error code: {GetLastError()}");
                    return IntPtr.Zero;
                }

                long offset = localFunctionAddress.ToInt64() - localModuleHandle.ToInt64();
                Debug.WriteLine($"Function offset: 0x{offset:X}");

                IntPtr remoteModuleBase = GetRemoteModuleHandle(processHandle, dllName);
                if (remoteModuleBase == IntPtr.Zero)
                {
                    Debug.WriteLine($"Module '{dllName}' not found in process {processId}");
                    return IntPtr.Zero;
                }

                IntPtr remoteFunctionAddress = new IntPtr(remoteModuleBase.ToInt64() + offset);
                Debug.WriteLine($"Remote function address: 0x{remoteFunctionAddress.ToInt64():X}");

                return remoteFunctionAddress;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in RemoteGetProcAddress: {ex.Message}");
                return IntPtr.Zero;
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                {
                    CloseHandle(processHandle);
                }
            }
        }

        /// <summary>
        /// Gets the base address of a module in a remote process
        /// </summary>
        private static IntPtr GetRemoteModuleHandle(IntPtr processHandle, string moduleName)
        {
            try
            {
                IntPtr[] moduleHandles = new IntPtr[1024];
                int bytesNeeded;

                if (!EnumProcessModules(processHandle, moduleHandles, Marshal.SizeOf(typeof(IntPtr)) * moduleHandles.Length, out bytesNeeded))
                {
                    Debug.WriteLine($"Failed to enumerate modules. Error code: {GetLastError()}");
                    return IntPtr.Zero;
                }

                int moduleCount = bytesNeeded / Marshal.SizeOf(typeof(IntPtr));
                StringBuilder moduleNameBuffer = new StringBuilder(256);

                string targetName = moduleName.ToLower();

                for (int i = 0; i < moduleCount; i++)
                {
                    GetModuleFileNameEx(processHandle, moduleHandles[i], moduleNameBuffer, moduleNameBuffer.Capacity);
                    string currentModuleName = moduleNameBuffer.ToString();

                    string fileName = System.IO.Path.GetFileName(currentModuleName).ToLower();

                    if (fileName == targetName || fileName == targetName + ".dll")
                    {
                        return moduleHandles[i];
                    }
                }

                return IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetRemoteModuleHandle: {ex.Message}");
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// Enables debug privilege for the specified process
        /// </summary>
        private static bool EnableDebugPrivilege(int processId)
        {
            IntPtr processHandle = IntPtr.Zero;
            IntPtr tokenHandle = IntPtr.Zero;

            try
            {
                processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, (uint)processId);
                if (processHandle == IntPtr.Zero)
                {
                    Debug.WriteLine("Failed to open process for debug privilege");
                    return false;
                }

                if (!OpenProcessToken(processHandle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out tokenHandle))
                {
                    Debug.WriteLine("Failed to open process token");
                    return false;
                }

                LUID luid;
                if (!LookupPrivilegeValue(null, "SeDebugPrivilege", out luid))
                {
                    Debug.WriteLine("Failed to lookup privilege value");
                    return false;
                }

                TOKEN_PRIVILEGES tokenPrivileges = new TOKEN_PRIVILEGES
                {
                    PrivilegeCount = 1,
                    Luid = luid,
                    Attributes = SE_PRIVILEGE_ENABLED
                };

                if (!AdjustTokenPrivileges(tokenHandle, false, ref tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero))
                {
                    Debug.WriteLine("Failed to adjust token privileges");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in EnableDebugPrivilege: {ex.Message}");
                return false;
            }
            finally
            {
                if (tokenHandle != IntPtr.Zero)
                    CloseHandle(tokenHandle);
                if (processHandle != IntPtr.Zero)
                    CloseHandle(processHandle);
            }
        }

        /// <summary>
        /// Asynchronously patches Opera processes with retry logic
        /// </summary>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        /// <param name="delayBetweenRetries">Delay between retry attempts in milliseconds</param>
        /// <returns>Task that completes when patching is done</returns>
        public static async Task PatchOperaAsync(int maxRetries = 5, int delayBetweenRetries = 2000)
        {
            await Task.Run(async () =>
            {
                for (int attempt = 0; attempt < maxRetries; attempt++)
                {
                    try
                    {
                        if (PatchOperaProcesses())
                        {
                            Debug.WriteLine("Opera patching completed successfully");
                            return;
                        }

                        if (attempt < maxRetries - 1)
                        {
                            Debug.WriteLine($"Opera patching attempt {attempt + 1} failed, retrying in {delayBetweenRetries}ms");
                            await Task.Delay(delayBetweenRetries);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Opera patching attempt {attempt + 1} failed with exception: {ex.Message}");
                        if (attempt < maxRetries - 1)
                        {
                            await Task.Delay(delayBetweenRetries);
                        }
                    }
                }

                Debug.WriteLine("All Opera patching attempts failed");
            });
        }
    }
}
