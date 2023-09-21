#include <windows.h>
#include "CoreProfilerFactory.h"
#include "Unity/IProfilerApi.h"

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
        auto factory = new (std::nothrow) CoreProfilerFactory();
        if (factory == nullptr)
            return E_OUTOFMEMORY;
        return factory->QueryInterface(riid, ppv);
    }
    return CLASS_E_CLASSNOTAVAILABLE;
}

STDAPI DllCanUnloadNow()
{
    return S_OK;
}

struct TestApi : ScriptingCoreCLR::IProfilerApi
{
    virtual void Test() override
    {
        OutputDebugString("[PROFILER] TEST API CALL\n");
    }

    virtual void RunLeakDetection() override
    {
        OutputDebugString("[PROFILER] LEAK DETECTION API CALL\n");
        auto prof = CoreProfilerFactory::profiler;
        auto info = prof->ProfilerInfo;
        info->ForceGC();
    }
};

STDAPI_(ScriptingCoreCLR::IProfilerApi*) DllGetProfilerApi()
{
    static TestApi* s_ProfilerApi = nullptr;
    if (s_ProfilerApi == nullptr)
        s_ProfilerApi = new (std::nothrow) TestApi();
    
    return s_ProfilerApi;
}
