using OGS.Core.Common.Audio;
using OGS.Core.Config.Data;

namespace OGS.Windows.Audio;

public sealed class WasapiAudioCaptureOptions
{
    public required DisplayCaptureAudioMode Mode { get; init; }
    public uint ProcessId { get; init; }
    public required Func<AudioCaptureState, IAudioEncoder> EncoderFactory { get; init; }
}
