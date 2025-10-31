#ifndef PCH_H
#define PCH_H

#include "framework.h"
#include <Windows.h>
#include <winternl.h>
#include <string>
#include <fstream>
#include <vector>
#include <memory>

typedef NTSTATUS(NTAPI* pNtCreateFile)(
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
);

typedef NTSTATUS(NTAPI* pNtOpenFile)(
    PHANDLE FileHandle,
    ACCESS_MASK DesiredAccess,
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

#endif //PCH_H
