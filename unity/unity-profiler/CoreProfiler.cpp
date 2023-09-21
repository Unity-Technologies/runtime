#include "CoreProfiler.h"

#include <sstream>

CoreProfiler::CoreProfiler()
    : m_RefCount(0)
    , ProfilerInfo(nullptr)
{
}

CoreProfiler::~CoreProfiler()
{
    if (this->ProfilerInfo != nullptr)
    {
        this->ProfilerInfo->Release();
        this->ProfilerInfo = nullptr;
    }
}

HRESULT STDMETHODCALLTYPE CoreProfiler::Initialize(IUnknown *pUnknown)
{
    HRESULT hr = pUnknown->QueryInterface(__uuidof(ICorProfilerInfo8), reinterpret_cast<LPVOID*>(&ProfilerInfo));
    if (FAILED(hr))
        return E_FAIL;

    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::Shutdown()
{
    if (this->ProfilerInfo != nullptr)
    {
        this->ProfilerInfo->Release();
        this->ProfilerInfo = nullptr;
    }

    return NOERROR;
}

ULONG STDMETHODCALLTYPE CoreProfiler::AddRef()
{
    return ++m_RefCount;
}

ULONG STDMETHODCALLTYPE CoreProfiler::Release()
{
    int count = --m_RefCount;
    if (count <= 0)
        delete this;

    return count;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::QueryInterface(REFIID riid, LPVOID* ppvObj)
{
    if (!ppvObj)
        return E_INVALIDARG;

    *ppvObj = nullptr;

    if (riid == __uuidof(ICorProfilerCallback8) ||
        riid == __uuidof(ICorProfilerCallback7) ||
        riid == __uuidof(ICorProfilerCallback6) ||
        riid == __uuidof(ICorProfilerCallback5) ||
        riid == __uuidof(ICorProfilerCallback4) ||
        riid == __uuidof(ICorProfilerCallback3) ||
        riid == __uuidof(ICorProfilerCallback2) ||
        riid == __uuidof(ICorProfilerCallback)  ||
        riid == IID_IUnknown)
    {
        *ppvObj = this;
        AddRef();
        return NOERROR;
    }

    return E_NOINTERFACE;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::AppDomainCreationStarted(AppDomainID appDomainId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::AppDomainCreationFinished(AppDomainID appDomainId, HRESULT hrStatus)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::AppDomainShutdownStarted(AppDomainID appDomainId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::AppDomainShutdownFinished(AppDomainID appDomainId, HRESULT hrStatus)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::AssemblyLoadStarted(AssemblyID assemblyId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::AssemblyLoadFinished(AssemblyID assemblyId, HRESULT hrStatus)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::AssemblyUnloadStarted(AssemblyID assemblyId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::AssemblyUnloadFinished(AssemblyID assemblyId, HRESULT hrStatus)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ModuleLoadStarted(ModuleID moduleId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ModuleUnloadStarted(ModuleID moduleId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ModuleUnloadFinished(ModuleID moduleId, HRESULT hrStatus)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ModuleAttachedToAssembly(ModuleID moduleId, AssemblyID AssemblyId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ClassLoadStarted(ClassID classId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ClassLoadFinished(ClassID classId, HRESULT hrStatus)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ClassUnloadStarted(ClassID classId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ClassUnloadFinished(ClassID classId, HRESULT hrStatus)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::FunctionUnloadStarted(FunctionID functionId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::JITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::JITCachedFunctionSearchStarted(FunctionID functionId, BOOL *pbUseCachedFunction)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::JITCachedFunctionSearchFinished(FunctionID functionId, COR_PRF_JIT_CACHE result)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::JITFunctionPitched(FunctionID functionId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::JITInlining(FunctionID callerId, FunctionID calleeId, BOOL *pfShouldInline)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ThreadCreated(ThreadID threadId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ThreadDestroyed(ThreadID threadId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::RemotingClientInvocationStarted()
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::RemotingClientSendingMessage(GUID *pCookie, BOOL fIsAsync)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::RemotingClientReceivingReply(GUID *pCookie, BOOL fIsAsync)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::RemotingClientInvocationFinished()
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::RemotingServerReceivingMessage(GUID *pCookie, BOOL fIsAsync)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::RemotingServerInvocationStarted()
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::RemotingServerInvocationReturned()
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::RemotingServerSendingReply(GUID *pCookie, BOOL fIsAsync)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::UnmanagedToManagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ManagedToUnmanagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON suspendReason)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::RuntimeSuspendFinished()
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::RuntimeSuspendAborted()
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::RuntimeResumeStarted()
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::RuntimeResumeFinished()
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::RuntimeThreadSuspended(ThreadID threadId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::RuntimeThreadResumed(ThreadID threadId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::MovedReferences(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], ULONG cObjectIDRangeLength[])
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ObjectAllocated(ObjectID objectId, ClassID classId)
{
  // Convert the ObjectID to a string
    std::wstringstream objectIdStream;
    objectIdStream << L"Object allocated - Object ID: " << objectId;
    const std::wstring objectIdString = objectIdStream.str();

    // Output the string
    OutputDebugStringW(objectIdString.c_str());

    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ObjectsAllocatedByClass(ULONG cClassCount, ClassID classIds[], ULONG cObjects[])
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ObjectReferences(ObjectID objectId, ClassID classId, ULONG cObjectRefs, ObjectID objectRefIds[])
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::RootReferences(ULONG cRootRefs, ObjectID rootRefIds[])
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ExceptionThrown(ObjectID thrownObjectId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ExceptionSearchFunctionEnter(FunctionID functionId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ExceptionSearchFunctionLeave()
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ExceptionSearchFilterEnter(FunctionID functionId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ExceptionSearchFilterLeave()
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ExceptionSearchCatcherFound(FunctionID functionId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ExceptionOSHandlerEnter(UINT_PTR __unused)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ExceptionOSHandlerLeave(UINT_PTR __unused)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ExceptionUnwindFunctionEnter(FunctionID functionId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ExceptionUnwindFunctionLeave()
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ExceptionUnwindFinallyEnter(FunctionID functionId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ExceptionUnwindFinallyLeave()
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ExceptionCatcherEnter(FunctionID functionId, ObjectID objectId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ExceptionCatcherLeave()
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::COMClassicVTableCreated(ClassID wrappedClassId, REFGUID implementedIID, void *pVTable, ULONG cSlots)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::COMClassicVTableDestroyed(ClassID wrappedClassId, REFGUID implementedIID, void *pVTable)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ExceptionCLRCatcherFound()
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ExceptionCLRCatcherExecute()
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[])
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::SurvivingReferences(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], ULONG cObjectIDRangeLength[])
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::GarbageCollectionFinished()
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::FinalizeableObjectQueued(DWORD finalizerFlags, ObjectID objectID)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::RootReferences2(ULONG cRootRefs, ObjectID rootRefIds[], COR_PRF_GC_ROOT_KIND rootKinds[], COR_PRF_GC_ROOT_FLAGS rootFlags[], UINT_PTR rootIds[])
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::HandleCreated(GCHandleID handleId, ObjectID initialObjectId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::HandleDestroyed(GCHandleID handleId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::InitializeForAttach(IUnknown *pCorProfilerInfoUnk, void *pvClientData, UINT cbClientData)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ProfilerAttachComplete()
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ProfilerDetachSucceeded()
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ReJITCompilationStarted(FunctionID functionId, ReJITID rejitId, BOOL fIsSafeToBlock)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::GetReJITParameters(ModuleID moduleId, mdMethodDef methodId, ICorProfilerFunctionControl *pFunctionControl)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ReJITCompilationFinished(FunctionID functionId, ReJITID rejitId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ReJITError(ModuleID moduleId, mdMethodDef methodId, FunctionID functionId, HRESULT hrStatus)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::MovedReferences2(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], SIZE_T cObjectIDRangeLength[])
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::SurvivingReferences2(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], SIZE_T cObjectIDRangeLength[])
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ConditionalWeakTableElementReferences(ULONG cRootRefs, ObjectID keyRefIds[], ObjectID valueRefIds[], GCHandleID rootIds[])
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::GetAssemblyReferences(const WCHAR *wszAssemblyPath, ICorProfilerAssemblyReferenceProvider *pAsmRefProvider)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::ModuleInMemorySymbolsUpdated(ModuleID moduleId)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::DynamicMethodJITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock, LPCBYTE ilHeader, ULONG cbILHeader)
{
    return NOERROR;
}

HRESULT STDMETHODCALLTYPE CoreProfiler::DynamicMethodJITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    return NOERROR;
}
