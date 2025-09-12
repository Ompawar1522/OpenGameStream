using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;
// ReSharper disable InconsistentNaming
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value


namespace OGS.Windows.Native.ComInterfaces;
public unsafe struct IGraphicsCaptureSession6
{
    public static readonly Guid Iid = Guid.Parse("D7419236-BE20-5E9F-BCD6-C4E98FD6AFDC");

    private readonly void** lpVtbl;

    public HRESULT QueryInterface(Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession6*, Guid*, void**, int>)lpVtbl[0])((IGraphicsCaptureSession6*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession6*, uint>)lpVtbl[1])((IGraphicsCaptureSession6*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession6*, uint>)lpVtbl[2])((IGraphicsCaptureSession6*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT GetIncludeSecondaryWindows(bool* result)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession6*, bool*, int>)lpVtbl[6])((IGraphicsCaptureSession6*)Unsafe.AsPointer(ref this), result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT SetIncludeSecondaryWindows(bool value)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession6*, bool, int>)lpVtbl[7])((IGraphicsCaptureSession6*)Unsafe.AsPointer(ref this), value);
    }
}
