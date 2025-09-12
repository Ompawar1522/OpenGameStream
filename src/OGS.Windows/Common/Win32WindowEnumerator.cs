using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace OGS.Windows.Common;

public static unsafe class Win32WindowEnumerator
{
    public delegate void WindowEnumCallback(HWND hWnd);

    private ref struct WindowEnumeratorState
    {
        public nint Callback;
    }

    public static void EnumerateWindows(WindowEnumCallback callback)
    {
        GCHandle callbackHandle = GCHandle.Alloc(callback);

        try
        {
            WindowEnumeratorState state = new WindowEnumeratorState
            {
                Callback = Marshal.GetFunctionPointerForDelegate(callback)
            };

            if (!EnumWindows(&EnumWindowCallback, (LPARAM)Unsafe.AsPointer(ref state)))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        finally
        {
            callbackHandle.Free();
        }
    }

    [UnmanagedCallersOnly]
    private static BOOL EnumWindowCallback(HWND hWnd, LPARAM lp)
    {
        WindowEnumeratorState* state = (WindowEnumeratorState*)lp;
        WindowEnumCallback callback = Marshal.GetDelegateForFunctionPointer<WindowEnumCallback>(state->Callback);

        callback(hWnd);
        return true;
    }
}
