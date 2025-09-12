using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace OGS.Windows.Common;

public static unsafe class Win32DisplayEnumerator
{
    public delegate void DisplayEnumeratorCallback(HMONITOR hMonitor);

    private ref struct DisplayEnumeratorState
    {
        public nint Callback;
    }

    public static void EnumerateDisplays(DisplayEnumeratorCallback callback)
    {
        GCHandle callbackHandle = GCHandle.Alloc(callback);

        try
        {
            DisplayEnumeratorState state = new DisplayEnumeratorState
            {
                Callback = Marshal.GetFunctionPointerForDelegate(callback)
            };

            if (!EnumDisplayMonitors(HDC.NULL, null, &EnumDisplayCallback, (LPARAM)Unsafe.AsPointer(ref state)))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        finally
        {
            callbackHandle.Free();
        }
    }

    [UnmanagedCallersOnly]
    private static BOOL EnumDisplayCallback(HMONITOR hMonitor, HDC hdcMonitor, RECT* lprcMonitor, LPARAM dwData)
    {
        DisplayEnumeratorState* state = (DisplayEnumeratorState*)dwData;
        DisplayEnumeratorCallback callback = Marshal.GetDelegateForFunctionPointer<DisplayEnumeratorCallback>(state->Callback);

        callback(hMonitor);
        return true;
    }
}