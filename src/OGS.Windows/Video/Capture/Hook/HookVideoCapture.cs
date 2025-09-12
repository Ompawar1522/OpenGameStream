using OGS.Core.Common;
using OGS.Windows.Common;
using OGS.Windows.HookShared;
using OGS.Windows.HookShared.Ipc;
using OGS.Windows.Video.Processing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.DirectX;

namespace OGS.Windows.Video.Capture.Hook;


public sealed unsafe class HookVideoCapture : IDisposable
{
    private static readonly Log Log = LogManager.GetLogger<HookVideoCapture>();

    private readonly Thread _thread;
    private readonly HookVideoCaptureOptions _options;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Mutex _mutex = new Mutex(false, "OGS_FRAME");

    private State* _state = MemoryHelper.AllocZeroed<State>();
    private ID3DProcessor? _processor;

    public HookVideoCapture(HookVideoCaptureOptions options)
    {
        _thread = new Thread(ThreadStart);
        _thread.Name = "Game hook thread";
        _thread.Start();

        _options = options;
    }

    private void ThreadStart()
    {
        try
        {
            HookThreadMain();
        }catch(Exception ex)
        {
            if(!_cancellationTokenSource.IsCancellationRequested)
                Log.Error("Hook thread error", ex);
        }
        finally
        {
            Log.Info("Hook thread exiting, cleaning up...");
            Cleanup();
        }
    }

    private void HookThreadMain()
    {
        D3D11CreateDevice(null, D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE, HMODULE.NULL, 0, null, 0, D3D11.D3D11_SDK_VERSION,
            &_state->Device, null, &_state->DeviceContext).ThrowIfFailed("D3DCreateDevice failed");

        Log.Info("Created D3D state");

        D3DHelpers.TrySetHighestGpuPriority(_state->Device);

        using (IpcServer ipcServer = new IpcServer("OGS_" + new Random().Next()))
        {
            RunHook(ipcServer);
        }
    }
    
    private void RunHook(IpcServer ipcServer)
    {
        using var winEvent = Win32NamedEvent.Create("Local\\OGS_FRAME_READY");

        HookInitArgs initArgs = default;
        initArgs.PipeNameLength = Encoding.Unicode.GetBytes(ipcServer.PipeName, new Span<byte>(initArgs.PipeNameUnicode, 64));
        initArgs.LogLevel = _options.LogLevel;
        initArgs.ShowConsole = _options.ShowConsole;

        Log.Info("Injecting hook...");
        HookInjector.Inject(_options.ProcessId, initArgs);
        Log.Info("Injected hook");

        ipcServer.WaitForConnection(_cancellationTokenSource.Token);
        Log.Info("IPC server -> client connection established");

        using (WinPreciseTimer pacer = new WinPreciseTimer(TimeSpan.FromSeconds(1) / _options.FrameRate))
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                while(ipcServer.Messages.TryDequeue(out var next))
                {
                    if(next.Type == IpcMessageType.IpcCreateTexture)
                    {
                        OpenSharedTexture(next.CreateTextureData);
                    }else if(next.Type == IpcMessageType.IpcDestroyTexture)
                    {
                        CloseSharedTexture();
                    }
                }

                if (winEvent.TryWait() && _state->SharedTexture is not null)
                {
                    HandleFrame();
                }

                pacer.WaitForNext();
            }
        }
    }

    private void HandleFrame()
    {
        _state->DeviceContext->CopyResource((ID3D11Resource*)_state->LocalTexture, (ID3D11Resource*)_state->SharedTexture);

        _processor?.Process(new D3DProcessArgs
        {
            Texture = _state->LocalTexture,
            CaptureTime = Stopwatch.GetTimestamp(),
        });
    }

    private void OpenSharedTexture(IpcCreateTextureData data)
    {
        Log.Info("Opening shared texture"); 

        CloseSharedTexture();

        try
        {
            _state->Device->OpenSharedResource((HANDLE)data.Handle, Uuidof<ID3D11Texture2D>(), (void**)&_state->SharedTexture)
           .ThrowIfFailed("OpenSharedResource failed");

            D3D11_TEXTURE2D_DESC desc;
            _state->SharedTexture->GetDesc(&desc);

            desc.MiscFlags = 0;
            _state->Device->CreateTexture2D(&desc, null, &_state->LocalTexture).ThrowIfFailed("Failed to create local texture");

            _processor = _options.ProcessorFactory(new D3DCaptureState
            {
                Device = _state->Device,
                DeviceContext = _state->DeviceContext,
                TextureFormat = desc.Format,
                TextureHeight = desc.Height,
                TextureWidth = desc.Width
            });

            Log.Info("Opened shared texture");
        }
        catch(Exception ex)
        {
            CloseSharedTexture();
            Log.Error("Failed to open shared texture", ex);
        }
    }

    private void CloseSharedTexture()
    {
        if(_processor is not null || _state->SharedTexture is not null)
        {
            _processor?.Dispose();
            ReleaseAndNull(ref _state->LocalTexture);
            ReleaseAndNull(ref _state->SharedTexture);

            Log.Info("Closed shared texture");
        }
    }

    private void Cleanup()
    {
        _processor?.Dispose();

        if(_state != null)
        {
            ReleaseAndNull(ref _state->SharedTexture);
            ReleaseAndNull(ref _state->LocalTexture);
            ReleaseAndNull(ref _state->Device);
            ReleaseAndNull(ref _state->DeviceContext);
            NativeMemory.Free(_state);
            _state = null;
        }
    }

    public void Dispose()
    {
        Log.Info("Disposing hook capture...");
        _cancellationTokenSource.Cancel();
        _thread.Join();
        _cancellationTokenSource.Dispose();
        Log.Info("hook capture disposed");
        NativeMemory.Free(_state);
    }

    private struct State
    {
        public ID3D11Texture2D* SharedTexture;
        public ID3D11Texture2D* LocalTexture;

        public ID3D11Device* Device;
        public ID3D11DeviceContext* DeviceContext;
    }
}