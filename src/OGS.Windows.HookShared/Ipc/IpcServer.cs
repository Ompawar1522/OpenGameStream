using System.Collections.Concurrent;
using System.IO.Pipes;
using ZeroLog;

namespace OGS.Windows.HookShared.Ipc;

public sealed class IpcServer : IDisposable
{
    private static readonly Log Log = LogManager.GetLogger<IpcServer>();

    public string PipeName { get; }
    public ConcurrentQueue<IpcMessage> Messages { get; } = new();

    private readonly NamedPipeServerStream _serverStream;
    private bool _disposing;

    public IpcServer(string pipeName)
    {
        PipeName = pipeName;
        _serverStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
    }

    public void WaitForConnection(CancellationToken ct)
    {
        Task.Run(async () =>
        {
            await _serverStream.WaitForConnectionAsync(ct);
        }).GetAwaiter().GetResult();

        Task.Run(() =>
        {
            try
            {
                while (_serverStream.IsConnected)
                {
                    var next = IpcReader.ReceiveMessage(_serverStream);
                    Messages.Enqueue(next);
                }
            }
            catch (Exception ex)
            {
                if (!_disposing)
                    Log.Error("Failed to read IPC message", ex);
            }
            finally
            {
                Messages.Enqueue(new IpcMessage { Type = IpcMessageType.IpcDisconnected });
            }
        });
    }
    
    public bool TrySendSimple(IpcMessageType type)
    {
        try
        {
            Span<byte> buffer = [(byte)type];
            _serverStream.Write(buffer);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error("Failed to write IPC message", ex);
            return false;
        }
    }

    public void Dispose()
    {
        _disposing = true;
        _serverStream.Dispose();
    }
}
