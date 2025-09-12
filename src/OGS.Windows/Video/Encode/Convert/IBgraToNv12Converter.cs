using TerraFX.Interop.DirectX;

namespace OGS.Windows.Video.Encode.Convert;

public unsafe interface IBgraToNv12Converter : IDisposable
{
    void Convert(ID3D11Texture2D* bgraTexture, ID3D11Texture2D* nv12Texture);
}
