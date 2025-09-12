using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;
// ReSharper disable InconsistentNaming
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace OGS.Windows.Native.ComInterfaces;

public unsafe struct IGraphicsCaptureItemInterop : INativeGuid
{
    private void** lpVtbl;

    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in _iid));

    private static ref readonly Guid _iid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                   0x1b,0xe8,0x28,0x36,0xac,0x3c,0x60,0x4c,0xb7,0xf4,0x23,0xce,0x0e,0x0c,0x33,0x56
                ];

            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static IGraphicsCaptureItemInterop* CreateInstance()
    {
        using(WinRtString str = new WinRtString("Windows.Graphics.Capture.GraphicsCaptureItem"))
        {
            IGraphicsCaptureItemInterop* instance;
            Guid id = new Guid(0x3628e81b, 0x3cac, 0x4c60, 0xb7, 0xf4, 0x23, 0xce, 0x0e, 0x0c, 0x33, 0x56);
            
            WinRT.RoGetActivationFactory(str.HString, &id, (void**)&instance)
                .ThrowIfFailed("RoGetActivationFactory failed");

            return instance;
        }
    }

    public HRESULT QueryInterface(Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureItemInterop*, Guid*, void**, int>)lpVtbl[0])((IGraphicsCaptureItemInterop*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureItemInterop*, uint>)lpVtbl[1])((IGraphicsCaptureItemInterop*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureItemInterop*, uint>)lpVtbl[2])((IGraphicsCaptureItemInterop*)Unsafe.AsPointer(ref this));
    }

    [PreserveSig]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT CreateForWindow(HWND hWnd, Guid* iid, void** result)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureItemInterop*, HWND, Guid*, void**, int>)lpVtbl[3])((IGraphicsCaptureItemInterop*)Unsafe.AsPointer(ref this), hWnd, iid, result);
    }

    [PreserveSig]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT CreateForMonitor(HMONITOR hMon, Guid* iid, void** result)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureItemInterop*, HMONITOR, Guid*, void**, int>)lpVtbl[4])((IGraphicsCaptureItemInterop*)Unsafe.AsPointer(ref this), hMon,  iid, result);
    }
}
