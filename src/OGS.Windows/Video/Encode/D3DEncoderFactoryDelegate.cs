using TerraFX.Interop.DirectX;

namespace OGS.Windows.Video.Encode;

public unsafe delegate ID3DEncoder D3DEncoderFactoryDelegate(ID3D11Device* device);