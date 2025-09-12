using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;

// ReSharper disable InconsistentNaming
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value


namespace OGS.Windows.Native.ComInterfaces;

public unsafe struct IDirect3D11CaptureFramePoolStatics2 : INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in _iid));

    private static ref readonly Guid _iid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
              0x3f,0x10,0x9b,0x58,0xbc,0x6b,0xf5,0x5d,0xa9,0x91,0x02,0xe2,0x8b,0x3b,0x66,0xd5

            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    private readonly void** lpVtbl;

    public HRESULT QueryInterface(Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDirect3D11CaptureFramePoolStatics2*, Guid*, void**, int>)lpVtbl[0])((IDirect3D11CaptureFramePoolStatics2*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDirect3D11CaptureFramePoolStatics2*, uint>)lpVtbl[1])((IDirect3D11CaptureFramePoolStatics2*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDirect3D11CaptureFramePoolStatics2*, uint>)lpVtbl[2])((IDirect3D11CaptureFramePoolStatics2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT CreateFreeThreaded(IDirect3DDevice* device, DirectXPixelFormat pixelFormat, int buffers, SizeInt32 size, Direct3D11CaptureFramePool** pool)
    {
        return ((delegate* unmanaged[MemberFunction]<IDirect3D11CaptureFramePoolStatics2*, IDirect3DDevice*, DirectXPixelFormat, int, SizeInt32, Direct3D11CaptureFramePool**, int>)lpVtbl[6])
            ((IDirect3D11CaptureFramePoolStatics2*)Unsafe.AsPointer(ref this), device, pixelFormat, buffers, size, pool);
    }
}
