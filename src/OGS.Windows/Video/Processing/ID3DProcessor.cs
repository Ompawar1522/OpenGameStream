namespace OGS.Windows.Video.Processing;

/// <summary>
/// Process & encodes an ID3D11Texture2D after it has been captured
/// via DXGI etc
/// </summary>
public interface ID3DProcessor : IDisposable
{
    void Process(D3DProcessArgs args);
}