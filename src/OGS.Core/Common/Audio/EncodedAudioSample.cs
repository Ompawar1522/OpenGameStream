namespace OGS.Core.Common.Audio;

public readonly ref struct EncodedAudioSample
{
    public required ReadOnlySpan<byte> Data { get; init; }
    public required uint SampleCount { get; init; }
}