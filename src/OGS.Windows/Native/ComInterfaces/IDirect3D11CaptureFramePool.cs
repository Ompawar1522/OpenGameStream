using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;
using IDirect3DDevice = TerraFX.Interop.WinRT.IDirect3DDevice;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace OGS.Windows.Native.ComInterfaces;

public unsafe struct Direct3D11CaptureFramePool : INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in Iid));

    private static ref readonly Guid Iid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
              0x23,0xc6,0x50,0xfa,0xda,0x38,0x32,0x4b,0xac,0xf3,0xfa,0x97,0x34,0xad,0x80,0x0e
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    private readonly void** _lpVtbl;

    public static HRESULT CreateFreeThreaded(IDirect3DDevice* device, DirectXPixelFormat format,
        int numBuffers, SizeInt32 size, Direct3D11CaptureFramePool** pool)
    {
        const string name = "Windows.Graphics.Capture.Direct3D11CaptureFramePool";

        using (WinRtString str = new WinRtString(name))
        {
            IDirect3D11CaptureFramePoolStatics2* statics;
            WinRT.RoGetActivationFactory(str.HString, Uuidof<IDirect3D11CaptureFramePoolStatics2>(), (void**)&statics)
                .ThrowIfFailed("RoGetActivationFactory failed");

            try
            {
                return statics->CreateFreeThreaded(device, format, numBuffers, size, pool);
            }
            finally
            {
                statics->Release();
            }
        }
            
    }

    public HRESULT QueryInterface(Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureItem*, Guid*, void**, int>)_lpVtbl[0])((IGraphicsCaptureItem*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureItem*, uint>)_lpVtbl[1])((IGraphicsCaptureItem*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureItem*, uint>)_lpVtbl[2])((IGraphicsCaptureItem*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT Recreate(IDirect3DDevice* device, DirectXPixelFormat format, int numBuffers, SizeInt32 size)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureItem*, IDirect3DDevice*, DirectXPixelFormat, int, SizeInt32, int>)
            _lpVtbl[6])((IGraphicsCaptureItem*)Unsafe.AsPointer(ref this), device, format, numBuffers, size);
    }

    [PreserveSig]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT TryGetNextFrame(IDirect3DCaptureFrame** frame)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureItem*, IDirect3DCaptureFrame**, int>)
            _lpVtbl[7])((IGraphicsCaptureItem*)Unsafe.AsPointer(ref this), frame);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT AddFrameArrived(nint typedEventHandler, EventRegistrationToken* token)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureItem*, nint, EventRegistrationToken*, int>)
            _lpVtbl[8])((IGraphicsCaptureItem*)Unsafe.AsPointer(ref this), typedEventHandler, token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT RemoveFrameArrived(EventRegistrationToken token)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureItem*, EventRegistrationToken, int>)
            _lpVtbl[9])((IGraphicsCaptureItem*)Unsafe.AsPointer(ref this), token);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT CreateCaptureSession(IGraphicsCaptureItem* item, IGraphicsCaptureSession** session)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureItem*, IGraphicsCaptureItem*, IGraphicsCaptureSession**, int>)
            _lpVtbl[10])((IGraphicsCaptureItem*)Unsafe.AsPointer(ref this), item, session);
    }
}
