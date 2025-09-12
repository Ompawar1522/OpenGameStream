using OGS.Windows.Video.Processing;

namespace OGS.Windows.Video.Capture.Hook;

public sealed class HookVideoCaptureOptions
{
    public required D3DProcessorFactoryDelegate ProcessorFactory { get; init; }
    public required uint FrameRate { get; init; }
    public required uint ProcessId { get; init; }

    public bool ShowConsole { get; init; } = false;
    public LogLevel LogLevel { get; init; } = LogLevel.Info;
}
