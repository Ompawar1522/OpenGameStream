using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using OGS.Core.Common;
using OGS.Core.Common.Video;
using OGS.Windows.Common;
using OGS.Windows.Video.Encode;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace OGS.Windows.Video.Processing;

/// <summary>
/// Processes & encoded D3D11 textures. Uses a shared context to allow for
/// multithreaded capture/encoding.
/// </summary>
public sealed unsafe class AsyncD3DProcessor : ID3DProcessor
{
    private static readonly Log Log = LogManager.GetLogger<AsyncD3DProcessor>();

    private readonly AsyncD3DProcessorOptions _options;

    private readonly CaptureContext* _captureContext = MemoryHelper.AllocZeroed<CaptureContext>();
    private readonly ProcessorContext* _processorContext = MemoryHelper.AllocZeroed<ProcessorContext>();

    private readonly Thread _processorThread;

    private readonly TaskCompletionSource _processorReadyTaskSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly IMemoryOwner<byte> _buffer = MemoryPool<byte>.Shared.Rent(1024 * 1024 * 4);

    private readonly object _lockObject = new();
    private readonly AutoResetEvent _textureReadyEvent = new(false);

    private volatile bool _stop;

    private bool _processorThreadCleanedUp;

    private long _captureTime;
    private long _firstFrameTime;

    public AsyncD3DProcessor(AsyncD3DProcessorOptions options)
    {
        _options = options;

        _processorThread = new Thread(ProcessorThreadStart)
        {
            Name = "D3DProcessor",
            Priority = ThreadPriority.Highest,
            IsBackground = false
        };

        _processorThread.Start();
    }

    private void ProcessorThreadStart()
    {
        Log.Info($"D3D processor thread started (texture format {_options.CaptureState.TextureFormat.ToString()})");

        try
        {
            InnerProcessorThreadStart();
            Log.Info("D3D processor thread ending");
        }
        catch (Exception ex)
        {
            Log.Error("D3D processor thread failed", ex);
        }
        finally
        {
            ProcessorThreadCleanup();
        }
    }

    private void InnerProcessorThreadStart()
    {
        try
        {
            lock (_lockObject)
            {
                InitializeCaptureContext();
                InitializeProcessorContext();
            }

            if (Log.IsDebugEnabled)
                Log.Debug("Processor state created");

            _processorReadyTaskSource.TrySetResult();
            ProcessorLoop();
        }
        catch (Exception ex)
        {
            _processorReadyTaskSource.TrySetException(new AggregateException(ex));
            throw;
        }
    }

    private void ProcessorLoop()
    {
        using (ID3DEncoder encoder = _options.EncoderFactory(_processorContext->Device))
        {
            while (!_stop)
            {
                bool eventFired = _textureReadyEvent.WaitOne(100);

                if (!eventFired)
                {
                    if (_stop)
                        break;

                    continue;
                }

                ProcessNext(encoder);
            }
        }
    }

    private void ProcessNext(ID3DEncoder encoder)
    {
        uint length;
        long captureTime;
        TimeSpan videoTrackTime;

        lock (_lockObject)
        {
            captureTime = _captureTime;
            videoTrackTime = TimeSpan.FromTicks(captureTime - _firstFrameTime);

            if (_processorContext->SwapChainBuffer is not null)
            {
                _processorContext->DeviceContext->CopyResource(
                    (ID3D11Resource*)_processorContext->SwapChainBuffer,
                    (ID3D11Resource*)_processorContext->SharedTexture);
            }

            long now = Stopwatch.GetTimestamp();

            length = encoder.Encode(new D3DEncodeArgs
            {
                Buffer = _buffer.Memory,
                Texture = _processorContext->SharedTexture,
                Keyframe = _options.KeyFrameSelector(),
                Timestamp = videoTrackTime
            });
        }

        if(length != 0)
        {
            var buff = _buffer.Memory.Span.Slice(0, (int)length);

            _options.OnEncodedCallback(new EncodedVideoFrame
            {
                Data = buff,
                Timestamp = videoTrackTime,
                CaptureTime = captureTime,
                Codec = VideoCodec.H264
            });
        }

        if (_processorContext->SwapChain is not null)
            _processorContext->SwapChain->Present(0, 0)
                .ThrowIfFailed("IDxgiSwapchain->Present failed");
    }

    private void InitializeCaptureContext()
    {
        _captureContext->Device = _options.CaptureState.Device;
        _captureContext->DeviceContext = _options.CaptureState.DeviceContext;

        _captureContext->Device->AddRef();
        _captureContext->DeviceContext->AddRef();

        _captureContext->Adapter = D3DHelpers.GetDxgiAdapterFromD3DDevice(_captureContext->Device);
        D3DHelpers.TrySetHighestGpuPriority(_captureContext->Device);
    }

    private void InitializeProcessorContext()
    {
        CreateProcessorDevices();
        D3DHelpers.TrySetHighestGpuPriority(_processorContext->Device);
        CreateSharedTexture();
        OpenSharedTextureOnCaptureContext();
    }

    private void CreateProcessorDevices()
    {
        if (_options.PreviewWindowHandle == 0)
        {
            uint flags = (uint)D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_DEBUG;
            DirectX.D3D11CreateDevice(_captureContext->Adapter,
                    D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_UNKNOWN,
                    HMODULE.NULL,
                    flags,
                    null,
                    0,
                    D3D11.D3D11_SDK_VERSION,
                    &_processorContext->Device,
                    null,
                    &_processorContext->DeviceContext)
                .ThrowIfFailed("Failed to create processor device");
        }
        else
        {
            DXGI_SWAP_CHAIN_DESC swDesc = new DXGI_SWAP_CHAIN_DESC
            {
                BufferCount = 1,
                Flags = 0,
                Windowed = true,
                OutputWindow = _options.PreviewWindowHandle,
                SampleDesc = new DXGI_SAMPLE_DESC(1, 0),
                SwapEffect = DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_DISCARD,
                BufferUsage = DXGI.DXGI_USAGE_RENDER_TARGET_OUTPUT | DXGI.DXGI_USAGE_SHADER_INPUT,
                BufferDesc = new DXGI_MODE_DESC
                {
                    Width = _options.CaptureState.TextureWidth,
                    Height = _options.CaptureState.TextureHeight,
                    Format = _options.CaptureState.TextureFormat
                }
            };

            DirectX.D3D11CreateDeviceAndSwapChain(_captureContext->Adapter,
                    D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_UNKNOWN,
                    HMODULE.NULL,
                    0,
                    null,
                    0,
                    D3D11.D3D11_SDK_VERSION,
                    &swDesc,
                    &_processorContext->SwapChain,
                    &_processorContext->Device,
                    null,
                    &_processorContext->DeviceContext)
                .ThrowIfFailed("Failed to create processor device & swapchain");

            _processorContext->SwapChain->GetBuffer(0, Uuidof<ID3D11Texture2D>(),
                    (void**)&_processorContext->SwapChainBuffer)
                .ThrowIfFailed("Failed to get processor swapchain buffer");
        }
    }

    private void CreateSharedTexture()
    {
        D3D11_TEXTURE2D_DESC desc = new()
        {
            Width = _options.CaptureState.TextureWidth,
            Height = _options.CaptureState.TextureHeight,
            ArraySize = 1,
            BindFlags = (uint)(D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE | D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET),
            CPUAccessFlags = 0,
            Format = _options.CaptureState.TextureFormat,
            MipLevels = 1,
            MiscFlags = (uint)(D3D11_RESOURCE_MISC_FLAG.D3D11_RESOURCE_MISC_SHARED |
                               D3D11_RESOURCE_MISC_FLAG.D3D11_RESOURCE_MISC_SHARED_NTHANDLE),
            SampleDesc = new(1, 0),
            Usage = 0
        };

        _processorContext->Device->CreateTexture2D(&desc, null, &_processorContext->SharedTexture)
            .ThrowIfFailed("Failed to create shared texture");

        _processorContext->SharedTextureHandle = CreateSharedTextureHandle(_processorContext->SharedTexture);

        if (Log.IsDebugEnabled)
            Log.Debug("Created shared texture & handle");
    }

    private HANDLE CreateSharedTextureHandle(ID3D11Texture2D* texture)
    {
        IDXGIResource1* resource = null;

        try
        {
            texture->QueryInterface(Uuidof<IDXGIResource1>(), (void**)&resource)
                .ThrowIfFailed("Failed to get IDXGIResource1 from ID3D11Texture2D");

            HANDLE handle;
            resource->CreateSharedHandle(null, DXGI.DXGI_SHARED_RESOURCE_READ | DXGI.DXGI_SHARED_RESOURCE_WRITE, null,
                    &handle)
                .ThrowIfFailed("Failed to create shared texture handle");

            return handle;
        }
        finally
        {
            ReleaseAndNull(ref resource);
        }
    }

    private void OpenSharedTextureOnCaptureContext()
    {
        ID3D11Device1* device1 = null;

        try
        {
            _captureContext->Device->QueryInterface(Uuidof<ID3D11Device1>(), (void**)&device1)
                .ThrowIfFailed("Failed to get ID3D11Device1 from ID3D11Device");

            device1->OpenSharedResource1(_processorContext->SharedTextureHandle, Uuidof<ID3D11Texture2D>(),
                    (void**)&_captureContext->SharedTextureRef)
                .ThrowIfFailed("Failed to open shared texture on capture context");

            if (Log.IsDebugEnabled)
                Log.Debug("Opened D3D shared texture");
        }
        finally
        {
            ReleaseAndNull(ref device1);
        }
    }

    private void ProcessorThreadCleanup()
    {
        lock (_lockObject)
        {
            _textureReadyEvent.Dispose();
        
            ReleaseAndNull(ref _captureContext->SharedTextureRef);
            ReleaseAndNull(ref _captureContext->Device);
            ReleaseAndNull(ref _captureContext->DeviceContext);
            ReleaseAndNull(ref _captureContext->Adapter);

            ReleaseAndNull(ref _processorContext->DeviceContext);
            ReleaseAndNull(ref _processorContext->Device);
            ReleaseAndNull(ref _processorContext->SwapChain);
            ReleaseAndNull(ref _processorContext->SwapChainBuffer);
            ReleaseAndNull(ref _processorContext->SharedTexture);

            if (_processorContext->SharedTextureHandle != HANDLE.NULL)
            {
                TerraFX.Interop.Windows.Windows.CloseHandle(_processorContext->SharedTextureHandle);
                _processorContext->SharedTextureHandle = HANDLE.NULL;
            }

            NativeMemory.Free(_captureContext);
            NativeMemory.Free(_processorContext);

            _buffer.Dispose();
            _processorThreadCleanedUp = true;
        }
    }

    public void Process(D3DProcessArgs args)
    {
        if (!TryEnterLock())
        {
            Log.Error("Failed to enter AsyncD3DProcessor lock");
            return;
        }

        try
        {
            if (_processorReadyTaskSource.Task.IsFaulted)
                throw new AggregateException(_processorReadyTaskSource.Task.Exception!);

            if (!_processorReadyTaskSource.Task.IsCompleted)
                return;

            ObjectDisposedException.ThrowIf(_processorThreadCleanedUp, this);

            D3DHelpers.CopyResourceBlocking(_captureContext->Device, _captureContext->DeviceContext, args.Texture, _captureContext->SharedTextureRef);

            _captureTime = args.CaptureTime;

            if (_firstFrameTime == 0)
                _firstFrameTime = _captureTime;

            _textureReadyEvent.Set();
        }
        finally
        {
            Monitor.Exit(_lockObject);
        }
    }

    private bool TryEnterLock()
    {
        for (int i = 0; i < 20; i++)
        {
            ObjectDisposedException.ThrowIf(_processorThreadCleanedUp, this);

            if (Monitor.TryEnter(_lockObject, 100))
                return true;
        }

        return false;
    }

    public void Dispose()
    {
        lock (_lockObject)
        {
            if (_processorThreadCleanedUp)
                return;

            Log.Debug("Disposing!");
            _stop = true;

            _textureReadyEvent.Set();
        }

        _processorThread.Join();
        Log.Debug("Disposed!");
    }

    private struct ProcessorContext
    {
        public ID3D11Device* Device;
        public ID3D11DeviceContext* DeviceContext;

        public IDXGISwapChain* SwapChain;
        public ID3D11Texture2D* SwapChainBuffer;

        public ID3D11Texture2D* SharedTexture;
        public HANDLE SharedTextureHandle;
    }

    private struct CaptureContext
    {
        public ID3D11Device* Device;
        public ID3D11DeviceContext* DeviceContext;
        public IDXGIAdapter* Adapter;

        public ID3D11Texture2D* SharedTextureRef;
    }
}