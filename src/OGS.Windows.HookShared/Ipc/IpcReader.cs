using System.Buffers.Binary;
using System.IO.Pipes;
using TerraFX.Interop.DirectX;

namespace OGS.Windows.HookShared.Ipc;
internal static class IpcReader
{
    public static unsafe IpcMessage ReceiveMessage(PipeStream stream)
    {
        Span<byte> buffer = stackalloc byte[1024];
        stream.ReadExactly(buffer.Slice(0, 1));

        IpcMessage message = new IpcMessage
        {
            Type = (IpcMessageType)buffer[0]
        };

        if (message.Type == IpcMessageType.IpcCreateTexture)
        {
            stream.ReadExactly(buffer.Slice(1, sizeof(IpcCreateTextureData)));

            message.CreateTextureData = new()
            {
                Width = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(1)),
                Height = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(5)),
                Format = (DXGI_FORMAT)BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(9)),
                Handle = BinaryPrimitives.ReadInt64LittleEndian(buffer.Slice(13))
            };
        }

        return message;
    }
}
