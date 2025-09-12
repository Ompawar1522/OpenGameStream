using OGS.Windows.HookLib.Apis;
using OGS.Windows.HookShared;
using OGS.Windows.HookShared.Ipc;
using ZeroLog;

namespace OGS.Windows.HookLib;

public sealed unsafe class HookServer : IDisposable
{
    private static readonly Log Log = LogManager.GetLogger<HookServer>();   

    private readonly HookInitArgs _args;
    private readonly Thread _thread;
    private readonly IpcClient _ipcClient;

    private D3D11Capture? _d3D11Capture;

    private bool _exit;

    public HookServer(HookInitArgs args)
    {
        _args = args;

        _ipcClient = new IpcClient(args.GetPipeName());
        _thread = new Thread(ServerThreadStart)
        {
            Name = "HookServer",
        };

        _thread.Start();
    }

    private void ServerThreadStart()
    {
        try
        {
            Log.Info($"Connecting to pipe {_ipcClient.PipeName}");
            _ipcClient.Connect(default);
            Log.Info($"Connected to pipe {_ipcClient.PipeName}");

            //This is where we should be detecting which graphics API the game is using
            //but for now we will just assume D3D11 is being used.
            _d3D11Capture = new D3D11Capture(_ipcClient);

            while (!_exit)
            {
                IpcMessage message = _ipcClient.ReceiveMessage();

                Log.Trace($"IpcMessage -> {message.Type.ToString()}");
            }
        }
        catch(Exception ex)
        {
            if(_exit)
                return;

            Log.Error("Hook server thread failed", ex);
        }
        finally
        {
            Log.Info("Hook server thread exiting");
            _d3D11Capture?.Dispose();
        }
    }

    public void Dispose()
    {
        _exit = true;
        _ipcClient.Dispose();
        _thread.Join();
    }
}
