using OGS.Core.Common.Audio;
using OGS.Core.Common.Video;

namespace OGS.Core.Platform;

public sealed class PlatformEvents
{
    public Event<EncodedVideoFrame> OnVideoFrame { get; } = new();
    public Event<EncodedAudioSample> OnAudioSample {  get; } = new();
}
