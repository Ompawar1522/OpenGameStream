using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;

// ReSharper disable InconsistentNaming
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value


namespace OGS.Windows.Native.ComInterfaces;

public unsafe struct IGraphicsCaptureItem : INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in _iid));
    static Guid* NativeGuid => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in _iid));

    private static ref readonly Guid _iid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                    0x5b,0xf9,0xc3,0x79,
                    0xf7,0x31,
                    0xc2,0x4e,
                    0xa4
                    ,0x64
                    ,0x63
                    ,0x2e
                    ,0xf5
                    ,0xd3
                    ,0x07
                    ,0x60
                ];

            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static HRESULT CreateForWindow(HWND hWnd, IGraphicsCaptureItem** item)
    {
        IGraphicsCaptureItemInterop* interop = IGraphicsCaptureItemInterop.CreateInstance();

        try
        {
            return interop->CreateForWindow(hWnd, NativeGuid, (void**)item);
        }
        finally
        {
            interop->Release();
        }
    }
    public static HRESULT CreateForMonitor(HMONITOR hMonitor, IGraphicsCaptureItem** item)
    {
        IGraphicsCaptureItemInterop* interop = IGraphicsCaptureItemInterop.CreateInstance();

        try
        {
            return interop->CreateForMonitor(hMonitor, NativeGuid, (void**)item);
        }
        finally
        {
            interop->Release();
        }
    }

    private void** lpVtbl;


    public HRESULT QueryInterface(Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureItem*, Guid*, void**, int>)lpVtbl[0])((IGraphicsCaptureItem*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureItem*, uint>)lpVtbl[1])((IGraphicsCaptureItem*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureItem*, uint>)lpVtbl[2])((IGraphicsCaptureItem*)Unsafe.AsPointer(ref this));
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT GetDisplayName(HSTRING* value)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureItem*, HSTRING*, int>)lpVtbl[6])((IGraphicsCaptureItem*)Unsafe.AsPointer(ref this), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT GetSize(SizeInt32* value)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureItem*, SizeInt32*, int>)lpVtbl[7])((IGraphicsCaptureItem*)Unsafe.AsPointer(ref this), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT AddClosed(void* eventHandler, EventRegistrationToken* token)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureItem*, void*, EventRegistrationToken*, int>)lpVtbl[8])((IGraphicsCaptureItem*)Unsafe.AsPointer(ref this), eventHandler, token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT RemoveClosed(void* eventHandler, EventRegistrationToken* token)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureItem*, void*, EventRegistrationToken*, int>)lpVtbl[9])((IGraphicsCaptureItem*)Unsafe.AsPointer(ref this), eventHandler, token);
    }
}
