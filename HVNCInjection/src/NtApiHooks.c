//===============================================================================================//
// NT API Hooking Implementation
//===============================================================================================//
#ifdef __cplusplus
extern "C" {
#endif

#include "NtApiHooks.h"
#include "MinHook.h"
#include <stdio.h>
#include <string.h>

#pragma comment(lib, "libMinHook.x64.lib")
#pragma comment(lib, "ntdll.lib")

    // Global search and replacement strings (filled from parameter)
    static WCHAR g_SearchString[512] = { 0 };
    static WCHAR g_ReplacementString[512] = { 0 };
    static BOOL g_HooksInitialized = FALSE;
    static HANDLE g_LogFile = INVALID_HANDLE_VALUE;

    // Helper function to log debug info
    void LogDebug(const WCHAR* message) {
        if (g_LogFile != INVALID_HANDLE_VALUE) {
            DWORD written;
            // Convert wide string to UTF-16 bytes
            DWORD messageLen = (DWORD)wcslen(message) * sizeof(WCHAR);
            WriteFile(g_LogFile, message, messageLen, &written, NULL);

            // Write newline
            const WCHAR newline[] = L"\r\n";
            WriteFile(g_LogFile, newline, sizeof(newline) - sizeof(WCHAR), &written, NULL);
            FlushFileBuffers(g_LogFile);
        }
    }

    void LogDebugA(const char* message) {
        if (g_LogFile != INVALID_HANDLE_VALUE) {
            DWORD written;
            DWORD messageLen = (DWORD)strlen(message);
            WriteFile(g_LogFile, message, messageLen, &written, NULL);

            // Write newline
            const char newline[] = "\r\n";
            WriteFile(g_LogFile, newline, sizeof(newline) - 1, &written, NULL);
            FlushFileBuffers(g_LogFile);
        }
    }

    // NT API typedefs
    typedef struct _UNICODE_STRING {
        USHORT Length;
        USHORT MaximumLength;
        PWSTR  Buffer;
    } UNICODE_STRING, * PUNICODE_STRING;

    typedef struct _OBJECT_ATTRIBUTES {
        ULONG Length;
        HANDLE RootDirectory;
        PUNICODE_STRING ObjectName;
        ULONG Attributes;
        PVOID SecurityDescriptor;
        PVOID SecurityQualityOfService;
    } OBJECT_ATTRIBUTES, * POBJECT_ATTRIBUTES;

    typedef struct _IO_STATUS_BLOCK {
        union {
            LONG Status;
            PVOID Pointer;
        };
        ULONG_PTR Information;
    } IO_STATUS_BLOCK, * PIO_STATUS_BLOCK;

    typedef enum _FILE_INFORMATION_CLASS {
        FileDirectoryInformation = 1,
        FileFullDirectoryInformation,
        FileBothDirectoryInformation,
        FileBasicInformation,
        FileStandardInformation,
        FileInternalInformation,
        FileEaInformation,
        FileAccessInformation,
        FileNameInformation,
        FileRenameInformation = 10,
        FileLinkInformation,
        FileNamesInformation,
        FileDispositionInformation,
        FilePositionInformation,
        FileFullEaInformation,
        FileModeInformation,
        FileAlignmentInformation,
        FileAllInformation,
        FileAllocationInformation,
        FileEndOfFileInformation,
        FileAlternateNameInformation,
        FileStreamInformation,
        FilePipeInformation,
        FilePipeLocalInformation,
        FilePipeRemoteInformation,
        FileMailslotQueryInformation,
        FileMailslotSetInformation,
        FileCompressionInformation,
        FileObjectIdInformation,
        FileCompletionInformation,
        FileMoveClusterInformation,
        FileQuotaInformation,
        FileReparsePointInformation,
        FileNetworkOpenInformation,
        FileAttributeTagInformation,
        FileTrackingInformation,
        FileIdBothDirectoryInformation,
        FileIdFullDirectoryInformation,
        FileValidDataLengthInformation,
        FileShortNameInformation,
        FileIoCompletionNotificationInformation,
        FileIoStatusBlockRangeInformation,
        FileIoPriorityHintInformation,
        FileSfioReserveInformation,
        FileSfioVolumeInformation,
        FileHardLinkInformation,
        FileProcessIdsUsingFileInformation,
        FileNormalizedNameInformation,
        FileNetworkPhysicalNameInformation,
        FileIdGlobalTxDirectoryInformation,
        FileIsRemoteDeviceInformation,
        FileUnusedInformation,
        FileNumaNodeInformation,
        FileStandardLinkInformation,
        FileRemoteProtocolInformation,
        FileRenameInformationBypassAccessCheck,
        FileLinkInformationBypassAccessCheck,
        FileVolumeNameInformation,
        FileIdInformation,
        FileIdExtdDirectoryInformation,
        FileReplaceCompletionInformation,
        FileHardLinkFullIdInformation,
        FileIdExtdBothDirectoryInformation,
        FileRenameInformationEx = 65,
        FileRenameInformationExBypassAccessCheck,
        FileMaximumInformation
    } FILE_INFORMATION_CLASS, * PFILE_INFORMATION_CLASS;

    // NT API function pointers
    typedef LONG NTSTATUS;

    typedef NTSTATUS(NTAPI* pNtCreateFile)(
        PHANDLE FileHandle,
        ULONG DesiredAccess,
        POBJECT_ATTRIBUTES ObjectAttributes,
        PIO_STATUS_BLOCK IoStatusBlock,
        PLARGE_INTEGER AllocationSize,
        ULONG FileAttributes,
        ULONG ShareAccess,
        ULONG CreateDisposition,
        ULONG CreateOptions,
        PVOID EaBuffer,
        ULONG EaLength
        );

    typedef NTSTATUS(NTAPI* pNtOpenFile)(
        PHANDLE FileHandle,
        ULONG DesiredAccess,
        POBJECT_ATTRIBUTES ObjectAttributes,
        PIO_STATUS_BLOCK IoStatusBlock,
        ULONG ShareAccess,
        ULONG OpenOptions
        );

    typedef NTSTATUS(NTAPI* pNtDeleteFile)(
        POBJECT_ATTRIBUTES ObjectAttributes
        );

    typedef NTSTATUS(NTAPI* pNtSetInformationFile)(
        HANDLE FileHandle,
        PIO_STATUS_BLOCK IoStatusBlock,
        PVOID FileInformation,
        ULONG Length,
        FILE_INFORMATION_CLASS FileInformationClass
        );

    typedef NTSTATUS(NTAPI* pNtQueryAttributesFile)(
        POBJECT_ATTRIBUTES ObjectAttributes,
        PVOID FileInformation
        );

    typedef NTSTATUS(NTAPI* pNtQueryFullAttributesFile)(
        POBJECT_ATTRIBUTES ObjectAttributes,
        PVOID FileInformation
        );

    typedef NTSTATUS(NTAPI* pNtQueryDirectoryFile)(
        HANDLE FileHandle,
        HANDLE Event,
        PVOID ApcRoutine,
        PVOID ApcContext,
        PIO_STATUS_BLOCK IoStatusBlock,
        PVOID FileInformation,
        ULONG Length,
        FILE_INFORMATION_CLASS FileInformationClass,
        BOOLEAN ReturnSingleEntry,
        PUNICODE_STRING FileName,
        BOOLEAN RestartScan
        );

    typedef NTSTATUS(NTAPI* pNtQueryDirectoryFileEx)(
        HANDLE FileHandle,
        HANDLE Event,
        PVOID ApcRoutine,
        PVOID ApcContext,
        PIO_STATUS_BLOCK IoStatusBlock,
        PVOID FileInformation,
        ULONG Length,
        FILE_INFORMATION_CLASS FileInformationClass,
        ULONG QueryFlags,
        PUNICODE_STRING FileName
        );

    // Original function pointers
    pNtCreateFile OriginalNtCreateFile = NULL;
    pNtOpenFile OriginalNtOpenFile = NULL;
    pNtDeleteFile OriginalNtDeleteFile = NULL;
    pNtSetInformationFile OriginalNtSetInformationFile = NULL;
    pNtQueryAttributesFile OriginalNtQueryAttributesFile = NULL;
    pNtQueryFullAttributesFile OriginalNtQueryFullAttributesFile = NULL;
    pNtQueryDirectoryFile OriginalNtQueryDirectoryFile = NULL;
    pNtQueryDirectoryFileEx OriginalNtQueryDirectoryFileEx = NULL;

    // Helper function to check if path needs redirection
    BOOL NeedsRedirection(const WCHAR* path, SIZE_T length) {
        if (!path || length == 0) return FALSE;

        SIZE_T searchLen = wcslen(g_SearchString);
        if (searchLen == 0 || length < searchLen) return FALSE;

        // Search for the search string in the path
        for (SIZE_T i = 0; i <= length - searchLen; i++) {
            if (wcsncmp(&path[i], g_SearchString, searchLen) == 0) {
                return TRUE;
            }
        }
        return FALSE;
    }

    // Helper function to replace search string with the replacement string
    WCHAR* ReplacePath(const WCHAR* originalPath, SIZE_T originalLength, SIZE_T* newLength) {
        if (!originalPath || originalLength == 0 || !newLength) return NULL;

        SIZE_T searchLen = wcslen(g_SearchString);
        SIZE_T replaceLen = wcslen(g_ReplacementString);

        if (searchLen == 0 || originalLength < searchLen) return NULL;

        // Count occurrences
        SIZE_T occurrences = 0;
        for (SIZE_T i = 0; i <= originalLength - searchLen; i++) {
            if (wcsncmp(&originalPath[i], g_SearchString, searchLen) == 0) {
                occurrences++;
            }
        }

        if (occurrences == 0) return NULL;

        // Calculate new length
        SIZE_T calcNewLength = originalLength + (occurrences * (replaceLen - searchLen));
        WCHAR* newPath = (WCHAR*)HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, (calcNewLength + 1) * sizeof(WCHAR));
        if (!newPath) return NULL;

        // Perform replacement
        SIZE_T destIdx = 0;
        SIZE_T srcIdx = 0;

        while (srcIdx < originalLength) {
            if (srcIdx <= originalLength - searchLen &&
                wcsncmp(&originalPath[srcIdx], g_SearchString, searchLen) == 0) {
                // Copy replacement string
                for (SIZE_T j = 0; j < replaceLen; j++) {
                    newPath[destIdx++] = g_ReplacementString[j];
                }
                srcIdx += searchLen;
            }
            else {
                newPath[destIdx++] = originalPath[srcIdx++];
            }
        }

        *newLength = destIdx;
        return newPath;
    }

    // Hook implementations
    NTSTATUS NTAPI HookedNtCreateFile(
        PHANDLE FileHandle,
        ULONG DesiredAccess,
        POBJECT_ATTRIBUTES ObjectAttributes,
        PIO_STATUS_BLOCK IoStatusBlock,
        PLARGE_INTEGER AllocationSize,
        ULONG FileAttributes,
        ULONG ShareAccess,
        ULONG CreateDisposition,
        ULONG CreateOptions,
        PVOID EaBuffer,
        ULONG EaLength
    ) {
        PUNICODE_STRING originalString = NULL;
        UNICODE_STRING newString = { 0 };
        WCHAR* buffer = NULL;

        // Only attempt redirection if hooks are properly initialized and we have the original function
        if (g_HooksInitialized && OriginalNtCreateFile && ObjectAttributes && ObjectAttributes->ObjectName && ObjectAttributes->ObjectName->Buffer) {
            SIZE_T pathLength = ObjectAttributes->ObjectName->Length / sizeof(WCHAR);

            if (NeedsRedirection(ObjectAttributes->ObjectName->Buffer, pathLength)) {
                SIZE_T newLength = 0;
                buffer = ReplacePath(ObjectAttributes->ObjectName->Buffer, pathLength, &newLength);

                if (buffer) {
                    LogDebug(L"[NtCreateFile] REDIRECTING: ");
                    LogDebug(ObjectAttributes->ObjectName->Buffer);
                    LogDebug(L" -> ");
                    LogDebug(buffer);

                    originalString = ObjectAttributes->ObjectName;
                    newString.Buffer = buffer;
                    newString.Length = (USHORT)(newLength * sizeof(WCHAR));
                    newString.MaximumLength = (USHORT)((newLength + 1) * sizeof(WCHAR));
                    ObjectAttributes->ObjectName = &newString;
                }
            }
        }

        NTSTATUS result = OriginalNtCreateFile(FileHandle, DesiredAccess, ObjectAttributes, IoStatusBlock,
            AllocationSize, FileAttributes, ShareAccess, CreateDisposition,
            CreateOptions, EaBuffer, EaLength);

        if (originalString) {
            ObjectAttributes->ObjectName = originalString;
            if (buffer) HeapFree(GetProcessHeap(), 0, buffer);
        }

        return result;
    }

    NTSTATUS NTAPI HookedNtOpenFile(
        PHANDLE FileHandle,
        ULONG DesiredAccess,
        POBJECT_ATTRIBUTES ObjectAttributes,
        PIO_STATUS_BLOCK IoStatusBlock,
        ULONG ShareAccess,
        ULONG OpenOptions
    ) {
        PUNICODE_STRING originalString = NULL;
        UNICODE_STRING newString = { 0 };
        WCHAR* buffer = NULL;

        if (ObjectAttributes && ObjectAttributes->ObjectName && ObjectAttributes->ObjectName->Buffer) {
            SIZE_T pathLength = ObjectAttributes->ObjectName->Length / sizeof(WCHAR);

            if (NeedsRedirection(ObjectAttributes->ObjectName->Buffer, pathLength)) {
                SIZE_T newLength = 0;
                buffer = ReplacePath(ObjectAttributes->ObjectName->Buffer, pathLength, &newLength);

                if (buffer) {
                    originalString = ObjectAttributes->ObjectName;
                    newString.Buffer = buffer;
                    newString.Length = (USHORT)(newLength * sizeof(WCHAR));
                    newString.MaximumLength = (USHORT)((newLength + 1) * sizeof(WCHAR));
                    ObjectAttributes->ObjectName = &newString;
                }
            }
        }

        NTSTATUS result = OriginalNtOpenFile(FileHandle, DesiredAccess, ObjectAttributes, IoStatusBlock, ShareAccess, OpenOptions);

        if (originalString) {
            ObjectAttributes->ObjectName = originalString;
            if (buffer) HeapFree(GetProcessHeap(), 0, buffer);
        }

        return result;
    }

    NTSTATUS NTAPI HookedNtDeleteFile(POBJECT_ATTRIBUTES ObjectAttributes) {
        PUNICODE_STRING originalString = NULL;
        UNICODE_STRING newString = { 0 };
        WCHAR* buffer = NULL;

        if (ObjectAttributes && ObjectAttributes->ObjectName && ObjectAttributes->ObjectName->Buffer) {
            SIZE_T pathLength = ObjectAttributes->ObjectName->Length / sizeof(WCHAR);

            if (NeedsRedirection(ObjectAttributes->ObjectName->Buffer, pathLength)) {
                SIZE_T newLength = 0;
                buffer = ReplacePath(ObjectAttributes->ObjectName->Buffer, pathLength, &newLength);

                if (buffer) {
                    originalString = ObjectAttributes->ObjectName;
                    newString.Buffer = buffer;
                    newString.Length = (USHORT)(newLength * sizeof(WCHAR));
                    newString.MaximumLength = (USHORT)((newLength + 1) * sizeof(WCHAR));
                    ObjectAttributes->ObjectName = &newString;
                }
            }
        }

        NTSTATUS result = OriginalNtDeleteFile(ObjectAttributes);

        if (originalString) {
            ObjectAttributes->ObjectName = originalString;
            if (buffer) HeapFree(GetProcessHeap(), 0, buffer);
        }

        return result;
    }

    NTSTATUS NTAPI HookedNtSetInformationFile(
        HANDLE FileHandle,
        PIO_STATUS_BLOCK IoStatusBlock,
        PVOID FileInformation,
        ULONG Length,
        FILE_INFORMATION_CLASS FileInformationClass
    ) {
        typedef struct {
            BOOLEAN ReplaceIfExists;
            HANDLE RootDirectory;
            ULONG FileNameLength;
            WCHAR FileName[1];
        } FILE_RENAME_INFO;

        if (FileInformation && (FileInformationClass == FileRenameInformation || FileInformationClass == FileRenameInformationEx)) {
            FILE_RENAME_INFO* renameInfo = (FILE_RENAME_INFO*)FileInformation;
            if (renameInfo->FileNameLength > 0) {
                SIZE_T pathLength = renameInfo->FileNameLength / sizeof(WCHAR);

                if (NeedsRedirection(renameInfo->FileName, pathLength)) {
                    SIZE_T newLength = 0;
                    WCHAR* newPath = ReplacePath(renameInfo->FileName, pathLength, &newLength);

                    if (newPath) {
                        ULONG newInfoSize = sizeof(FILE_RENAME_INFO) - sizeof(WCHAR) + (newLength * sizeof(WCHAR));
                        FILE_RENAME_INFO* newRenameInfo = (FILE_RENAME_INFO*)HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, newInfoSize);

                        if (newRenameInfo) {
                            newRenameInfo->ReplaceIfExists = renameInfo->ReplaceIfExists;
                            newRenameInfo->RootDirectory = renameInfo->RootDirectory;
                            newRenameInfo->FileNameLength = (ULONG)(newLength * sizeof(WCHAR));
                            memcpy(newRenameInfo->FileName, newPath, newRenameInfo->FileNameLength);

                            NTSTATUS result = OriginalNtSetInformationFile(FileHandle, IoStatusBlock, newRenameInfo, newInfoSize, FileInformationClass);

                            HeapFree(GetProcessHeap(), 0, newRenameInfo);
                            HeapFree(GetProcessHeap(), 0, newPath);
                            return result;
                        }
                        HeapFree(GetProcessHeap(), 0, newPath);
                    }
                }
            }
        }

        return OriginalNtSetInformationFile(FileHandle, IoStatusBlock, FileInformation, Length, FileInformationClass);
    }

    NTSTATUS NTAPI HookedNtQueryAttributesFile(
        POBJECT_ATTRIBUTES ObjectAttributes,
        PVOID FileInformation
    ) {
        PUNICODE_STRING originalString = NULL;
        UNICODE_STRING newString = { 0 };
        WCHAR* buffer = NULL;

        if (ObjectAttributes && ObjectAttributes->ObjectName && ObjectAttributes->ObjectName->Buffer) {
            SIZE_T pathLength = ObjectAttributes->ObjectName->Length / sizeof(WCHAR);

            if (NeedsRedirection(ObjectAttributes->ObjectName->Buffer, pathLength)) {
                SIZE_T newLength = 0;
                buffer = ReplacePath(ObjectAttributes->ObjectName->Buffer, pathLength, &newLength);

                if (buffer) {
                    originalString = ObjectAttributes->ObjectName;
                    newString.Buffer = buffer;
                    newString.Length = (USHORT)(newLength * sizeof(WCHAR));
                    newString.MaximumLength = (USHORT)((newLength + 1) * sizeof(WCHAR));
                    ObjectAttributes->ObjectName = &newString;
                }
            }
        }

        NTSTATUS result = OriginalNtQueryAttributesFile(ObjectAttributes, FileInformation);

        if (originalString) {
            ObjectAttributes->ObjectName = originalString;
            if (buffer) HeapFree(GetProcessHeap(), 0, buffer);
        }

        return result;
    }

    NTSTATUS NTAPI HookedNtQueryFullAttributesFile(
        POBJECT_ATTRIBUTES ObjectAttributes,
        PVOID FileInformation
    ) {
        PUNICODE_STRING originalString = NULL;
        UNICODE_STRING newString = { 0 };
        WCHAR* buffer = NULL;

        if (ObjectAttributes && ObjectAttributes->ObjectName && ObjectAttributes->ObjectName->Buffer) {
            SIZE_T pathLength = ObjectAttributes->ObjectName->Length / sizeof(WCHAR);

            if (NeedsRedirection(ObjectAttributes->ObjectName->Buffer, pathLength)) {
                SIZE_T newLength = 0;
                buffer = ReplacePath(ObjectAttributes->ObjectName->Buffer, pathLength, &newLength);

                if (buffer) {
                    originalString = ObjectAttributes->ObjectName;
                    newString.Buffer = buffer;
                    newString.Length = (USHORT)(newLength * sizeof(WCHAR));
                    newString.MaximumLength = (USHORT)((newLength + 1) * sizeof(WCHAR));
                    ObjectAttributes->ObjectName = &newString;
                }
            }
        }

        NTSTATUS result = OriginalNtQueryFullAttributesFile(ObjectAttributes, FileInformation);

        if (originalString) {
            ObjectAttributes->ObjectName = originalString;
            if (buffer) HeapFree(GetProcessHeap(), 0, buffer);
        }

        return result;
    }

    NTSTATUS NTAPI HookedNtQueryDirectoryFile(
        HANDLE FileHandle,
        HANDLE Event,
        PVOID ApcRoutine,
        PVOID ApcContext,
        PIO_STATUS_BLOCK IoStatusBlock,
        PVOID FileInformation,
        ULONG Length,
        FILE_INFORMATION_CLASS FileInformationClass,
        BOOLEAN ReturnSingleEntry,
        PUNICODE_STRING FileName,
        BOOLEAN RestartScan
    ) {
        return OriginalNtQueryDirectoryFile(FileHandle, Event, ApcRoutine, ApcContext, IoStatusBlock,
            FileInformation, Length, FileInformationClass,
            ReturnSingleEntry, FileName, RestartScan);
    }

    NTSTATUS NTAPI HookedNtQueryDirectoryFileEx(
        HANDLE FileHandle,
        HANDLE Event,
        PVOID ApcRoutine,
        PVOID ApcContext,
        PIO_STATUS_BLOCK IoStatusBlock,
        PVOID FileInformation,
        ULONG Length,
        FILE_INFORMATION_CLASS FileInformationClass,
        ULONG QueryFlags,
        PUNICODE_STRING FileName
    ) {
        return OriginalNtQueryDirectoryFileEx(FileHandle, Event, ApcRoutine, ApcContext, IoStatusBlock,
            FileInformation, Length, FileInformationClass,
            QueryFlags, FileName);
    }

    // Install all hooks
    void InstallNtApiHooks(LPVOID lpParameter) {
        __try {
            // Disable logging - no disk writes
            g_LogFile = INVALID_HANDLE_VALUE;

            // LogDebugA("=== DLL Injection Started ===");

            // Extract the replacement string from the parameter
            __try {
                if (lpParameter) {
                    LogDebugA("Parameter pointer received, attempting to read...");
                    WCHAR* paramStr = (WCHAR*)lpParameter;

                    // Find the delimiter '|' to split search and replacement strings
                    SIZE_T len = 0;
                    SIZE_T delimiterPos = (SIZE_T)-1;
                    while (len < 511 && paramStr[len] != L'\0') {
                        if (paramStr[len] == L'|' && delimiterPos == (SIZE_T)-1) {
                            delimiterPos = len;
                        }
                        len++;
                    }

                    if (len > 0 && len < 512 && delimiterPos != (SIZE_T)-1 && delimiterPos > 0 && delimiterPos < len - 1) {
                        // Copy search string (before delimiter)
                        wcsncpy_s(g_SearchString, 512, paramStr, delimiterPos);
                        g_SearchString[delimiterPos] = L'\0';

                        // Copy replacement string (after delimiter)
                        wcscpy_s(g_ReplacementString, 512, &paramStr[delimiterPos + 1]);

                        LogDebug(L"Search string set to: ");
                        LogDebug(g_SearchString);
                        LogDebug(L"Replacement string set to: ");
                        LogDebug(g_ReplacementString);
                    }
                    else {
                        // No valid parameter - hooks will not redirect anything
                        g_SearchString[0] = L'\0';
                        g_ReplacementString[0] = L'\0';
                        LogDebugA("Parameter format invalid, hooks disabled");
                    }
                }
                else {
                    // No parameter - hooks will not redirect anything
                    g_SearchString[0] = L'\0';
                    g_ReplacementString[0] = L'\0';
                    LogDebugA("No parameter provided, hooks disabled");
                }
            }
            __except (EXCEPTION_EXECUTE_HANDLER) {
                // Exception reading parameter - hooks will not redirect anything
                g_SearchString[0] = L'\0';
                g_ReplacementString[0] = L'\0';
                LogDebugA("Exception reading parameter, hooks disabled");
            }

            LogDebugA("Initializing MinHook...");
            if (MH_Initialize() != MH_OK) {
                LogDebugA("ERROR: MinHook initialization failed!");
                return;
            }
            LogDebugA("MinHook initialized successfully");

            HMODULE ntdll = GetModuleHandleW(L"ntdll.dll");
            if (!ntdll) {
                LogDebugA("ERROR: Failed to get ntdll.dll handle!");
                MH_Uninitialize();
                return;
            }
            LogDebugA("Got ntdll.dll handle");

            // Hook NtCreateFile
            FARPROC pNtCreateFile = GetProcAddress(ntdll, "NtCreateFile");
            if (pNtCreateFile) {
                MH_CreateHook(pNtCreateFile, &HookedNtCreateFile, (LPVOID*)&OriginalNtCreateFile);
                MH_EnableHook(pNtCreateFile);
                LogDebugA("Hooked NtCreateFile");
            }

            // Hook NtOpenFile
            FARPROC pNtOpenFile = GetProcAddress(ntdll, "NtOpenFile");
            if (pNtOpenFile) {
                MH_CreateHook(pNtOpenFile, &HookedNtOpenFile, (LPVOID*)&OriginalNtOpenFile);
                MH_EnableHook(pNtOpenFile);
                LogDebugA("Hooked NtOpenFile");
            }

            // Hook NtDeleteFile
            FARPROC pNtDeleteFile = GetProcAddress(ntdll, "NtDeleteFile");
            if (pNtDeleteFile) {
                MH_CreateHook(pNtDeleteFile, &HookedNtDeleteFile, (LPVOID*)&OriginalNtDeleteFile);
                MH_EnableHook(pNtDeleteFile);
                LogDebugA("Hooked NtDeleteFile");
            }

            // Hook NtSetInformationFile
            FARPROC pNtSetInformationFile = GetProcAddress(ntdll, "NtSetInformationFile");
            if (pNtSetInformationFile) {
                MH_CreateHook(pNtSetInformationFile, &HookedNtSetInformationFile, (LPVOID*)&OriginalNtSetInformationFile);
                MH_EnableHook(pNtSetInformationFile);
                LogDebugA("Hooked NtSetInformationFile");
            }

            // Hook NtQueryAttributesFile
            FARPROC pNtQueryAttributesFile = GetProcAddress(ntdll, "NtQueryAttributesFile");
            if (pNtQueryAttributesFile) {
                MH_CreateHook(pNtQueryAttributesFile, &HookedNtQueryAttributesFile, (LPVOID*)&OriginalNtQueryAttributesFile);
                MH_EnableHook(pNtQueryAttributesFile);
                LogDebugA("Hooked NtQueryAttributesFile");
            }

            // Hook NtQueryFullAttributesFile
            FARPROC pNtQueryFullAttributesFile = GetProcAddress(ntdll, "NtQueryFullAttributesFile");
            if (pNtQueryFullAttributesFile) {
                MH_CreateHook(pNtQueryFullAttributesFile, &HookedNtQueryFullAttributesFile, (LPVOID*)&OriginalNtQueryFullAttributesFile);
                MH_EnableHook(pNtQueryFullAttributesFile);
                LogDebugA("Hooked NtQueryFullAttributesFile");
            }

            // Hook NtQueryDirectoryFile
            FARPROC pNtQueryDirectoryFile = GetProcAddress(ntdll, "NtQueryDirectoryFile");
            if (pNtQueryDirectoryFile) {
                MH_CreateHook(pNtQueryDirectoryFile, &HookedNtQueryDirectoryFile, (LPVOID*)&OriginalNtQueryDirectoryFile);
                MH_EnableHook(pNtQueryDirectoryFile);
                LogDebugA("Hooked NtQueryDirectoryFile");
            }

            // Hook NtQueryDirectoryFileEx
            FARPROC pNtQueryDirectoryFileEx = GetProcAddress(ntdll, "NtQueryDirectoryFileEx");
            if (pNtQueryDirectoryFileEx) {
                MH_CreateHook(pNtQueryDirectoryFileEx, &HookedNtQueryDirectoryFileEx, (LPVOID*)&OriginalNtQueryDirectoryFileEx);
                MH_EnableHook(pNtQueryDirectoryFileEx);
                LogDebugA("Hooked NtQueryDirectoryFileEx");
            }

            g_HooksInitialized = TRUE;
            LogDebugA("=== All hooks installed successfully ===");
        }
        __except (EXCEPTION_EXECUTE_HANDLER) {
            // If anything fails during initialization, fail silently
            // This prevents crashing the target process
            LogDebugA("EXCEPTION: Hook installation failed!");
        }
    }

    void RemoveNtApiHooks() {
        __try {
            LogDebugA("=== Removing hooks ===");
            g_HooksInitialized = FALSE;
            MH_DisableHook(MH_ALL_HOOKS);
            MH_Uninitialize();

            if (g_LogFile != INVALID_HANDLE_VALUE) {
                CloseHandle(g_LogFile);
                g_LogFile = INVALID_HANDLE_VALUE;
            }
        }
        __except (EXCEPTION_EXECUTE_HANDLER) {
            // Fail silently on cleanup
        }
    }

#ifdef __cplusplus
}
#endif
