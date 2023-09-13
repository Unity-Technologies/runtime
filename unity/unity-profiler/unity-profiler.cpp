#include <windows.h>
#include "CoreProfilerFactory.h"

STDAPI_(BOOL) DllMain(HINSTANCE hInstDll, DWORD reason, PVOID)
{
    switch (reason)
    {
        case DLL_PROCESS_ATTACH:
            OutputDebugString("[PROFILER] DLL ATTACH\n");
            break;

        case DLL_PROCESS_DETACH:
            OutputDebugString("[PROFILER] DLL DETACH\n");
            break;
    }
    return TRUE;
}

class __declspec(uuid("55A7EED1-7744-4C2C-A5D4-7FD66EF45AAE")) CoreProfiler;

STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID FAR* ppv)
{
    OutputDebugString("[PROFILER] DllGetClassObject\n");

    if (rclsid == __uuidof(CoreProfiler))
    {
        static CoreProfilerFactory factory;
        return factory.QueryInterface(riid, ppv);
    }
    return CLASS_E_CLASSNOTAVAILABLE;
}

STDAPI DllCanUnloadNow()
{
    return S_OK;
}
