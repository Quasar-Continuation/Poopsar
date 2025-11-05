//===============================================================================================//
// NT API Hooking Implementation
//===============================================================================================//
#ifdef __cplusplus
extern "C" {
#endif

#include "NtApiHooks.h"
#include "NtApiHooksConfig.h"
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
#if ENABLE_DEBUG_LOGGING
        if (g_LogFile != INVALID_HANDLE_VALUE) {
            DWORD written;
            DWORD messageLen = (DWORD)wcslen(message) * sizeof(WCHAR);
            WriteFile(g_LogFile, message, messageLen, &written, NULL);

            const WCHAR newline[] = L"\r\n";
            WriteFile(g_LogFile, newline, sizeof(newline) - sizeof(WCHAR), &written, NULL);
            FlushFileBuffers(g_LogFile);
        }
#endif
    }

    void LogDebugA(const char* message) {
#if ENABLE_DEBUG_LOGGING
        if (g_LogFile != INVALID_HANDLE_VALUE) {
            DWORD written;
            DWORD messageLen = (DWORD)strlen(message);
            WriteFile(g_LogFile, message, messageLen, &written, NULL);

            const char newline[] = "\r\n";
            WriteFile(g_LogFile, newline, sizeof(newline) - 1, &written, NULL);
            FlushFileBuffers(g_LogFile);
        }
#endif
    }

    // Helper function for case-insensitive wide string comparison
    int wcsnicmp_custom(const WCHAR* s1, const WCHAR* s2, SIZE_T count) {
        for (SIZE_T i = 0; i < count; i++) {
            WCHAR c1 = s1[i];
            WCHAR c2 = s2[i];

            // Convert to uppercase for comparison
            if (c1 >= L'a' && c1 <= L'z') c1 = c1 - L'a' + L'A';
            if (c2 >= L'a' && c2 <= L'z') c2 = c2 - L'a' + L'A';

            // Also handle backslash vs forward slash
            if (c1 == L'/') c1 = L'\\';
            if (c2 == L'/') c2 = L'\\';

            if (c1 != c2) return (c1 < c2) ? -1 : 1;
        }
        return 0;
    }

    // Helper function to normalize NT paths - skip \??\ prefix if present
    const WCHAR* NormalizePath(const WCHAR* path, SIZE_T* adjustedLength) {
        if (!path || !adjustedLength) return path;

        SIZE_T length = *adjustedLength;

        // Check for \??\ prefix (NT object namespace for DOS devices)
        if (length >= 4 && path[0] == L'\\' && path[1] == L'?' && path[2] == L'?' && path[3] == L'\\') {
            *adjustedLength = length - 4;
            return path + 4;
        }

        // Check for \Device\ or \DEVICE\ prefix
        if (length >= 8 &&
            (wcsnicmp_custom(path, L"\\DEVICE\\", 8) == 0 || wcsnicmp_custom(path, L"\\Device\\", 8) == 0)) {
            // Don't adjust - these are device paths, not file paths
            return path;
        }

        return path;
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

        // Normalize the path (strip \??\ prefix if present)
        SIZE_T normalizedLength = length;
        const WCHAR* normalizedPath = NormalizePath(path, &normalizedLength);

        if (g_LogFile != INVALID_HANDLE_VALUE) {
            WCHAR tempPath[512] = { 0 };
            SIZE_T copyLen = normalizedLength < 511 ? normalizedLength : 511;
            wcsncpy_s(tempPath, 512, normalizedPath, copyLen);
            LogDebug(L"[NeedsRedirection] Checking normalized path: ");
            LogDebug(tempPath);
            LogDebug(L"[NeedsRedirection] Against search string: ");
            LogDebug(g_SearchString);
        }

        if (normalizedLength < searchLen) return FALSE;

        // Search for the search string in the normalized path (case-insensitive)
        for (SIZE_T i = 0; i <= normalizedLength - searchLen; i++) {
            if (wcsnicmp_custom(&normalizedPath[i], g_SearchString, searchLen) == 0) {
                if (g_LogFile != INVALID_HANDLE_VALUE) {
                    LogDebug(L"[NeedsRedirection] MATCH FOUND at position ");
                    WCHAR posStr[32];
                    wsprintfW(posStr, L"%zu", i);
                    LogDebug(posStr);
                }
                return TRUE;
            }
        }

        if (g_LogFile != INVALID_HANDLE_VALUE) {
            LogDebug(L"[NeedsRedirection] NO MATCH");
        }
        return FALSE;
    }

    // Helper function to replace search string with the replacement string
    WCHAR* ReplacePath(const WCHAR* originalPath, SIZE_T originalLength, SIZE_T* newLength) {
        if (!originalPath || originalLength == 0 || !newLength) return NULL;

        SIZE_T searchLen = wcslen(g_SearchString);
        SIZE_T replaceLen = wcslen(g_ReplacementString);

        if (searchLen == 0 || originalLength < searchLen) return NULL;

        // Normalize the path
        SIZE_T normalizedLength = originalLength;
        const WCHAR* normalizedPath = NormalizePath(originalPath, &normalizedLength);
        SIZE_T prefixLength = originalLength - normalizedLength; // Length of \??\ or other prefix

        if (normalizedLength < searchLen) return NULL;

        // Count occurrences (case-insensitive) in normalized portion
        SIZE_T occurrences = 0;
        for (SIZE_T i = 0; i <= normalizedLength - searchLen; i++) {
            if (wcsnicmp_custom(&normalizedPath[i], g_SearchString, searchLen) == 0) {
                occurrences++;
                i += searchLen - 1; // Skip past this occurrence
            }
        }

        if (occurrences == 0) return NULL;

        // Calculate new length (prefix + modified path)
        SIZE_T calcNewLength = prefixLength + normalizedLength + (occurrences * (replaceLen - searchLen));
        WCHAR* newPath = (WCHAR*)HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, (calcNewLength + 1) * sizeof(WCHAR));
        if (!newPath) return NULL;

        // Copy prefix (\??\ or other) if present
        SIZE_T destIdx = 0;
        for (SIZE_T i = 0; i < prefixLength; i++) {
            newPath[destIdx++] = originalPath[i];
        }

        // Perform replacement in normalized portion (case-insensitive)
        SIZE_T srcIdx = 0;

        while (srcIdx < normalizedLength) {
            if (srcIdx <= normalizedLength - searchLen &&
                wcsnicmp_custom(&normalizedPath[srcIdx], g_SearchString, searchLen) == 0) {
                // Copy replacement string
                for (SIZE_T j = 0; j < replaceLen; j++) {
                    newPath[destIdx++] = g_ReplacementString[j];
                }
                srcIdx += searchLen;
            }
            else {
                newPath[destIdx++] = normalizedPath[srcIdx++];
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

            // Log all paths for debugging
            if (g_LogFile != INVALID_HANDLE_VALUE && pathLength > 0) {
                WCHAR tempPath[512] = { 0 };
                SIZE_T copyLen = pathLength < 511 ? pathLength : 511;
                wcsncpy_s(tempPath, 512, ObjectAttributes->ObjectName->Buffer, copyLen);
                LogDebug(L"");
                LogDebug(L"[NtCreateFile] Original Path: ");
                LogDebug(tempPath);
            }

            if (NeedsRedirection(ObjectAttributes->ObjectName->Buffer, pathLength)) {
                SIZE_T newLength = 0;
                buffer = ReplacePath(ObjectAttributes->ObjectName->Buffer, pathLength, &newLength);

                if (buffer) {
                    WCHAR tempBuf[512] = { 0 };
                    SIZE_T copyLen = newLength < 511 ? newLength : 511;
                    wcsncpy_s(tempBuf, 512, buffer, copyLen);
                    LogDebug(L"[NtCreateFile] *** REDIRECTING TO: ");
                    LogDebug(tempBuf);

                    originalString = ObjectAttributes->ObjectName;
                    newString.Buffer = buffer;
                    newString.Length = (USHORT)(newLength * sizeof(WCHAR));
                    newString.MaximumLength = (USHORT)((newLength + 1) * sizeof(WCHAR));
                    ObjectAttributes->ObjectName = &newString;
                }
                else {
                    LogDebug(L"[NtCreateFile] ReplacePath returned NULL");
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

            // Log all paths for debugging
            if (g_LogFile != INVALID_HANDLE_VALUE && pathLength > 0 && g_HooksInitialized) {
                WCHAR tempPath[512] = { 0 };
                SIZE_T copyLen = pathLength < 511 ? pathLength : 511;
                wcsncpy_s(tempPath, 512, ObjectAttributes->ObjectName->Buffer, copyLen);
                LogDebug(L"");
                LogDebug(L"[NtOpenFile] Original Path: ");
                LogDebug(tempPath);
            }

            if (NeedsRedirection(ObjectAttributes->ObjectName->Buffer, pathLength)) {
                SIZE_T newLength = 0;
                buffer = ReplacePath(ObjectAttributes->ObjectName->Buffer, pathLength, &newLength);

                if (buffer) {
                    WCHAR tempBuf[512] = { 0 };
                    SIZE_T copyLen = newLength < 511 ? newLength : 511;
                    wcsncpy_s(tempBuf, 512, buffer, copyLen);
                    LogDebug(L"[NtOpenFile] *** REDIRECTING TO: ");
                    LogDebug(tempBuf);

                    originalString = ObjectAttributes->ObjectName;
                    newString.Buffer = buffer;
                    newString.Length = (USHORT)(newLength * sizeof(WCHAR));
                    newString.MaximumLength = (USHORT)((newLength + 1) * sizeof(WCHAR));
                    ObjectAttributes->ObjectName = &newString;
                }
                else {
                    LogDebug(L"[NtOpenFile] ReplacePath returned NULL");
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
        // Use a global try-catch to prevent any crashes
        __try {
#if ENABLE_DEBUG_LOGGING
            // Enable logging for debugging
            WCHAR logPath[512];
            __try {
                ExpandEnvironmentStringsW(L"%TEMP%\\rdi_hooks.log", logPath, 512);
                g_LogFile = CreateFileW(logPath, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
            }
            __except (EXCEPTION_EXECUTE_HANDLER) {
                g_LogFile = INVALID_HANDLE_VALUE;
            }

            if (g_LogFile != INVALID_HANDLE_VALUE) {
                LogDebugA("=== DLL Injection Started ===");
            }
#else
            g_LogFile = INVALID_HANDLE_VALUE;
#endif

            // Initialize to empty strings to prevent crashes
            g_SearchString[0] = L'\0';
            g_ReplacementString[0] = L'\0';

            // Try to get configuration from environment variables
            __try {
                WCHAR envSearchString[512] = { 0 };
                WCHAR envReplaceString[512] = { 0 };

                DWORD searchLen = GetEnvironmentVariableW(L"RDI_SEARCH_PATH", envSearchString, 512);
                DWORD replaceLen = GetEnvironmentVariableW(L"RDI_REPLACE_PATH", envReplaceString, 512);

                if (searchLen > 0 && searchLen < 512 && replaceLen > 0 && replaceLen < 512) {
                    wcsncpy_s(g_SearchString, 512, envSearchString, searchLen);
                    g_SearchString[searchLen] = L'\0';
                    wcsncpy_s(g_ReplacementString, 512, envReplaceString, replaceLen);
                    g_ReplacementString[replaceLen] = L'\0';

                    if (g_LogFile != INVALID_HANDLE_VALUE) {
                        LogDebug(L"========================================");
                        LogDebug(L"[ENV] Search string from env: ");
                        LogDebug(g_SearchString);
                        LogDebug(L"[ENV] Replacement string from env: ");
                        LogDebug(g_ReplacementString);

                        char lenMsg[256];
                        sprintf_s(lenMsg, 256, "[ENV] Search string length: %zu characters", wcslen(g_SearchString));
                        LogDebugA(lenMsg);
                        LogDebug(L"========================================");
                    }
                }
                else {
                    if (g_LogFile != INVALID_HANDLE_VALUE) {
                        LogDebugA("Environment variables not found, hooks disabled");
                    }
                }
            }
            __except (EXCEPTION_EXECUTE_HANDLER) {
                if (g_LogFile != INVALID_HANDLE_VALUE) {
                    LogDebugA("Exception reading environment variables");
                }
                g_SearchString[0] = L'\0';
                g_ReplacementString[0] = L'\0';
            }

            // Initialize MinHook (this must succeed)
            if (g_LogFile != INVALID_HANDLE_VALUE) {
                LogDebugA("Initializing MinHook...");
            }

            if (MH_Initialize() != MH_OK) {
                if (g_LogFile != INVALID_HANDLE_VALUE) {
                    LogDebugA("ERROR: MinHook initialization failed!");
                }
                return;
            }

            if (g_LogFile != INVALID_HANDLE_VALUE) {
                LogDebugA("MinHook initialized successfully");
            }

            HMODULE ntdll = GetModuleHandleW(L"ntdll.dll");
            if (!ntdll) {
                if (g_LogFile != INVALID_HANDLE_VALUE) {
                    LogDebugA("ERROR: Failed to get ntdll.dll handle!");
                }
                MH_Uninitialize();
                return;
            }

            if (g_LogFile != INVALID_HANDLE_VALUE) {
                LogDebugA("Got ntdll.dll handle");
            }

            // Hook all the NT APIs
            FARPROC pNtCreateFile = GetProcAddress(ntdll, "NtCreateFile");
            if (pNtCreateFile) {
                MH_CreateHook(pNtCreateFile, &HookedNtCreateFile, (LPVOID*)&OriginalNtCreateFile);
                MH_EnableHook(pNtCreateFile);
                if (g_LogFile != INVALID_HANDLE_VALUE) LogDebugA("Hooked NtCreateFile");
            }

            FARPROC pNtOpenFile = GetProcAddress(ntdll, "NtOpenFile");
            if (pNtOpenFile) {
                MH_CreateHook(pNtOpenFile, &HookedNtOpenFile, (LPVOID*)&OriginalNtOpenFile);
                MH_EnableHook(pNtOpenFile);
                if (g_LogFile != INVALID_HANDLE_VALUE) LogDebugA("Hooked NtOpenFile");
            }

            FARPROC pNtDeleteFile = GetProcAddress(ntdll, "NtDeleteFile");
            if (pNtDeleteFile) {
                MH_CreateHook(pNtDeleteFile, &HookedNtDeleteFile, (LPVOID*)&OriginalNtDeleteFile);
                MH_EnableHook(pNtDeleteFile);
                if (g_LogFile != INVALID_HANDLE_VALUE) LogDebugA("Hooked NtDeleteFile");
            }

            FARPROC pNtSetInformationFile = GetProcAddress(ntdll, "NtSetInformationFile");
            if (pNtSetInformationFile) {
                MH_CreateHook(pNtSetInformationFile, &HookedNtSetInformationFile, (LPVOID*)&OriginalNtSetInformationFile);
                MH_EnableHook(pNtSetInformationFile);
                if (g_LogFile != INVALID_HANDLE_VALUE) LogDebugA("Hooked NtSetInformationFile");
            }

            FARPROC pNtQueryAttributesFile = GetProcAddress(ntdll, "NtQueryAttributesFile");
            if (pNtQueryAttributesFile) {
                MH_CreateHook(pNtQueryAttributesFile, &HookedNtQueryAttributesFile, (LPVOID*)&OriginalNtQueryAttributesFile);
                MH_EnableHook(pNtQueryAttributesFile);
                if (g_LogFile != INVALID_HANDLE_VALUE) LogDebugA("Hooked NtQueryAttributesFile");
            }

            FARPROC pNtQueryFullAttributesFile = GetProcAddress(ntdll, "NtQueryFullAttributesFile");
            if (pNtQueryFullAttributesFile) {
                MH_CreateHook(pNtQueryFullAttributesFile, &HookedNtQueryFullAttributesFile, (LPVOID*)&OriginalNtQueryFullAttributesFile);
                MH_EnableHook(pNtQueryFullAttributesFile);
                if (g_LogFile != INVALID_HANDLE_VALUE) LogDebugA("Hooked NtQueryFullAttributesFile");
            }

            FARPROC pNtQueryDirectoryFile = GetProcAddress(ntdll, "NtQueryDirectoryFile");
            if (pNtQueryDirectoryFile) {
                MH_CreateHook(pNtQueryDirectoryFile, &HookedNtQueryDirectoryFile, (LPVOID*)&OriginalNtQueryDirectoryFile);
                MH_EnableHook(pNtQueryDirectoryFile);
                if (g_LogFile != INVALID_HANDLE_VALUE) LogDebugA("Hooked NtQueryDirectoryFile");
            }

            FARPROC pNtQueryDirectoryFileEx = GetProcAddress(ntdll, "NtQueryDirectoryFileEx");
            if (pNtQueryDirectoryFileEx) {
                MH_CreateHook(pNtQueryDirectoryFileEx, &HookedNtQueryDirectoryFileEx, (LPVOID*)&OriginalNtQueryDirectoryFileEx);
                MH_EnableHook(pNtQueryDirectoryFileEx);
                if (g_LogFile != INVALID_HANDLE_VALUE) LogDebugA("Hooked NtQueryDirectoryFileEx");
            }

            g_HooksInitialized = TRUE;
            if (g_LogFile != INVALID_HANDLE_VALUE) {
                LogDebugA("=== All hooks installed successfully ===");
            }
        }
        __except (EXCEPTION_EXECUTE_HANDLER) {
            if (g_LogFile != INVALID_HANDLE_VALUE) {
                char excMsg[256];
                sprintf_s(excMsg, 256, "CRITICAL EXCEPTION: Hook installation failed! Code: 0x%X", GetExceptionCode());
                LogDebugA(excMsg);
            }
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
