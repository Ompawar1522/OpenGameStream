using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using ZeroLog;

namespace OGS.Windows.HookLib.Common;
internal class D3DHelpers
{
    private static readonly Log Log = LogManager.GetLogger<D3DHelpers>();

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

        hr = D3DKMTSetProcessSchedulingPriorityClass(TerraFX.Interop.Windows.Windows.GetCurrentProcess(),
            D3DkmtSchedulingpriorityclass.D3DkmtSchedulingpriorityclassRealtime);

        if (hr.SUCCEEDED)
            Log.Info("D3DKMTSetProcessSchedulingPriorityClass realtime ok");
        else
            Log.Error("D3DKMTSetProcessSchedulingPriorityClass realtime failed", Marshal.GetExceptionForHR(hr));

        dxgiDevice->Release();
    }

    [DllImport("gdi32.dll")]
    public static extern HRESULT D3DKMTSetProcessSchedulingPriorityClass(nint processHandle, D3DkmtSchedulingpriorityclass cls);
    [DllImport("gdi32.dll")]
    public static extern HRESULT D3DKMTGetProcessSchedulingPriorityClass(nint processHandle, out D3DkmtSchedulingpriorityclass cls);



    public enum D3DkmtSchedulingpriorityclass
    {
        D3DkmtSchedulingpriorityclassIdle,
        D3DkmtSchedulingpriorityclassBelowNormal,
        D3DkmtSchedulingpriorityclassNormal,
        D3DkmtSchedulingpriorityclassAboveNormal,
        D3DkmtSchedulingpriorityclassHigh,
        D3DkmtSchedulingpriorityclassRealtime
    }
}
