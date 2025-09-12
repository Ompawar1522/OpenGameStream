using OGS.Core.Common;

namespace OGS.Windows.Video.Encode.Intel;

public sealed class QsvH264EncoderOptions
{
    public required BitrateValue Bitrate { get; init; }
    public required uint Framerate { get; init; }
}
