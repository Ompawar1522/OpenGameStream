using System.Buffers;
using System.Runtime.InteropServices;
using OGS.Core.Common;
using OGS.Core.Common.Video;
using OGS.Windows.Common;
using OGS.Windows.Video.Encode;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace OGS.Windows.Video.Processing;

public sealed unsafe class BasicD3DProcessor : ID3DProcessor
{
    private static readonly Log Log = LogManager.GetLogger<BasicD3DProcessor>();

    private readonly IMemoryOwner<byte> _encodeBuffer = MemoryPool<byte>.Shared.Rent(1024 * 1024 * 4);

    private readonly BasicD3DProcessorOptions _options;
    private ID3DEncoder? _encoder;
    private State* _state = MemoryHelper.AllocZeroed<State>();

    private long _firstFrameTime = 0;

    public BasicD3DProcessor(BasicD3DProcessorOptions options)
    {
        _options = options;

        try
        {
            InitializeState();
        }
        catch (Exception)
        {
            Dispose();
            throw;
        }
    }

    private void InitializeState()
    {
        _state->CaptureDevice = _options.CaptureState.Device;
        _state->CaptureDeviceContext = _options.CaptureState.DeviceContext;

        _state->CaptureDevice->AddRef();
        _state->CaptureDeviceContext->AddRef();

        _state->CaptureDevice->QueryInterface(Uuidof<IDXGIDevice>(), (void**)&_state->DxgiDevice)
            .ThrowIfFailed("QI For IDXGIDevice failed");

        _state->DxgiDevice->GetParent(Uuidof<IDXGIAdapter>(), (void**)&_state->DxgiAdapter)
            .ThrowIfFailed("GetParent on IDXGIAdapter failed");

        D3DHelpers.TrySetHighestGpuPriority(_state->CaptureDevice);

        _encoder = _options.EncoderFactory(_state->CaptureDevice);

        if (_options.PreviewWindowHandle != 0)
        {
            Log.Info("Creating swapchain...");
            CreateSwapChain();
        }

        Log.Info("Created texture processor state");
    }

    private void CreateSwapChain()
    {
        _state->DxgiAdapter->GetParent(Uuidof<IDXGIFactory>(), (void**)&_state->DxgiFactory)
            .ThrowIfFailed("GetParent on IDXGIFactory failed");

        DXGI_SWAP_CHAIN_DESC swDesc = new DXGI_SWAP_CHAIN_DESC();
        swDesc.BufferCount = 1;
        swDesc.Flags = 0;
        swDesc.Windowed = true;
        swDesc.OutputWindow = _options.PreviewWindowHandle;
        swDesc.SampleDesc = new DXGI_SAMPLE_DESC(1, 0);
        swDesc.SwapEffect = 0;
        swDesc.BufferUsage = DXGI.DXGI_USAGE_RENDER_TARGET_OUTPUT | DXGI.DXGI_USAGE_SHADER_INPUT;
        swDesc.BufferDesc = new DXGI_MODE_DESC();
        swDesc.BufferDesc.Width = _options.CaptureState.TextureWidth;
        swDesc.BufferDesc.Height = _options.CaptureState.TextureHeight;
        swDesc.BufferDesc.Format = _options.CaptureState.TextureFormat;

        Log.Info("Using backbuffer format " + swDesc.BufferDesc.Format);

        _state->DxgiFactory->CreateSwapChain((IUnknown*)_state->CaptureDevice,
                &swDesc, &_state->SwapChain)
            .ThrowIfFailed("CreateSwapChain failed");

        _state->SwapChain->GetBuffer(0, Uuidof<ID3D11Texture2D>(), (void**)&_state->SwapChainBuffer)
            .ThrowIfFailed("GetBuffer failed");
    }

    public void Process(D3DProcessArgs args)
    {
        try
        {
            ID3D11Texture2D* encoderTexture = args.Texture;

            if (_state->SwapChainBuffer is not null)
            {
                _state->CaptureDeviceContext->CopyResource((ID3D11Resource*)_state->SwapChainBuffer,
                        (ID3D11Resource*)args.Texture);
            }

            TimeSpan trackTimestamp = default;

            if (_firstFrameTime == 0)
                _firstFrameTime = args.CaptureTime;
            else
                trackTimestamp = TimeSpan.FromTicks(_firstFrameTime = args.CaptureTime);

            uint size = _encoder!.Encode(new D3DEncodeArgs
            {
                Buffer = _encodeBuffer.Memory,
                Keyframe = _options.KeyFrameSelector(),
                Texture = encoderTexture,
                Timestamp = trackTimestamp
            });

            _options.OnEncodedCallback(new EncodedVideoFrame
            {
                Data = _encodeBuffer.Memory.Span.Slice(0, (int)size),
                Timestamp = trackTimestamp,
                CaptureTime = args.CaptureTime,
                Codec = VideoCodec.H264
            });

            if (_state->SwapChain is not null)
            {
                _state->SwapChain->Present(0, 0);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Failed to process D3D texture", ex);
        }
    }

    public void Dispose()
    {
        _encodeBuffer.Dispose();

        _encoder?.Dispose();
        _encoder = null!;

        if (_state is not null)
        {
            ReleaseAndNull(ref _state->CaptureDevice);
            ReleaseAndNull(ref _state->CaptureDeviceContext);

            ReleaseAndNull(ref _state->DxgiDevice);
            ReleaseAndNull(ref _state->DxgiAdapter);
            ReleaseAndNull(ref _state->DxgiFactory);

            ReleaseAndNull(ref _state->SwapChain);
            ReleaseAndNull(ref _state->SwapChainBuffer);
        }

        NativeMemory.Free(_state);
        _state = null;
    }

    private struct State
    {
        public ID3D11Device* CaptureDevice;
        public ID3D11DeviceContext* CaptureDeviceContext;

        public IDXGISwapChain* SwapChain;
        public ID3D11Texture2D* SwapChainBuffer;

        public IDXGIDevice* DxgiDevice;
        public IDXGIAdapter* DxgiAdapter;
        public IDXGIFactory* DxgiFactory;
    }
}
