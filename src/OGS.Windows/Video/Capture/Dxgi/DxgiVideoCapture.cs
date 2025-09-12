using OGS.Core.Common;
using OGS.Windows.Video.Processing;
using System.Runtime.InteropServices;
using OGS.Windows.Common;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.DirectX;

namespace OGS.Windows.Video.Capture.Dxgi;

public sealed unsafe class DxgiVideoCapture : IDisposable
{
    private static readonly Log Log = LogManager.GetLogger<DxgiVideoCapture>();

    private readonly DxgiVideoCaptureOptions _options;

    /// <summary>
    /// Background DXGI thread
    /// </summary>
    private readonly Thread _thread;

    private readonly WinPreciseTimer? _framePacer;
    private readonly State* _state = MemoryHelper.AllocZeroed<State>();

    private const uint MaxAdapters = 12;
    private const uint MaxOutouts = 12;

    private volatile bool _exit;

    private ID3DProcessor? _processor;
    private uint _currentWidth;
    private uint _currentHeight;

    public DxgiVideoCapture(DxgiVideoCaptureOptions options)
    {
        _options = options;

        if (options.FramerateLimit <= 0 || options.FramerateLimit > 1000)
        {
            Log.Error("Invalid framerate limit. Defaulting to 60");
            _framePacer = new WinPreciseTimer(TimeSpan.FromSeconds(1) / 60);
        }
        else
            _framePacer = new WinPreciseTimer(TimeSpan.FromSeconds(1) / _options.FramerateLimit);

        _thread = new Thread(ThreadStart);
        _thread.Name = "DXGI";
        _thread.Priority = ThreadPriority.Highest;
        _thread.IsBackground = false;
        _thread.Start();
    }

    private void ThreadStart()
    {
        Log.Info("DXGI thread start");

        try
        {
            InnerThreadStart();
        }
        catch (Exception ex)
        {
            Log.Error("DXGI thread failed", ex);
        }
        finally
        {
            ThreadCleanup();
            Log.Info("DXGI thread exit");
        }
    }

    private void InnerThreadStart()
    {
        FindOutputFromHMonitor(_options.Display);

        D3D11CreateDevice((IDXGIAdapter*)_state->Adapter, D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_UNKNOWN,
                HMODULE.NULL, 0, null, 0, D3D11.D3D11_SDK_VERSION,
                &_state->Device, &_state->FeatureLevel, &_state->DeviceContext)
            .ThrowIfFailed("D3D11CreateDevice failed");

        Log.Info("Created D3D device context with D3D level " + _state->FeatureLevel);
            
        D3DHelpers.TrySetHighestGpuPriority(_state->Device);

        _state->Output->QueryInterface(Uuidof<IDXGIOutput5>(), (void**)&_state->Output5)
            .ThrowIfFailed("QI for IDXGIOutput5 failed");

        CaptureLoop();
    }

    private void CaptureLoop()
    {
        DXGI_OUTDUPL_FRAME_INFO frameInfo;
        IDXGIResource* resource;
        HRESULT hr = 0;

        while (!_exit)
        {
            if (_state->Duplication is null)
            {
                Log.Info("Attempting to recreate dxgi duplication");
                KeepRetryingDuplication(200);
                Log.Info("Recreated dxgi duplication");
            }

            //_state->Output5->WaitForVBlank()
                //.ThrowIfFailed("WaitForVBlank failed");

            if (_exit)
                break;

            hr = _state->Duplication->AcquireNextFrame(1000/_options.FramerateLimit, &frameInfo, &resource);

            if (hr.SUCCEEDED)
            {
                if (!HandleCaptureSuccess(frameInfo, resource))
                    continue;
            }
            else
            {
                HandleCaptureFailed(hr);
            }

            _framePacer!.WaitForNext();
        }
    }

    private void HandleCaptureFailed(HRESULT hr)
    {
        if (hr == DXGI.DXGI_ERROR_WAIT_TIMEOUT)
            return;

        Log.Error("AcquireNextFrame failed", Marshal.GetExceptionForHR(hr));

        //Release the duplication, duplication will be recreated on the next capture loop
        ReleaseAndNull(ref _state->Duplication);
    }

    private void KeepRetryingDuplication(int timeout)
    {
        while (true)
        {
            HRESULT hr = RecreateDuplication();

            if (hr.SUCCEEDED)
                return;

            if (_exit)
                throw new OperationCanceledException();

            Log.Error("Failed to recreate dxgi duplication", Marshal.GetExceptionForHR(hr));
            Thread.Sleep(timeout);
        }
    }

    private HRESULT RecreateDuplication()
    {
        ReleaseAndNull(ref _state->Duplication);

        return _state->Output5->DuplicateOutput((IUnknown*)_state->Device, &_state->Duplication);
    }

    private bool HandleCaptureSuccess(DXGI_OUTDUPL_FRAME_INFO frameInfo, IDXGIResource* resource)
    {
        ID3D11Texture2D* texture;
        resource->QueryInterface(Uuidof<ID3D11Texture2D>(), (void**)&texture)
            .ThrowIfFailed("QI for ID3D11texture2d failed");

        try
        {
            if (frameInfo.LastPresentTime > 0)
            {
                EnsureProcessor(texture);

                _processor!.Process(new D3DProcessArgs
                {
                    Texture = texture,
                    CaptureTime = frameInfo.LastPresentTime,
                });

                return true;
            }

            return false;
        }
        finally
        {
            texture->Release();
            resource->Release();
            _state->Duplication->ReleaseFrame();
        }
    }

    private void EnsureProcessor(ID3D11Texture2D* texture)
    {
        D3D11_TEXTURE2D_DESC desc;
        texture->GetDesc(&desc);

        if (_processor is null || _currentHeight != desc.Height || _currentWidth != desc.Width)
        {
            _processor?.Dispose();

            _processor = _options.ProcessorFactory(new D3DCaptureState
            {
                Device = _state->Device,
                DeviceContext = _state->DeviceContext,
                TextureFormat = desc.Format,
                TextureHeight = desc.Height,
                TextureWidth = desc.Width
            });

            _currentHeight = desc.Height;
            _currentWidth = desc.Width;
        }
    }

    private void ThreadCleanup()
    {
        Log.Info("DXGI thread is cleaning up");

        _framePacer?.Dispose();
        _processor?.Dispose();

        ReleaseAndNull(ref _state->Factory);
        ReleaseAndNull(ref _state->Adapter);
        ReleaseAndNull(ref _state->Output);
        ReleaseAndNull(ref _state->Output5);
        ReleaseAndNull(ref _state->Duplication);

        ReleaseAndNull(ref _state->DeviceContext);
        ReleaseAndNull(ref _state->Device);

        NativeMemory.Free(_state);
    }

    private void FindOutputFromHMonitor(HMONITOR monitor)
    {
        CreateDXGIFactory1(Uuidof<IDXGIFactory1>(), (void**)&_state->Factory)
            .ThrowIfFailed("CreateDXGIFactory1 failed");

        for (uint i = 0; i < MaxAdapters; i++)
        {
            IDXGIAdapter1* adapter = null;
            HRESULT hr = _state->Factory->EnumAdapters1(i, &adapter);
        
            if (hr.FAILED)
                break;

            Log.Info("Found DXGI adapter " + D3DHelpers.GetAdapterName(adapter));

            bool found = false;
            for (uint b = 0; b < MaxOutouts; b++)
            {
                IDXGIOutput* output = null;
                hr = adapter->EnumOutputs(b, &output);

                if (hr.FAILED)
                    break;

                DXGI_OUTPUT_DESC outputDesc;
                hr = output->GetDesc(&outputDesc);

                if (hr.FAILED)
                {
                    Log.Warn("IDXGIOutput->GetDesc failed: " + Marshal.GetExceptionForHR(hr));
                    output->Release();
                    break;
                }

                string outputName = new string(&outputDesc.DeviceName.e0);
                Log.Info("Found DXGI output " + outputName);

                if (outputDesc.Monitor == monitor)
                {
                    Log.Info($"Found matching IDXGIOutput '{outputName}'");

                    _state->Adapter = adapter;
                    _state->Output = output;
                    found = true;
                    break;
                }
                else
                {
                    output->Release();
                }
            }

            if (found)
                return;

            adapter->Release();
        }

        throw new InvalidOperationException("Could not find IDXGIOutput from HMONITOR");
    }

    public void Dispose()
    {
        _exit = true;

        if (!_thread.Join(2000))
        {
            Log.Warn("Timed out waiting for DXGI thread to exit");
        }
    }

    private struct State
    {
        public IDXGIFactory1* Factory;
        public IDXGIAdapter1* Adapter;
        public IDXGIOutput* Output;
        public IDXGIOutput5* Output5;

        public D3D_FEATURE_LEVEL FeatureLevel;
        public ID3D11Device* Device;
        public ID3D11DeviceContext* DeviceContext;

        public IDXGIOutputDuplication* Duplication;
    }
}