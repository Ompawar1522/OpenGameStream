using OGS.Core.Common.Input;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace OGS.Windows.Input;

internal static class WinSendInput
{
    private static readonly Log Log = LogManager.GetLogger(typeof(WinSendInput));
    
    public static void SendMouseScroll(MouseScrollDirection direction)
    {
        int mouseData = 0;

        if (direction == MouseScrollDirection.Up)
            mouseData = 120;
        else if (direction == MouseScrollDirection.Down)
            mouseData = -120;
        
        //Todo - mousedata is negative, but also uint?
        SendMouseInput(0, 0, (uint)mouseData, MOUSEEVENTF.MOUSEEVENTF_WHEEL);
    }
    
    public static unsafe void SendKeyboardKey(int key, bool pressed)
    {
        //Try to convert virtual key code to scan key. scan codes are injected lower into the input stack, and some
        //games don't receive virtual key code events
        uint scan = TerraFX.Interop.Windows.Windows.MapVirtualKey((uint)key, TerraFX.Interop.Windows.Windows.MAPVK_VK_TO_VSC);
        
        if(scan == 0)
        {
            Log.Warn($"Failed to map vkey {key} to scan code");
            SendKeyboardInput((ushort)key, 0, (uint)(pressed ? 0 : TerraFX.Interop.Windows.Windows.KEYEVENTF_KEYUP));
        }
        else
        { 
            SendKeyboardInput(0, (ushort)scan, (uint)(pressed ?  TerraFX.Interop.Windows.Windows.KEYEVENTF_SCANCODE :
                TerraFX.Interop.Windows.Windows.KEYEVENTF_KEYUP |  TerraFX.Interop.Windows.Windows.KEYEVENTF_SCANCODE));
        }
    }
    
    public static void SendMouseButton(MouseButton button, bool pressed)
    {
        uint dwFlags = button switch
        {
            MouseButton.Left => (uint)(pressed ? MOUSEEVENTF.MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF.MOUSEEVENTF_LEFTUP),
            MouseButton.Right => (uint)(pressed ? MOUSEEVENTF.MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF.MOUSEEVENTF_RIGHTUP),
            MouseButton.Middle =>
                (uint)(pressed ? MOUSEEVENTF.MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF.MOUSEEVENTF_MIDDLEUP),
            MouseButton.X1 => (uint)(pressed ? MOUSEEVENTF.MOUSEEVENTF_XDOWN : MOUSEEVENTF.MOUSEEVENTF_XUP),
            MouseButton.X2 => (uint)(pressed ? MOUSEEVENTF.MOUSEEVENTF_XDOWN : MOUSEEVENTF.MOUSEEVENTF_XUP),
            _ => 0
        };

        uint mouseData = 0;

        if (button == MouseButton.X1)
            mouseData |= 0x0001;
        else if (button == MouseButton.X2)
            mouseData |= 0x0002;

        SendMouseInput(0, 0, mouseData, dwFlags);
    }
    
    public static void SendMouseInput(int dx, int dy, uint mouseData, uint dwFlags, uint time = 0, UIntPtr dwExtraInfo = 0)
    {
        CallSendInput(new INPUT()
        {
            mi = new MOUSEINPUT
            {
                dx = dx,
                dy = dy,
                dwExtraInfo = dwExtraInfo,
                time = time,
                mouseData = mouseData,
                dwFlags = dwFlags,
            },
            type = INPUT.INPUT_MOUSE
        });
    }
    
    public static void SendKeyboardInput(ushort wVk, ushort wScan, uint dwFlags, uint time = 0, UIntPtr dwExtraInfo = 0)
    {
        CallSendInput(new INPUT()
        {
            ki = new KEYBDINPUT()
            {
                wVk = wVk,
                wScan = wScan,
                dwFlags = dwFlags,
                time = time,
                dwExtraInfo = dwExtraInfo
            },
            type = INPUT.INPUT_KEYBOARD
        });
    }
    
    private static readonly int InputSize = Marshal.SizeOf<INPUT>();
    private static void CallSendInput(INPUT input)
    {
        unsafe
        {
            if (TerraFX.Interop.Windows.Windows.SendInput(1, &input, InputSize) != 0)
            {
                //Todo - on failure?
            }
        }
    }
}