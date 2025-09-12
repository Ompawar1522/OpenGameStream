using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;
// ReSharper disable InconsistentNaming
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value


namespace OGS.Windows.Native.ComInterfaces;

public enum GraphicsCaptureDirtyRegionMode
{
    ReportOnly = 0,
    ReportAndRender = 1,
}

public unsafe struct IGraphicsCaptureSession4
{
    public static readonly Guid Iid = Guid.Parse("AE99813C-C257-5759-8ED0-668C9B557ED4");

    private readonly void** lpVtbl;

    public HRESULT QueryInterface(Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession4*, Guid*, void**, int>)lpVtbl[0])((IGraphicsCaptureSession4*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession4*, uint>)lpVtbl[1])((IGraphicsCaptureSession4*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession4*, uint>)lpVtbl[2])((IGraphicsCaptureSession4*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT GetDirtyRegionMode(GraphicsCaptureDirtyRegionMode* result)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession4*, GraphicsCaptureDirtyRegionMode*, int>)lpVtbl[6])((IGraphicsCaptureSession4*)Unsafe.AsPointer(ref this), result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT SetDirtyRegionMode(GraphicsCaptureDirtyRegionMode value)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession4*, GraphicsCaptureDirtyRegionMode, int>)lpVtbl[7])((IGraphicsCaptureSession4*)Unsafe.AsPointer(ref this), value);
    }
}
