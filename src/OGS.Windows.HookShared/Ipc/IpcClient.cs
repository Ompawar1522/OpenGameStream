using System.Buffers.Binary;
using System.IO.Pipes;
using ZeroLog;

namespace OGS.Windows.HookShared.Ipc;

public sealed class IpcClient : IDisposable
{
    private static readonly Log Log = LogManager.GetLogger<IpcClient>();

    private readonly string _pipeName;
    private readonly NamedPipeClientStream _clientStream;

    public string PipeName => _pipeName;

    public IpcClient(string pipeName)
    {
        _pipeName = pipeName;

        _clientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
    }

    public void Connect(CancellationToken ct)
    {
        Task.Run(async () =>
        {
            await _clientStream.ConnectAsync(ct);
        }).GetAwaiter().GetResult();
    }

    public IpcMessage ReceiveMessage() => IpcReader.ReceiveMessage(_clientStream);

    public bool TrySendDestroyTexture() => TrySendSimple(IpcMessageType.IpcDestroyTexture);

    private bool TrySendSimple(IpcMessageType type)
    {
        try
        {
            Span<byte> buffer = [(byte)type];
            _clientStream.Write(buffer);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error("Failed to write to IPC client", ex);
            return false;
        }
    }

    public unsafe bool TrySendCreateTexture(IpcCreateTextureData data)
    {
        try
        {
            Span<byte> buffer = stackalloc byte[1 + sizeof(IpcCreateTextureData)];
            buffer[0] = (byte)IpcMessageType.IpcCreateTexture;
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(1), data.Width);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(5), data.Height);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(9), (int)data.Format);
            BinaryPrimitives.WriteInt64LittleEndian(buffer.Slice(13), data.Handle);
            _clientStream.Write(buffer);
            return true;
        }catch(Exception ex)
        {
            Log.Error("Failed to write to IPC client", ex);
            return false;
        }
    }

    public void Dispose()
    {
        _clientStream.Dispose();
    }
}
