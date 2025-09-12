using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;
// ReSharper disable InconsistentNaming
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value


namespace OGS.Windows.Native.ComInterfaces;
public unsafe struct IGraphicsCaptureSession2
{
    public static readonly Guid Iid = Guid.Parse("2C39AE40-7D2E-5044-804E-8B6799D4CF9E");

    private readonly void** lpVtbl;

    public HRESULT QueryInterface(Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession2*, Guid*, void**, int>)lpVtbl[0])((IGraphicsCaptureSession2*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession2*, uint>)lpVtbl[1])((IGraphicsCaptureSession2*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession2*, uint>)lpVtbl[2])((IGraphicsCaptureSession2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT GetIsCursorCaptureEnabled(bool* result)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession2*, bool*, int>)lpVtbl[6])((IGraphicsCaptureSession2*)Unsafe.AsPointer(ref this), result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT SetIsCursorCaptureEnabled(bool value)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession2*, bool, int>)lpVtbl[7])((IGraphicsCaptureSession2*)Unsafe.AsPointer(ref this), value);
    }
}
