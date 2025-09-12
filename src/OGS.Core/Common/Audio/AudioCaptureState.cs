namespace OGS.Core.Common.Audio;

public sealed class AudioCaptureState
{
    public required uint Channels { get; init; }
    public required uint SampleRate { get; init; }
}
