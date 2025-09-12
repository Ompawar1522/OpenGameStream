using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace OGS.Windows.Audio;

internal unsafe struct WasapiCompletionHandler
{
    private readonly Vtbl* _vtbl;

    private static readonly ConcurrentDictionary<int, ManualResetEventSlim> _resetEvents = new();
    private readonly int _instanceId;
    private uint _refCount;

    public WasapiCompletionHandler(ManualResetEventSlim resetEvent)
    {
        _vtbl = (Vtbl*)NativeMemory.AllocZeroed((nuint)sizeof(Vtbl));
        _instanceId = GetUniqueId();
        _refCount = 1;

        _resetEvents.TryAdd(_instanceId, resetEvent);

        _vtbl->QueryInterface = &QueryInterfaceImpl;
        _vtbl->AddRef = &AddRefImpl;
        _vtbl->Release = &ReleaseImpl;
        _vtbl->ActivateCompleted = &ActivateCompletedImpl;
    }

    private static int GetUniqueId()
    {
        return Environment.TickCount ^ Thread.CurrentThread.ManagedThreadId ^ Guid.NewGuid().GetHashCode();
    }

    private static void RaiseEvent(WasapiCompletionHandler* self)
    {
        if (_resetEvents.TryRemove(self->_instanceId, out var handler))
        {
            handler.Set();
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    static HRESULT QueryInterfaceImpl(WasapiCompletionHandler* thisPtr, Guid* riid, void** ppvObject)
    {
        if (ppvObject == null)
            return E.E_POINTER;

        *ppvObject = null;

        if (*riid == IID.IID_IActivateAudioInterfaceCompletionHandler)
        {
            *ppvObject = thisPtr;
            thisPtr->_vtbl->AddRef(thisPtr);
            return S.S_OK;
        }

        else if (*riid == IID.IID_IAgileObject)
        {
            *ppvObject = thisPtr;
            thisPtr->_vtbl->AddRef(thisPtr);
            return S.S_OK;
        }

        {
            return E.E_NOINTERFACE;
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    static uint AddRefImpl(WasapiCompletionHandler* thisPtr)
    {
        return Interlocked.Increment(ref thisPtr->_refCount);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    static uint ReleaseImpl(WasapiCompletionHandler* thisPtr)
    {
        uint refCount = Interlocked.Decrement(ref thisPtr->_refCount);
        if (refCount == 0)
        {
            _resetEvents.TryRemove(thisPtr->_instanceId, out _);

            if (thisPtr->_vtbl != null)
            {
                NativeMemory.Free(thisPtr->_vtbl);
            }
        }
        return refCount;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    static HRESULT ActivateCompletedImpl(WasapiCompletionHandler* thisPtr, IActivateAudioInterfaceAsyncOperation* operation)
    {
        try
        {
            RaiseEvent(thisPtr);
            return S.S_OK;
        }
        catch
        {
            return E.E_FAIL;
        }
    }

    public void Dispose()
    {
        _resetEvents.TryRemove(_instanceId, out _);
        if (_vtbl != null)
        {
            NativeMemory.Free(_vtbl);
        }
    }

    private struct Vtbl
    {
        public delegate* unmanaged[Stdcall]<WasapiCompletionHandler*, Guid*, void**, HRESULT> QueryInterface;
        public delegate* unmanaged[Stdcall]<WasapiCompletionHandler*, uint> AddRef;
        public delegate* unmanaged[Stdcall]<WasapiCompletionHandler*, uint> Release;
        public delegate* unmanaged[Stdcall]<WasapiCompletionHandler*, IActivateAudioInterfaceAsyncOperation*, HRESULT> ActivateCompleted;
    }
}