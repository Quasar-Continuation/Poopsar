#pragma once

#include <windows.h>

// Export macro for the reflective loader
#define DLLEXPORT __declspec(dllexport)

// The reflective loader function that will be called by the injector
extern "C" DLLEXPORT ULONG_PTR WINAPI ReflectiveLoader(LPVOID lpParameter);
