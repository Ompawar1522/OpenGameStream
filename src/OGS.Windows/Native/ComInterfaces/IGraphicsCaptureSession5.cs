using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;
// ReSharper disable InconsistentNaming
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value


namespace OGS.Windows.Native.ComInterfaces;
public unsafe struct IGraphicsCaptureSession5
{
    public static readonly Guid Iid = Guid.Parse("67C0EA62-1F85-5061-925A-239BE0AC09CB");

    private readonly void** lpVtbl;

    public HRESULT QueryInterface(Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession5*, Guid*, void**, int>)lpVtbl[0])((IGraphicsCaptureSession5*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession5*, uint>)lpVtbl[1])((IGraphicsCaptureSession5*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession5*, uint>)lpVtbl[2])((IGraphicsCaptureSession5*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT GetMinUpdateInterval(TimeSpan* result)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession5*, TimeSpan*, int>)lpVtbl[6])((IGraphicsCaptureSession5*)Unsafe.AsPointer(ref this), result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT SetMinUpdateInterval(TimeSpan value)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession5*, TimeSpan, int>)lpVtbl[7])((IGraphicsCaptureSession5*)Unsafe.AsPointer(ref this), value);
    }
}
