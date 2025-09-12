using OGS.Core.Common;

namespace OGS.Windows.Video.Encode.Nvidia;

public sealed class NvencH264EncoderOptions
{
    public required BitrateValue Bitrate { get; init; }
    public required uint Framerate { get; init; }
    public uint GopLength { get; init; } = 0;
}