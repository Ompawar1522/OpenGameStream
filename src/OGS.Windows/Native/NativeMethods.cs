using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace OGS.Windows.Native;
internal static class NativeMethods
{
    [DllImport("gdi32.dll")]
    public static extern HRESULT D3DKMTSetProcessSchedulingPriorityClass(nint processHandle, D3DkmtSchedulingpriorityclass cls);
    [DllImport("gdi32.dll")]
    public static extern HRESULT D3DKMTGetProcessSchedulingPriorityClass(nint processHandle,out  D3DkmtSchedulingpriorityclass cls);

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern HRESULT NtSetTimerResolution(int desiredResolution, bool setResolution, out int currentResolution);

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
