using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace OGS.Windows.Extensions;

public static class HMonitorExtensions
{
    public static unsafe bool TryGetName(this HMONITOR hMonitor, out string name)
    {
        MONITORINFOEXW* info = stackalloc MONITORINFOEXW[1];
        info->Base.cbSize = (uint)sizeof(MONITORINFOEXW);

        if (!GetMonitorInfoW(hMonitor, (MONITORINFO*)info))
        {
            name = string.Empty;
            return false;
        }

        name = new string(&info->szDevice.e0);
        return true;
    }
}
