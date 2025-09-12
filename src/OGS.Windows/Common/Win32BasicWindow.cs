using System.ComponentModel;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace OGS.Windows.Common;

public sealed unsafe class Win32BasicWindow
{
    public HWND Handle { get; private set; }

    private readonly TaskCompletionSource _handleCreatedTask = new();
    private readonly Thread _windowThread;
    private static delegate* unmanaged<HWND, uint, WPARAM, LPARAM, LRESULT> _wndProcDelegate;

    public Win32BasicWindow()
    {
        _wndProcDelegate = &WndProc;

        _windowThread = new Thread(WindowThreadStart);
        _windowThread.Name = "NativeWindowThread";
        _windowThread.SetApartmentState(ApartmentState.STA);
        _windowThread.Start();

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        _handleCreatedTask.Task.Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
    }

    private void WindowThreadStart()
    {
        try
        {
            CreateHandle();
            _handleCreatedTask.TrySetResult();
        }
        catch (Exception ex)
        {
            _handleCreatedTask.TrySetException(ex);
            return;
        }

        MessageLoop();
    }

    private void MessageLoop()
    {
        MSG msg;

        while (GetMessage(&msg, HWND.NULL, 0, 0) > 0)
        {
            TranslateMessage(&msg);
            DispatchMessage(&msg);
        }
    }

    private void CreateHandle()
    {
        fixed (char* windowClassName = "OGS_CLS_" + new Random().Next())
        {
            WNDCLASSEXW windowClass = new WNDCLASSEXW();
            windowClass.cbSize = (uint)sizeof(WNDCLASSEXW);
            windowClass.style = CS.CS_HREDRAW | CS.CS_VREDRAW;
            _wndProcDelegate = &WndProc;
            windowClass.lpfnWndProc = _wndProcDelegate;
            windowClass.hInstance = GetModuleHandleW(null);
            windowClass.lpszClassName = windowClassName;

            if (RegisterClassExW(&windowClass) == 0)
            {
                throw new Win32Exception();
            }

            fixed (char* name = "OGS_" + new Random().Next())
            {
                // Create the window
                HWND hwnd = CreateWindowExW(
                    0,
                    windowClassName,
                    name,
                    WS.WS_OVERLAPPEDWINDOW,
                    CW_USEDEFAULT,
                    CW_USEDEFAULT,
                    500,
                    500,
                    default,
                    default,
                    windowClass.hInstance,
                    null
                );

                if (hwnd.Value is null)
                {
                    throw new Win32Exception();
                }

                UpdateWindow(hwnd);
                Handle = hwnd;
            }
        }
    }

    [UnmanagedCallersOnly]
    private static LRESULT WndProc(HWND hwnd, uint uMsg, WPARAM wParam, LPARAM lParam)
    {
        switch (uMsg)
        {
            case WM.WM_DESTROY:
                PostQuitMessage(0);
                return 0;
        }

        return DefWindowProcW(hwnd, uMsg, wParam, lParam);
    }

    public void Show() => ShowWindow(Handle, 5);
    public void Hide() => ShowWindow(Handle, 0);

    public void Dispose()
    {
        if (Handle != nint.Zero)
        {
            PostMessage(Handle, WM.WM_CLOSE, 0, 0);
            _windowThread.Join();
            Handle = HWND.NULL;
        }
    }
}