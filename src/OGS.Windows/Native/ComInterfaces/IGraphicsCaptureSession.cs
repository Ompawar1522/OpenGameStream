using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;

// ReSharper disable InconsistentNaming
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace OGS.Windows.Native.ComInterfaces;
public unsafe struct IGraphicsCaptureSession
{
    private readonly void** lpVtbl;

    public HRESULT QueryInterface(Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession*, Guid*, void**, int>)lpVtbl[0])((IGraphicsCaptureSession*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession*, uint>)lpVtbl[1])((IGraphicsCaptureSession*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession*, uint>)lpVtbl[2])((IGraphicsCaptureSession*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT StartCapture()
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession*, int>)lpVtbl[6])((IGraphicsCaptureSession*)Unsafe.AsPointer(ref this));
    }
}
