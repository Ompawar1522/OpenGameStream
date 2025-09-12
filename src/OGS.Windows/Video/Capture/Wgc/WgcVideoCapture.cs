using System.Runtime.InteropServices;
using OGS.Core.Common;
using OGS.Windows.Common;
using OGS.Windows.Native.ComInterfaces;
using OGS.Windows.Video.Processing;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;

namespace OGS.Windows.Video.Capture.Wgc;

public sealed unsafe class WgcVideoCapture : IDisposable
{
    private static readonly Log Log = LogManager.GetLogger<WgcVideoCapture>();

    private readonly State* _state = MemoryHelper.AllocZeroed<State>();
    private readonly WgcVideoCaptureOptions _options;
    private readonly Thread _captureThread;

    private bool _disposed;
    private readonly Lock _lockObject = new();

    private ID3DProcessor? _processor;
    private SizeInt32 _currentItemSize;
    private int _frameRate;

    public WgcVideoCapture(WgcVideoCaptureOptions options)
    {
        _options = options;

        if (options.FrameRate <= 0 || options.FrameRate > 1000)
        {
            Log.Error("Invalid framerate limit. Defaulting to 60");
            _frameRate = 60;
        }
        else
            _frameRate = options.FrameRate;
        
        try
        {
            DirectX.D3D11CreateDevice(null, D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE,
                HMODULE.NULL, 0, null, 0, D3D11.D3D11_SDK_VERSION,
                &_state->Device, &_state->FeatureLevel, &_state->DeviceContext)
                .ThrowIfFailed("D3D11CreateDevice failed");

            Log.Info("Created D3D device");

            D3DHelpers.TrySetHighestGpuPriority(_state->Device);
            CreateCaptureItem();

            D3DHelpers.ConvertDeviceToWinRt(_state->Device, &_state->RtDevice)
                .ThrowIfFailed("Failed to convert from ID3D11Device to IDirect3DDevice");

            SizeInt32 itemSize;
            _state->Item->GetSize(&itemSize)
                .ThrowIfFailed("GetSize failed");

            Direct3D11CaptureFramePool.CreateFreeThreaded(_state->RtDevice, DirectXPixelFormat.DirectXPixelFormat_B8G8R8A8UIntNormalized,
                    1, itemSize, &_state->FramePool)
                .ThrowIfFailed("CreateFreeThreaded failed");
            
            _state->FramePool->CreateCaptureSession(_state->Item, &_state->Session)
                .ThrowIfFailed("CreateCaptureSession failed");

            TrySetBorderEnabled(_options.EnableBorder);
            TrySetCursorEnabled(_options.EnableMouse);
            TrySetMinimumUpdateInterval(TimeSpan.FromMilliseconds(1));

            _state->Session->StartCapture()
                .ThrowIfFailed("StartCapture failed");

            _captureThread = new Thread(ThreadStart);
            _captureThread.Name = "WGC";
            _captureThread.Start();

            Log.Info("Started WGC capture");
        }
        catch (Exception)
        {
            if (_captureThread is null || !_captureThread.IsAlive)
                Cleanup();
            else
                Dispose();
            
            throw;
        }
    }

    private void ThreadStart()
    {
        try
        {  using (WinPreciseTimer fp = new WinPreciseTimer(TimeSpan.FromSeconds(1) / _frameRate))
            {
                while (!_disposed)
                {
                    GetNextFrame();
                    fp.WaitForNext();
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("WGC capture error", ex);
        }
        finally
        {
            Cleanup();
        }
    }

    private void CreateCaptureItem()
    {
        if (_options.Window != default)
        {
            IGraphicsCaptureItem.CreateForWindow(_options.Window, &_state->Item)
                .ThrowIfFailed("CreateForWindow failed");
        }
        else if (_options.Monitor != default)
        {
            IGraphicsCaptureItem.CreateForMonitor(_options.Monitor, &_state->Item)
                .ThrowIfFailed("CreateForMonitor failed");
        }
        else
        {
            throw new ArgumentException("No monitor or window specified");
        }
    }

    private TimeSpan _startTime;

    private bool GetNextFrame()
    {
        IDirect3DCaptureFrame* frame = null;
        IDirect3DSurface* surface = null;
        ID3D11Texture2D* texture = null;
        IDirect3DDxgiInterfaceAccess* dxgiInterface = null;

        try
        {
            if (_disposed)
                return false;

            if (_state->FramePool->TryGetNextFrame(&frame).FAILED || frame is null)
                return false;

            TimeSpan frameTime;
            frame->SystemRelativeTime(&frameTime);
            TimeSpan currentTime = default;

            if (_startTime == default)
            {
                _startTime = frameTime;
            }
            else
            {
                currentTime = frameTime - _startTime;
            }

            frame->GetSurface(&surface)
                .ThrowIfFailed("GetSurface failed");

            surface->QueryInterface(Uuidof<IDirect3DDxgiInterfaceAccess>(), (void**)&dxgiInterface)
                .ThrowIfFailed("QI For IDirect3DDxgiInterfaceAccess failed");

            dxgiInterface->GetInterface(Uuidof<ID3D11Texture2D>(), (void**)&texture)
                .ThrowIfFailed("GetInterface failed");


            SizeInt32 size;
            _state->Item->GetSize(&size);

            if (size.Width != _currentItemSize.Width || size.Height != _currentItemSize.Height)
            {
                _processor?.Dispose();
                _processor = null;

                Log.Info($"New texture size: {size.Width}x{size.Height}");
                _state->FramePool->Recreate(_state->RtDevice, DirectXPixelFormat.DirectXPixelFormat_B8G8R8A8UIntNormalized, 1,
                    new SizeInt32 { Width = size.Width, Height = size.Height })
                    .ThrowIfFailed("Recreate failed");

                _currentItemSize = size;
                return true;
            }

            EnsureProcessor(texture);
            
            _processor!.Process(new D3DProcessArgs
            {
                Texture = texture,
                CaptureTime = currentTime.Ticks,
            });

            return true;
        }
        finally
        {
            ReleaseAndNull(ref frame);
            ReleaseAndNull(ref surface);
            ReleaseAndNull(ref texture);
            ReleaseAndNull(ref dxgiInterface);
        }
    }

    private void EnsureProcessor(ID3D11Texture2D* texture)
    {
        D3D11_TEXTURE2D_DESC desc;
        texture->GetDesc(&desc);

        if (_processor is null)
        {
            _processor = _options.ProcessorFactory(new D3DCaptureState
            {
                Device = _state->Device,
                DeviceContext = _state->DeviceContext,
                TextureFormat = desc.Format,
                TextureHeight = desc.Height,
                TextureWidth = desc.Width
            });
        }
    }

    private void TrySetCursorEnabled(bool enable)
    {
        IGraphicsCaptureSession2* s2;

        fixed (Guid* iid = &IGraphicsCaptureSession2.Iid)
        {
            if (_state->Session->QueryInterface(iid, (void**)&s2).SUCCEEDED)
            {
                s2->SetIsCursorCaptureEnabled(enable);
                s2->Release();
            }
            else
            {
                Log.Warn("Could not set CursorEnabled property: Interface not supported");
            }
        }
    }

    private void TrySetBorderEnabled(bool enable)
    {
        IGraphicsCaptureSession3* s3;

        fixed (Guid* iid = &IGraphicsCaptureSession3.Iid)
        {
            if (_state->Session->QueryInterface(iid, (void**)&s3).SUCCEEDED)
            {
                s3->SetIsBorderRequired(enable);
                s3->Release();
            }
            else
            {
                Log.Warn("Could not set BorderRequired property: Interface not supported");
            }
        }
    }

    private void TrySetMinimumUpdateInterval(TimeSpan interval)
    {
        IGraphicsCaptureSession5* s5;

        fixed (Guid* iid = &IGraphicsCaptureSession5.Iid)
        {
            if (_state->Session->QueryInterface(iid, (void**)&s5).SUCCEEDED)
            {
                s5->SetMinUpdateInterval(interval);
                s5->Release();
            }
            else
            {
                Log.Warn("Could not set MinimumUpdateInterval property: Interface not supported");
            }
        }
    }
    
    private void Cleanup()
    {
        Log.Info("Cleaning up WGC capture objects");
        
        _processor?.Dispose();

        ReleaseAndNull(ref _state->Device);
        ReleaseAndNull(ref _state->DeviceContext);

        TryCallIClosableClose(_state->RtDevice);
        ReleaseAndNull(ref _state->RtDevice);

        TryCallIClosableClose(_state->Item);
        ReleaseAndNull(ref _state->Item);

        TryCallIClosableClose(_state->Session);
        ReleaseAndNull(ref _state->Session);

        TryCallIClosableClose(_state->FramePool);
        ReleaseAndNull(ref _state->FramePool);

        NativeMemory.Free(_state);
    }

    public void Dispose()
    {
        using (_lockObject.EnterScope())
        {
            if (_disposed)
                return;

            _disposed = true;
            _captureThread?.Join();
        }
    }

    private struct State
    {
        public ID3D11Device* Device;
        public ID3D11DeviceContext* DeviceContext;
        public D3D_FEATURE_LEVEL FeatureLevel;

        public IDirect3DDevice* RtDevice;

        public IGraphicsCaptureItem* Item;
        public Direct3D11CaptureFramePool* FramePool;
        public IGraphicsCaptureSession* Session;
    }
}
