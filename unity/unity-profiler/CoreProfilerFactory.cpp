#include "CoreProfilerFactory.h"
#include "CoreProfiler.h"

CoreProfilerFactory::CoreProfilerFactory()
    : m_RefCount(0)
{
}

CoreProfilerFactory::~CoreProfilerFactory()
{
}

HRESULT STDMETHODCALLTYPE CoreProfilerFactory::QueryInterface(REFIID riid, LPVOID* ppvObj)
{
    if (!ppvObj)
        return E_INVALIDARG;

    *ppvObj = nullptr;

    if (riid == IID_IUnknown || riid == IID_IClassFactory)
    {
        *ppvObj = this;
        AddRef();
        return NOERROR;
    }

    return E_NOINTERFACE;
}

ULONG STDMETHODCALLTYPE CoreProfilerFactory::AddRef()
{
    return ++m_RefCount;
}

ULONG STDMETHODCALLTYPE CoreProfilerFactory::Release()
{
    int count = --m_RefCount;
    if (count <= 0)
        delete this;

    return count;
}

HRESULT STDMETHODCALLTYPE CoreProfilerFactory::CreateInstance(IUnknown *pUnkOuter, REFIID riid, LPVOID* ppvObject)
{
    if (pUnkOuter != nullptr)
    {
        *ppvObject = nullptr;
        return CLASS_E_NOAGGREGATION;
    }

    auto profiler = new (std::nothrow) CoreProfiler();
    if (profiler == nullptr)
        return E_OUTOFMEMORY;

    auto hr = profiler->QueryInterface(riid, ppvObject);
    profiler->Release();
    return hr;
}

HRESULT STDMETHODCALLTYPE CoreProfilerFactory::LockServer(BOOL fLock)
{
    return E_NOTIMPL;
}
