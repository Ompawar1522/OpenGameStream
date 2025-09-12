using TerraFX.Interop.DirectX;

namespace OGS.Windows.Video.Processing;

public readonly unsafe ref struct D3DProcessArgs
{
    public required ID3D11Texture2D* Texture { get; init; }
    public long CaptureTime { get; init; }
}