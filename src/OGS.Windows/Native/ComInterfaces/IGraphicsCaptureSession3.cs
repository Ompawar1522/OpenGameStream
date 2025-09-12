using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;

// ReSharper disable InconsistentNaming
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace OGS.Windows.Native.ComInterfaces;
public unsafe struct IGraphicsCaptureSession3
{
    public static readonly Guid Iid = Guid.Parse("f2cdd966-22ae-5ea1-9596-3a289344c3be");

    private readonly void** lpVtbl;

    public HRESULT QueryInterface(Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession3*, Guid*, void**, int>)lpVtbl[0])((IGraphicsCaptureSession3*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession3*, uint>)lpVtbl[1])((IGraphicsCaptureSession3*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession3*, uint>)lpVtbl[2])((IGraphicsCaptureSession3*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT GetIsBorderRequired(bool* result)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession3*, bool*, int>)lpVtbl[6])((IGraphicsCaptureSession3*)Unsafe.AsPointer(ref this), result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT SetIsBorderRequired(bool value)
    {
        return ((delegate* unmanaged[MemberFunction]<IGraphicsCaptureSession3*, bool, int>)lpVtbl[7])((IGraphicsCaptureSession3*)Unsafe.AsPointer(ref this), value);
    }
}
