using TerraFX.Interop.DirectX;

namespace OGS.Windows.Video.Encode;

public readonly unsafe ref struct D3DEncodeArgs
{
    public required ID3D11Texture2D* Texture { get; init; }
    public required Memory<byte> Buffer { get; init; }
    public required bool Keyframe { get; init; }
    public required TimeSpan Timestamp { get; init; }
}