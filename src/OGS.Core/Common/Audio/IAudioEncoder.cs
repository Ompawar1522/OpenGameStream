namespace OGS.Core.Common.Audio;

public interface IAudioEncoder : IDisposable
{
    void Encode(ReadOnlySpan<byte> samples);
}
