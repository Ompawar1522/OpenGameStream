using OGS.Windows.Video.Processing;
using TerraFX.Interop.Windows;

namespace OGS.Windows.Video.Capture.Dxgi;

public sealed class DxgiVideoCaptureOptions
{
    public required HMONITOR Display { get; init; }
    public uint FramerateLimit { get; init; } = 60;

    public required D3DProcessorFactoryDelegate ProcessorFactory { get; init; }
}