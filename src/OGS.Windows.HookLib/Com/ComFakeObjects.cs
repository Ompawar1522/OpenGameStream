using OGS.Windows.HookLib.Common;
using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;


namespace OGS.Windows.HookLib.Com;

public static unsafe class ComFakeObject
{
    private unsafe struct FakeObjects
    {
        public IDXGISwapChain* SwapChain;
        public ID3D11Device* Device;
        public ID3D11DeviceContext* DeviceContext;
    }

    private static readonly FakeObjects* Objects = (FakeObjects*)NativeMemory.AllocZeroed((nuint)Marshal.SizeOf<FakeObjects>());

    private static NativeWindowThread? _fakeWindow;

    public static IDXGISwapChain* GetFakeSwapChain()
    {
        if (Objects->SwapChain is not null)
            return Objects->SwapChain;

        CreateDeviceAndSwapChain();
        return Objects->SwapChain;
    }

    private static void CreateDeviceAndSwapChain()
    {
        DXGI_SWAP_CHAIN_DESC swDesc = new DXGI_SWAP_CHAIN_DESC();
        swDesc.BufferCount = 1;
        swDesc.Flags = 0;
        swDesc.Windowed = true;
        swDesc.OutputWindow = (HWND)GetWindowHandle();
        swDesc.SampleDesc = new DXGI_SAMPLE_DESC(1, 0);
        swDesc.SwapEffect = DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_DISCARD;
        swDesc.BufferUsage = DXGI.DXGI_USAGE_SHADER_INPUT;
        swDesc.BufferDesc = new DXGI_MODE_DESC();
        swDesc.BufferDesc.Width = 500;
        swDesc.BufferDesc.Height = 500;
        swDesc.BufferDesc.Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM;

        DirectX.D3D11CreateDeviceAndSwapChain(null, D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE, HMODULE.NULL,
            0, null, 0, D3D11.D3D11_SDK_VERSION, &swDesc, &Objects->SwapChain, &Objects->Device, null, &Objects->DeviceContext).ThrowIfFailed();
    }

    private static readonly object WindowLock = new();
    public static nint GetWindowHandle()
    {
        lock (WindowLock)
        {
            if (_fakeWindow is null)
            {
                _fakeWindow = new NativeWindowThread();
            }
        }

        return _fakeWindow.Handle;
    }

    public static void ReleaseObjects()
    {
        ReleaseAndNull(ref Objects->SwapChain);
        ReleaseAndNull(ref Objects->Device);
        ReleaseAndNull(ref Objects->DeviceContext);
        _fakeWindow?.Dispose();
    }
}
