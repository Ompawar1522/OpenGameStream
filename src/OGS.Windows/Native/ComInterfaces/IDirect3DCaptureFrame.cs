using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;

// ReSharper disable InconsistentNaming
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace OGS.Windows.Native.ComInterfaces;

public unsafe struct IDirect3DCaptureFrame : INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in _iid));
    private static ref readonly Guid _iid
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

    private readonly void** lpVtbl;
    public HRESULT QueryInterface(Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDirect3DCaptureFrame*, Guid*, void**, int>)lpVtbl[0])((IDirect3DCaptureFrame*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDirect3DCaptureFrame*, uint>)lpVtbl[1])((IDirect3DCaptureFrame*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDirect3DCaptureFrame*, uint>)lpVtbl[2])((IDirect3DCaptureFrame*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT GetSurface(IDirect3DSurface** surface)
    {
        return ((delegate* unmanaged[MemberFunction]<IDirect3DCaptureFrame*, IDirect3DSurface**, int>)lpVtbl[6])((IDirect3DCaptureFrame*)Unsafe.AsPointer(ref this), surface);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT SystemRelativeTime(TimeSpan* value)
    {
        return ((delegate* unmanaged[MemberFunction]<IDirect3DCaptureFrame*, TimeSpan*, int>)lpVtbl[7])((IDirect3DCaptureFrame*)Unsafe.AsPointer(ref this), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT ContentSize(SizeInt32* value)
    {
        return ((delegate* unmanaged[MemberFunction]<IDirect3DCaptureFrame*, SizeInt32*, int>)lpVtbl[8])((IDirect3DCaptureFrame*)Unsafe.AsPointer(ref this), value);
    }


}
