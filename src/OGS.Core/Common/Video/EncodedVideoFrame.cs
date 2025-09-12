namespace OGS.Core.Common.Video;

public readonly ref struct EncodedVideoFrame
{
    public required ReadOnlySpan<byte> Data { get; init; }
    public required VideoCodec Codec { get; init; }

    /// <summary>
    /// Time (in ticks) that the frame was produced by the capture source
    /// </summary>
    public required long CaptureTime { get; init; }

    /// <summary>
    /// Timestamp of the frame in the video stream. 
    /// The timestamp resets when video capture is restarted.
    /// </summary>
    public required TimeSpan Timestamp { get; init; }
}
