using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Platform;
using TerraFX.Interop.Windows;

namespace OGS.Windows.Ui;

/// <summary>
/// This is a hacky way to render a directX11 texture to an avalonia control.
/// Getting the handle of a normal control doesn't work for some reason, maybe need to get the controls parent or child handle
/// </summary>
[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public sealed class WinPreviewControl : NativeControlHost
{
    private readonly Action<HWND> _onHandleCreated;

    public HWND Handle { get; private set; }

    public WinPreviewControl(Action<HWND> onHandleCreated)
    {
        _onHandleCreated = onHandleCreated;
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        if (Handle != HWND.NULL)
        {
            return new PlatformHandle(Handle, "HWND");
        }
        else
        {
            var h = base.CreateNativeControlCore(parent);
            Handle = (HWND)h.Handle;
            _onHandleCreated(Handle);
            return h;
        }
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        Handle = HWND.NULL;
        
        base.DestroyNativeControlCore(control);
    }
}