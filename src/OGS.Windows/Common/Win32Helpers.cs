using OGS.Windows.Native;
using System.ComponentModel;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;


namespace OGS.Windows.Common;

public static class Win32Helpers
{
    private static readonly Log Log = LogManager.GetLogger(typeof(Win32Helpers));

    public static void ThrowWin32Exception(string message)
    {
        throw new Win32Exception(message, new Win32Exception());
    }

    public static HMONITOR GetPrimaryMonitor()
    {
        return MonitorFromPoint(default, 0);
    }
    
    public static unsafe void SetMinimumTimerResolution()
    {
        if (DwmEnableMMCSS(true).FAILED)
            Log.Warn("DwmEnableMMCSS failed");

        uint max, min, current;
        HRESULT hr = NtQueryTimerResolution(&max, &min, &current);

        if (hr.SUCCEEDED)
        {
            if (NativeMethods.NtSetTimerResolution((int)min, true, out int _).FAILED)
            {
                Log.Warn("NtSetTimerResolution failed, using timeBeginPeriod");

                timeBeginPeriod(1);
            }
            else
            {
                Log.Debug($"Timer resolution set to {min}");
            }
        }
        else
        {
            Log.Warn("NtQueryTimerResolution failed");

            if (NativeMethods.NtSetTimerResolution(5000, true, out int _).FAILED)
            {
                Log.Warn("NtSetTimerResolution failed, using timeBeginPeriod");

                timeBeginPeriod(1);
            }
        }
    }
}