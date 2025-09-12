using TerraFX.Interop.DirectX;

namespace OGS.Windows.Video.Capture;

public sealed unsafe class D3DCaptureState
{
    public required ID3D11Device* Device { get; init; }
    public required ID3D11DeviceContext* DeviceContext { get; init; }

    public required uint TextureWidth { get; init; }
    public required uint TextureHeight { get; init; }
    public required DXGI_FORMAT TextureFormat { get; init; }
}
