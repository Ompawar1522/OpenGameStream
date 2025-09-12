using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;

namespace OGS.Windows.Video.Capture.Wgc;

public unsafe delegate void NativeEventHandlerDelegate(IInspectable* sender, IInspectable* arg);

/// <summary>
/// An AOT friendly version implementation of TypedEventHandler<T1, T2>
/// </summary>
internal unsafe struct NativeEventHandler
{
    private readonly void** _lpVtbl;
    private readonly GCHandle _callbackHandle;
    private readonly GCHandle _selfHandle;
    private uint _refCount;

    public NativeEventHandler(NativeEventHandlerDelegate callback)
    {
        _lpVtbl = (void**)NativeMemory.AllocZeroed((nuint)(IntPtr.Size * 4));
        
        _lpVtbl[0] = (delegate* unmanaged<NativeEventHandler*, Guid*, void**, HRESULT>)&QueryInterface;
        _lpVtbl[1] = (delegate* unmanaged<NativeEventHandler*, uint>)&AddRef;
        _lpVtbl[2] = (delegate* unmanaged<NativeEventHandler*, uint>)&Release;
        _lpVtbl[3] = (delegate* unmanaged<NativeEventHandler*, IInspectable*, IInspectable*, HRESULT>)&Invoke;
        
        _callbackHandle = GCHandle.Alloc(callback);
        _selfHandle = GCHandle.Alloc(this, GCHandleType.Pinned);
        _refCount = 1;
    }

    [UnmanagedCallersOnly]
    private static HRESULT QueryInterface(NativeEventHandler* pThis, Guid* riid, void** ppvObject)
    {
        if (ppvObject == null)
            return E.E_POINTER;

        *ppvObject = null;
        
        if (*riid == IID.IID_IUnknown)
        {
            *ppvObject = pThis;
            ((delegate* unmanaged<NativeEventHandler*, uint>)&AddRef)(pThis);
            return S.S_OK;
        }
        
        if (*riid == IID.IID_IAgileObject)
        {
            *ppvObject = pThis;
            ((delegate* unmanaged<NativeEventHandler*, uint>)&AddRef)(pThis);
            return S.S_OK;
        }
        
        return E.E_NOINTERFACE;
    }

    [UnmanagedCallersOnly]
    private static uint AddRef(NativeEventHandler* pThis)
    {
        return ++pThis->_refCount;
    }

    [UnmanagedCallersOnly]
    private static uint Release(NativeEventHandler* pThis)
    {
        uint newRefCount = --pThis->_refCount;

        if (newRefCount == 0)
        {
            if (pThis->_callbackHandle.IsAllocated)
                pThis->_callbackHandle.Free();
            
            if (pThis->_selfHandle.IsAllocated)
                pThis->_selfHandle.Free();
            
            if (pThis->_lpVtbl != null)
                NativeMemory.Free(pThis->_lpVtbl);
        }

        return newRefCount;
    }

    [UnmanagedCallersOnly]
    private static HRESULT Invoke(NativeEventHandler* pThis, IInspectable* sender, IInspectable* args)
    {
        try
        {
            var callback = (NativeEventHandlerDelegate)pThis->_callbackHandle.Target!;
            callback(sender, args);
            return S.S_OK;
        }
        catch (Exception)
        {
            return E.E_FAIL;
        }
    }
}
