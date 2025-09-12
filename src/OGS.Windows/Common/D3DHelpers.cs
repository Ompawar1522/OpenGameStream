using OGS.Windows.Native;
using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;

namespace OGS.Windows.Common;

public static class D3DHelpers
{
    private static readonly Log Log = LogManager.GetLogger(typeof(D3DHelpers));

    public static unsafe void EnableDeviceThreadProtection(ID3D11Device* device)
    {
        ID3D11Multithread* mt = null;

        try
        {
            device->QueryInterface(Uuidof<ID3D11Multithread>(), (void**)&mt)
                .ThrowIfFailed("QI for ID3D11Multithread failed");

            mt->SetMultithreadProtected(true);
        }
        finally
        {
            ReleaseAndNull(ref mt);
        }
    }
    
    public static unsafe void TrySetHighestGpuPriority(ID3D11Device* device)
    {
        Log.Info("Setting gpu highest priority");

        IDXGIDevice1* dxgiDevice;
        HRESULT hr = device->QueryInterface(Uuidof<IDXGIDevice1>(), (void**)&dxgiDevice);

        if (hr.FAILED)
        {
            Log.Error("QI for IDXGIDevice1 failed");
            return;
        }

        hr = dxgiDevice->SetGPUThreadPriority(7);

        if (hr.SUCCEEDED)
            Log.Info("SetGPUThreadPriority 7 ok");
        else
            Log.Error("SetGpuThreadPriority 7 failed", Marshal.GetExceptionForHR(hr));

        hr = dxgiDevice->SetMaximumFrameLatency(1);

        if (hr.SUCCEEDED)
            Log.Info("SetMaximumFrameLatency 1 ok");
        else
            Log.Error("SetMaximumFrameLatency 1 failed", Marshal.GetExceptionForHR(hr));

        hr = NativeMethods.D3DKMTSetProcessSchedulingPriorityClass(TerraFX.Interop.Windows.Windows.GetCurrentProcess(),
            NativeMethods.D3DkmtSchedulingpriorityclass.D3DkmtSchedulingpriorityclassRealtime);

        if (hr.SUCCEEDED)
            Log.Info("D3DKMTSetProcessSchedulingPriorityClass realtime ok");
        else
            Log.Error("D3DKMTSetProcessSchedulingPriorityClass realtime failed", Marshal.GetExceptionForHR(hr));

        dxgiDevice->Release();
    }

    public static unsafe string? GetAdapterName(IDXGIAdapter1* adapter)
    {
        DXGI_ADAPTER_DESC1 desc;
        HRESULT hr = adapter->GetDesc1(&desc);

        if (hr.FAILED)
            return null;

        return new string(&desc.Description.e0);
    }

    public static unsafe string? GetGpuDeviceName(ID3D11Device* d3d11Device)
    {
        IDXGIDevice* device = default;
        IDXGIAdapter* adapter = default;
        IDXGIAdapter1* adapter1 = default;

        try
        {
            d3d11Device->QueryInterface(Uuidof<IDXGIDevice>(), (void**)&device)
                .ThrowIfFailed("QI for IDXGIDevice failed");

            device->GetAdapter(&adapter)
                .ThrowIfFailed("GetAdapter failed");

            adapter->QueryInterface(Uuidof<IDXGIAdapter1>(), (void**)&adapter1)
                .ThrowIfFailed("QI for IDXGIAdapter1 failed");

            DXGI_ADAPTER_DESC1 desc1;
            adapter1->GetDesc1(&desc1)
                .ThrowIfFailed("GetDesc failed");

            string deviceName = new string(&desc1.Description.e0);

            return deviceName;
        }
        catch (Exception ex)
        {
            Log.Error("Failed to get GPU device name", ex);
            return null;
        }
        finally
        {
            ReleaseAndNull(ref adapter1);
            ReleaseAndNull(ref adapter);
            ReleaseAndNull(ref device);
        }
    }

    public static unsafe HRESULT ConvertDeviceToWinRt(ID3D11Device* device, IDirect3DDevice** ptr)
    {
        IDXGIDevice* dxgiDevice = null;

        try
        {
            HRESULT hr = device->QueryInterface(Uuidof<IDXGIDevice>(), (void**)&dxgiDevice);

            if (hr.FAILED)
                return hr;

            return ConvertDeviceToWinRt(dxgiDevice, ptr);
        }
        finally
        {
            ReleaseAndNull(ref dxgiDevice);
        }
    }

    public static unsafe HRESULT ConvertDeviceToWinRt(IDXGIDevice* dxgiDevice, IDirect3DDevice** ptr)
    {
        return WinRT.CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice, (IInspectable**)ptr);
    }

    public static unsafe IDXGIAdapter* GetDxgiAdapterFromD3DDevice(ID3D11Device* device)
    {
        IDXGIDevice* dDevice = null;
        IDXGIAdapter* adapter;

        try
        {
            device->QueryInterface(Uuidof<IDXGIDevice>(), (void**)&dDevice)
                .ThrowIfFailed("Failed to get IDXGIDevice from ID3D11Device");

            dDevice->GetAdapter(&adapter)
                .ThrowIfFailed("Failed to get IDXGIAdapter from IDXGIDevice");

            return adapter;
        }
        finally
        {
            ReleaseAndNull(ref dDevice);
        }
    }

    /// <summary>
    /// Copies a texture to another texture synchronously
    /// </summary>
    /// <param name="device"></param>
    /// <param name="deviceContext"></param>
    /// <param name="src"></param>
    /// <param name="dst"></param>
    public static unsafe void CopyResourceBlocking(ID3D11Device* device, ID3D11DeviceContext* deviceContext, ID3D11Texture2D* src, ID3D11Texture2D* dst)
    {
        ID3D11Query* query = default;

        try
        {
            D3D11_QUERY_DESC queryDesc = new D3D11_QUERY_DESC
            {
                Query = D3D11_QUERY.D3D11_QUERY_EVENT
            };

            device->CreateQuery(&queryDesc, &query).ThrowIfFailed();

            deviceContext->CopyResource(
                (ID3D11Resource*)dst,
                (ID3D11Resource*)src);

            deviceContext->Flush();
            deviceContext->End((ID3D11Asynchronous*)query);

            while (deviceContext->GetData((ID3D11Asynchronous*)query, null, 0, 0) != S.S_OK)
            {
                Thread.SpinWait(1);
            }
        }
        finally
        {
            ReleaseAndNull(ref query);
        }
    }
}