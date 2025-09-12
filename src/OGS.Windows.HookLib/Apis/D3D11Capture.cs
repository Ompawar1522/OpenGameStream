using OGS.Windows.HookLib.Com;
using OGS.Windows.HookLib.Common;
using OGS.Windows.HookShared;
using OGS.Windows.HookShared.Ipc;
using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using ZeroLog;
using static TerraFX.Interop.Windows.Windows;

namespace OGS.Windows.HookLib.Apis;

internal sealed unsafe class D3D11Capture : IDisposable
{
    private static readonly Log Log = LogManager.GetLogger<D3D11Capture>();

    private readonly IpcClient _ipcClient;
    private readonly Lock _lock = new Lock();
    private readonly State* _state = (State*)NativeMemory.AllocZeroed((nuint)sizeof(State));
    private readonly Win32NamedEvent _frameEvent;

    private readonly ComHooks.IdxgiSwapChainPresentDelegate _customPresent;
    private readonly ComHooks.IUnknownReleaseDelegate _customRelease;
    private readonly ComHooks.IdxgiSwapChainResizeBuffersDelegate _customResizeBuffers;

    private volatile bool _disposing;
    private volatile bool _capturing;

    public D3D11Capture(IpcClient ipcClient)
    {
        _ipcClient = ipcClient;

        _customPresent = CustomPresent;
        _customRelease = CustomRelease;
        _customResizeBuffers = CustomResizeBuffers;
        _frameEvent = Win32NamedEvent.Open("Local\\OGS_FRAME_READY");

        Log.Info("Hooking d3d11");

        try
        {
            using (_lock.EnterScope())
            {
                ComHooks.HookSwapChainPresent(_customPresent);
                ComHooks.HookIUknownRelease(_customRelease);
                ComHooks.HookSwapChainResizeBuffers(_customResizeBuffers);
            }
        }catch(Exception ex)
        {
            Log.Error("Failed to hook d3d11", ex);
            Dispose();
            throw;
        }
    }

    private void InitializeState(IDXGISwapChain* swapChain)
    {
        try
        {
            Log.Info("Initializing D3D state");

            _state->SwapChainRef = swapChain;
            _state->SwapChainRef->AddRef();

            _state->SwapChainRef->GetBuffer(0, __uuidof<ID3D11Texture2D>(), (void**)&_state->SwapChainBuffer)
                .ThrowIfFailed("GetBuffer failed");

            ID3D11Texture2D* temp = default;

            _state->SwapChainRef->GetDevice(__uuidof<ID3D11Device>(), (void**)&_state->Device)
                .ThrowIfFailed("GetDevice failed");

            _state->Device->GetImmediateContext(&_state->DeviceContext);
            D3DHelpers.TrySetHighestGpuPriority(_state->Device);

            D3D11_TEXTURE2D_DESC desc;
            _state->SwapChainBuffer->GetDesc(&desc);

            desc.MiscFlags = (uint)(D3D11_RESOURCE_MISC_FLAG.D3D11_RESOURCE_MISC_SHARED);
            desc.BindFlags = (uint)D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE;

            _state->Device->CreateTexture2D(&desc, null, &_state->SharedTexture)
                .ThrowIfFailed("CreateTexture2D failed");

            CreateSharedTextureHandle();

            Log.Info($"Initialized capture state: {desc.Width}:{desc.Height} as {desc.Format}");

            _ipcClient.TrySendCreateTexture(new IpcCreateTextureData
            {
                Width = (int)desc.Width,
                Height = (int)desc.Height,
                Format = desc.Format,
                Handle = _state->SharedTextureHandle
            });

            _capturing = true;
            Log.Info("Initialized D3D state");
        }
        catch(Exception ex)
        {
            Log.Error("Failed to initialize D3D11 state", ex);
            ReleaseState();
        }
    }

    private void CreateSharedTextureHandle()
    {
        IDXGIResource* resource = null;

        try
        {
            _state->SharedTexture->QueryInterface(__uuidof<IDXGIResource>(), (void**)&resource)
                .ThrowIfFailed("Failed to get IDXGIResource from SwapChain buffer");

            resource->GetSharedHandle(&_state->SharedTextureHandle)
                .ThrowIfFailed("Failed to create shared texture handle");

            Log.Info("Created shared texture handle");
        }
        finally
        {
            resource->Release();
        }
    }

    private void ReleaseState()
    {
        Log.Info("Releasing D3D11 objects");

        ReleaseAndNull(ref _state->SwapChainBuffer);

        if (_state->SwapChainRef is not null)
        {
            if (ComHooks.OriginalReleaseDelegate is not null)
            {
                ComHooks.OriginalReleaseDelegate((IUnknown*)_state->SwapChainRef);
            }
            else
            {
                _state->SwapChainRef->Release();
            }

            _state->SwapChainRef = null;
        }

        ReleaseAndNull(ref _state->SharedTexture);
        ReleaseAndNull(ref _state->SwapChainRef);
        ReleaseAndNull(ref _state->Device);
        ReleaseAndNull(ref _state->DeviceContext);
        ReleaseAndNull(ref _state->SharedTexture);
        CloseHandle(_state->SharedTextureHandle);

        _ipcClient.TrySendDestroyTexture();

        _capturing = false;

        Log.Info($"Released D3D11 objects");
    }

    private HRESULT CustomPresent(IDXGISwapChain* _this, uint a, uint b)
    {
        using (_lock.EnterScope())
        {
            if (_disposing)
                return ComHooks.OriginalSwapChainPresentDelegate(_this, a, b);

            /*if (Log.IsTraceEnabled)
                Log.Trace($"Present: {a}, {b}");*/

            if (!_capturing)
            {
                InitializeState(_this);
            }

            if(_state->Device is not null)
            {
                UpdateSharedTexture();
            }

            return ComHooks.OriginalSwapChainPresentDelegate(_this, a, b);
        } 
    }

    private void UpdateSharedTexture()
    {
        _state->DeviceContext->CopyResource((ID3D11Resource*)_state->SharedTexture, (ID3D11Resource*)_state->SwapChainBuffer);
        _frameEvent.Set();
    }

    private HRESULT CustomResizeBuffers(IDXGISwapChain* @this, uint bufferCount, uint width, uint height, DXGI_FORMAT format, uint flags)
    {
        using (_lock.EnterScope())
        {
            if (_disposing || !_capturing)
                return ComHooks.OriginalSwapChainResizeBuffersDelegate(@this, bufferCount, width, height, format, flags);

            ReleaseState();
            var realResult = ComHooks.OriginalSwapChainResizeBuffersDelegate(@this, bufferCount, width, height, format, flags);
            Log.Info($"ResizeBuffers -> {bufferCount}x{width}x{height} as {format}");

            return realResult;
        }
    }

    private uint CustomRelease(IUnknown* _this)
    {
        using (_lock.EnterScope())
        {
            uint refCount = ComHooks.OriginalReleaseDelegate(_this);

            if (_disposing || !_capturing)
                return refCount;

            if(_this == _state->SwapChainRef)
            {
                if (refCount == 1 || refCount == 2)
                    ReleaseState();
            }

            return refCount;
        }
    }

    public void Dispose()
    {
        using(_lock.EnterScope())
        {
            _disposing = true;

            Log.Info("Unhooking d3d11");
            ComHooks.RestoreSwapChainResizeTargets();
            ComHooks.RestoreSwapChainResizeBuffers();
            ComHooks.RestoreSwapChainGetBuffer();
            ComHooks.RestoreSwapChainPresent();
            ComHooks.RestoreIUnknownRelease();
            Log.Info("Unhooked d3d11");
            _frameEvent?.Dispose();

            ReleaseState();
        }
    }

    private struct State
    {
        public IDXGISwapChain* SwapChainRef;
        public ID3D11Texture2D* SwapChainBuffer;

        public ID3D11Texture2D* SharedTexture;
        public HANDLE SharedTextureHandle;

        public ID3D11Device* Device;
        public ID3D11DeviceContext* DeviceContext;
    }
}
