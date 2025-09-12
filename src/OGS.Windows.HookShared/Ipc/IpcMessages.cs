using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;

namespace OGS.Windows.HookShared.Ipc;

public enum IpcMessageType
{
    Unknown = 0,
    IpcDestroyTexture,
    IpcCreateTexture,
    IpcDisconnected
}

[StructLayout(LayoutKind.Explicit)]
public struct IpcMessage
{
    [FieldOffset(0)]
    public IpcMessageType Type;

    [FieldOffset(4)]
    public IpcCreateTextureData CreateTextureData;
}

public struct IpcCreateTextureData
{
    public int Width;
    public int Height;
    public DXGI_FORMAT Format;
    public long Handle;
}
