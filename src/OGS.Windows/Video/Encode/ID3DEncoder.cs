namespace OGS.Windows.Video.Encode;

public interface ID3DEncoder : IDisposable
{
    uint Encode(D3DEncodeArgs args);
}