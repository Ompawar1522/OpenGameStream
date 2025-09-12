using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace OGS.Windows.Extensions;

public static class HwndExtensions
{
    public static unsafe bool TryGetTitle(this HWND hWnd, out string title)
    {
        int len = GetWindowTextLength(hWnd);
        title = null!;

        if (len == 0)
            return false;

        char* buffer = stackalloc char[512];
        len = GetWindowText(hWnd, buffer, 512);

        title = new string(buffer, 0, len);
        return !string.IsNullOrWhiteSpace(title);
    }

    public static uint GetProcessId(this HWND hWnd)
    {
        unsafe
        {
            uint pid = 0;
            GetWindowThreadProcessId(hWnd, &pid);
            return pid;
        }
    }
}
