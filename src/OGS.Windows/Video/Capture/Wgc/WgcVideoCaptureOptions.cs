using OGS.Windows.Video.Processing;
using TerraFX.Interop.Windows;

namespace OGS.Windows.Video.Capture.Wgc;

public sealed class WgcVideoCaptureOptions
{
    public required D3DProcessorFactoryDelegate ProcessorFactory { get; init; }
    public HMONITOR Monitor { get; init; }
    public HWND Window { get; init; }

    public bool EnableBorder { get; init; }
    public bool EnableMouse { get; init; }
    public int FrameRate { get; init; }
}
