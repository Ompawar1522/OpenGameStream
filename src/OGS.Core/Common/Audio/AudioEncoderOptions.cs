namespace OGS.Core.Common.Audio;

public class AudioEncoderOptions
{

    public required BitrateValue Bitrate { get; init; }

    public required Action<EncodedAudioSample> Callback { get; init; }
}
