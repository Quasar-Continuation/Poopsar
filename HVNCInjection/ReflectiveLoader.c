//===============================================================================================//
// Reflective DLL Injection Loader
// Simplified version that's more stable
//===============================================================================================//

#include <windows.h>
#include <winternl.h>

#ifdef __cplusplus
#define DLLEXPORT extern "C" __declspec(dllexport)
#else
#define DLLEXPORT __declspec(dllexport)
#endif

// Forward declarations for PEB structures if not available
#ifndef _PPEB_DEFINED
#define _PPEB_DEFINED
typedef struct _PEB* PPEB;
#endif

// Function pointer typedefs
typedef HMODULE(WINAPI* fnLoadLibraryA)(LPCSTR);
typedef FARPROC(WINAPI* fnGetProcAddress)(HMODULE, LPCSTR);
typedef LPVOID(WINAPI* fnVirtualAlloc)(LPVOID, SIZE_T, DWORD, DWORD);
typedef BOOL(WINAPI* fnVirtualProtect)(LPVOID, SIZE_T, DWORD, PDWORD);
typedef BOOL(WINAPI* fnDllMain)(HINSTANCE, DWORD, LPVOID);
typedef HMODULE(WINAPI* fnGetModuleHandleA)(LPCSTR);

// Helper to get kernel32 base - using a safer method
__forceinline HMODULE GetKernel32Base()
{
    // Method 1: Search backwards from our own address to find kernel32's MZ header
    // This is safer than PEB walking which can crash
    
    // Start from a known address range where kernel32 typically loads
    MEMORY_BASIC_INFORMATION mbi;
    HMODULE hKernel32 = NULL;
    
    // We'll use the return address trick - kernel32 is calling us indirectly
    // So we search our own module's import table which should have kernel32 functions
    
    // Alternative: Use a known kernel32 address from our IAT
    // The injector allocated us, so we need to find kernel32 another way
    
    // Safest method: Walk memory regions looking for kernel32's PE signature
    for (BYTE* addr = (BYTE*)0x180000000; addr < (BYTE*)0x7FF000000000; addr += 0x10000)
    {
        __try
        {
            // Check for MZ header
            if (*(WORD*)addr == IMAGE_DOS_SIGNATURE)
            {
                IMAGE_DOS_HEADER* dosHeader = (IMAGE_DOS_HEADER*)addr;
                if (dosHeader->e_lfanew > 0 && dosHeader->e_lfanew < 0x1000)
                {
                    IMAGE_NT_HEADERS* ntHeaders = (IMAGE_NT_HEADERS*)(addr + dosHeader->e_lfanew);
                    if (ntHeaders->Signature == IMAGE_NT_SIGNATURE)
                    {
                        // Check export directory for kernel32 exports
                        IMAGE_DATA_DIRECTORY* exportDir = &ntHeaders->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT];
                        if (exportDir->VirtualAddress > 0)
                        {
                            IMAGE_EXPORT_DIRECTORY* exports = (IMAGE_EXPORT_DIRECTORY*)(addr + exportDir->VirtualAddress);
                            if (exports->Name > 0)
                            {
                                char* dllName = (char*)(addr + exports->Name);
                                // Check if this is kernel32.dll
                                if ((dllName[0] == 'k' || dllName[0] == 'K') &&
                                    (dllName[1] == 'e' || dllName[1] == 'E') &&
                                    (dllName[2] == 'r' || dllName[2] == 'R') &&
                                    (dllName[3] == 'n' || dllName[3] == 'N') &&
                                    (dllName[4] == 'e' || dllName[4] == 'E') &&
                                    (dllName[5] == 'l' || dllName[5] == 'L') &&
                                    dllName[6] == '3' &&
                                    dllName[7] == '2')
                                {
                                    hKernel32 = (HMODULE)addr;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
            // Continue searching
        }
    }
    
    return hKernel32;
}

// Find exported function in a module
__forceinline FARPROC GetExportAddress(HMODULE hModule, const char* functionName)
{
    if (!hModule || !functionName)
        return NULL;
        
    BYTE* baseAddress = (BYTE*)hModule;
    IMAGE_DOS_HEADER* dosHeader = (IMAGE_DOS_HEADER*)baseAddress;
    
    if (dosHeader->e_magic != IMAGE_DOS_SIGNATURE)
        return NULL;
        
    IMAGE_NT_HEADERS* ntHeaders = (IMAGE_NT_HEADERS*)(baseAddress + dosHeader->e_lfanew);
    
    if (ntHeaders->Signature != IMAGE_NT_SIGNATURE)
        return NULL;
        
    IMAGE_DATA_DIRECTORY* exportDir = &ntHeaders->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT];
    
    if (exportDir->VirtualAddress == 0)
        return NULL;
        
    IMAGE_EXPORT_DIRECTORY* exports = (IMAGE_EXPORT_DIRECTORY*)(baseAddress + exportDir->VirtualAddress);
    DWORD* names = (DWORD*)(baseAddress + exports->AddressOfNames);
    DWORD* functions = (DWORD*)(baseAddress + exports->AddressOfFunctions);
    WORD* ordinals = (WORD*)(baseAddress + exports->AddressOfNameOrdinals);
    
    for (DWORD i = 0; i < exports->NumberOfNames; i++)
    {
        const char* name = (const char*)(baseAddress + names[i]);
        
        // Compare strings manually
        const char* n1 = name;
        const char* n2 = functionName;
        while (*n1 && *n2 && *n1 == *n2)
        {
            n1++;
            n2++;
        }
        
        if (*n1 == *n2) // Both reached null terminator - match!
        {
            WORD ordinal = ordinals[i];
            return (FARPROC)(baseAddress + functions[ordinal]);
        }
    }
    
    return NULL;
}

// The main reflective loader function
DLLEXPORT ULONG_PTR WINAPI ReflectiveLoader(LPVOID lpParameter)
{
    // Validate parameter
    if (!lpParameter)
        return 0;
        
    BYTE* dllData = (BYTE*)lpParameter;
    
    // Validate we can read from this address
    __try
    {
        BYTE test = dllData[0];
        (void)test; // Suppress unused variable warning
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        return 0; // Can't read from this address
    }
    
    // Get kernel32.dll base address
    HMODULE hKernel32 = GetKernel32Base();
    if (!hKernel32)
        return 0;
    
    // Get required functions
    fnLoadLibraryA pLoadLibraryA = (fnLoadLibraryA)GetExportAddress(hKernel32, "LoadLibraryA");
    fnGetProcAddress pGetProcAddress = (fnGetProcAddress)GetExportAddress(hKernel32, "GetProcAddress");
    fnVirtualAlloc pVirtualAlloc = (fnVirtualAlloc)GetExportAddress(hKernel32, "VirtualAlloc");
    fnVirtualProtect pVirtualProtect = (fnVirtualProtect)GetExportAddress(hKernel32, "VirtualProtect");
    
    if (!pLoadLibraryA || !pGetProcAddress || !pVirtualAlloc || !pVirtualProtect)
        return 0;
    
    // Parse PE headers
    IMAGE_DOS_HEADER* dosHeader = (IMAGE_DOS_HEADER*)dllData;
    if (dosHeader->e_magic != IMAGE_DOS_SIGNATURE)
        return 0;
        
    IMAGE_NT_HEADERS* ntHeaders = (IMAGE_NT_HEADERS*)(dllData + dosHeader->e_lfanew);
    if (ntHeaders->Signature != IMAGE_NT_SIGNATURE)
        return 0;
    
    // Allocate memory for the DLL
    SIZE_T imageSize = ntHeaders->OptionalHeader.SizeOfImage;
    BYTE* dllBase = (BYTE*)pVirtualAlloc(NULL, imageSize, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
    
    if (!dllBase)
        return 0;
    
    // Copy headers
    SIZE_T headersSize = ntHeaders->OptionalHeader.SizeOfHeaders;
    for (SIZE_T i = 0; i < headersSize; i++)
        dllBase[i] = dllData[i];
    
    // Copy sections
    IMAGE_SECTION_HEADER* section = IMAGE_FIRST_SECTION(ntHeaders);
    for (WORD i = 0; i < ntHeaders->FileHeader.NumberOfSections; i++, section++)
    {
        if (section->SizeOfRawData == 0)
            continue;
            
        BYTE* dest = dllBase + section->VirtualAddress;
        BYTE* src = dllData + section->PointerToRawData;
        
        for (DWORD j = 0; j < section->SizeOfRawData; j++)
            dest[j] = src[j];
    }
    
    // Update NT headers pointer for new location
    IMAGE_NT_HEADERS* newNtHeaders = (IMAGE_NT_HEADERS*)(dllBase + dosHeader->e_lfanew);
    
    // Process relocations
    LONGLONG delta = (LONGLONG)dllBase - (LONGLONG)newNtHeaders->OptionalHeader.ImageBase;
    if (delta != 0)
    {
        IMAGE_DATA_DIRECTORY* relocDir = &newNtHeaders->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC];
        if (relocDir->Size > 0)
        {
            IMAGE_BASE_RELOCATION* reloc = (IMAGE_BASE_RELOCATION*)(dllBase + relocDir->VirtualAddress);
            
            while (reloc->VirtualAddress > 0)
            {
                BYTE* dest = dllBase + reloc->VirtualAddress;
                WORD* relocData = (WORD*)((BYTE*)reloc + sizeof(IMAGE_BASE_RELOCATION));
                DWORD numRelocations = (reloc->SizeOfBlock - sizeof(IMAGE_BASE_RELOCATION)) / sizeof(WORD);
                
                for (DWORD i = 0; i < numRelocations; i++)
                {
                    WORD relocType = relocData[i] >> 12;
                    WORD relocOffset = relocData[i] & 0xFFF;
                    
                    if (relocType == IMAGE_REL_BASED_DIR64)
                    {
                        ULONGLONG* patch = (ULONGLONG*)(dest + relocOffset);
                        *patch += delta;
                    }
                    else if (relocType == IMAGE_REL_BASED_HIGHLOW)
                    {
                        DWORD* patch = (DWORD*)(dest + relocOffset);
                        *patch += (DWORD)delta;
                    }
                }
                
                reloc = (IMAGE_BASE_RELOCATION*)((BYTE*)reloc + reloc->SizeOfBlock);
            }
        }
    }
    
    // Resolve imports
    IMAGE_DATA_DIRECTORY* importDir = &newNtHeaders->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT];
    if (importDir->Size > 0)
    {
        IMAGE_IMPORT_DESCRIPTOR* importDesc = (IMAGE_IMPORT_DESCRIPTOR*)(dllBase + importDir->VirtualAddress);
        
        while (importDesc->Name)
        {
            char* moduleName = (char*)(dllBase + importDesc->Name);
            HMODULE hModule = pLoadLibraryA(moduleName);
            
            if (hModule)
            {
                ULONG_PTR* thunk = (ULONG_PTR*)(dllBase + importDesc->FirstThunk);
                ULONG_PTR* origThunk = (ULONG_PTR*)(dllBase + 
                    (importDesc->OriginalFirstThunk ? importDesc->OriginalFirstThunk : importDesc->FirstThunk));
                
                while (*origThunk)
                {
                    if (IMAGE_SNAP_BY_ORDINAL(*origThunk))
                    {
                        *thunk = (ULONG_PTR)pGetProcAddress(hModule, (LPCSTR)IMAGE_ORDINAL(*origThunk));
                    }
                    else
                    {
                        IMAGE_IMPORT_BY_NAME* importByName = (IMAGE_IMPORT_BY_NAME*)(dllBase + (*origThunk));
                        *thunk = (ULONG_PTR)pGetProcAddress(hModule, importByName->Name);
                    }
                    
                    thunk++;
                    origThunk++;
                }
            }
            
            importDesc++;
        }
    }
    
    // Set proper memory protections for sections
    section = IMAGE_FIRST_SECTION(newNtHeaders);
    for (WORD i = 0; i < newNtHeaders->FileHeader.NumberOfSections; i++, section++)
    {
        DWORD protect = PAGE_READONLY;
        
        if (section->Characteristics & IMAGE_SCN_MEM_EXECUTE)
        {
            if (section->Characteristics & IMAGE_SCN_MEM_WRITE)
                protect = PAGE_EXECUTE_READWRITE;
            else
                protect = PAGE_EXECUTE_READ;
        }
        else if (section->Characteristics & IMAGE_SCN_MEM_WRITE)
        {
            protect = PAGE_READWRITE;
        }
        
        DWORD oldProtect;
        pVirtualProtect(dllBase + section->VirtualAddress, section->Misc.VirtualSize, protect, &oldProtect);
    }
    
    // Flush instruction cache
    typedef BOOL(WINAPI* fnFlushInstructionCache)(HANDLE, LPCVOID, SIZE_T);
    fnFlushInstructionCache pFlushInstructionCache = 
        (fnFlushInstructionCache)pGetProcAddress(hKernel32, "FlushInstructionCache");
    if (pFlushInstructionCache)
        pFlushInstructionCache((HANDLE)-1, NULL, 0);
    
    // Call DLL entry point with exception handling
    fnDllMain dllMain = (fnDllMain)(dllBase + newNtHeaders->OptionalHeader.AddressOfEntryPoint);
    BOOL result = FALSE;
    
    __try
    {
        result = dllMain((HINSTANCE)dllBase, DLL_PROCESS_ATTACH, NULL);
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        // DllMain crashed - return error code to indicate this
        return 0xDEADBEEF; // Special error code to indicate DllMain crash
    }
    
    if (!result)
    {
        return 0xBADCAFE; // DllMain returned FALSE
    }
    
    // Return the base address of the loaded DLL
    return (ULONG_PTR)dllBase;
}
