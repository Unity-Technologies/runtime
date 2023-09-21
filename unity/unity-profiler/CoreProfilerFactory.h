#pragma once

#include <unknwn.h>
#include <atomic>
#include "CoreProfiler.h"

class CoreProfilerFactory : public IClassFactory
{
private:
    std::atomic<int> m_RefCount;

public:
    CoreProfilerFactory();
    virtual ~CoreProfilerFactory();
    static CoreProfiler* profiler;

    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, LPVOID* ppvObj) override;
    ULONG   STDMETHODCALLTYPE AddRef(void) override;
    ULONG   STDMETHODCALLTYPE Release(void) override;
    HRESULT STDMETHODCALLTYPE CreateInstance(IUnknown *pUnkOuter, REFIID riid, LPVOID* ppvObj) override;
    HRESULT STDMETHODCALLTYPE LockServer(BOOL fLock) override;
};
