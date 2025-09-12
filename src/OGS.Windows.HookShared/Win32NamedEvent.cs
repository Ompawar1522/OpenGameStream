using System.ComponentModel;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace OGS.Windows.HookShared;

public sealed class Win32NamedEvent : IDisposable
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr OpenEvent(
    uint dwDesiredAccess,
    bool bInheritHandle,
    string lpName);

    public const uint EVENT_MODIFY_STATE = 0x0002;
    public const uint SYNCHRONIZE = 0x00100000;

    public string EventName { get; }
    public HANDLE Handle { get; }

    private Win32NamedEvent(HANDLE handle, string name)
    {
        EventName = name;
        Handle = handle;
    }

    public static Win32NamedEvent Create(string name)
    {
        unsafe
        {
            fixed (char* namePtr = name)
            {
                SECURITY_ATTRIBUTES sa;
                NativeMemory.Clear(&sa, (nuint)sizeof(SECURITY_ATTRIBUTES));
                sa.nLength = (uint)sizeof(SECURITY_ATTRIBUTES);
                sa.bInheritHandle = FALSE;

                SECURITY_DESCRIPTOR sd;
                InitializeSecurityDescriptor(&sd, SECURITY.SECURITY_DESCRIPTOR_REVISION);
                SetSecurityDescriptorDacl(&sd, TRUE, null, FALSE);
                sa.lpSecurityDescriptor = &sd;

                var handle = CreateEvent(&sa, false, false, namePtr);

                if (handle == HANDLE.NULL)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to create named event '{name}'");
                }

                return new Win32NamedEvent(handle, name);
            }
        }
    }

    public static Win32NamedEvent Open(string name)
    {
        unsafe
        {
            var handle = OpenEvent(EVENT_MODIFY_STATE | SYNCHRONIZE, false, name);

            if (handle == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error, $"Failed to open named event '{name}'");
            }

            Console.WriteLine("Opened event : " + name);

            return new Win32NamedEvent((HANDLE)handle, name);
        }
    }

    public void Set()
    {
        if (!SetEvent(Handle))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to set named event '{EventName}'");
        }
    }

    public void Reset()
    {
        if (!ResetEvent(Handle))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to reset named event '{EventName}'");
        }
    }

    public bool TryWait(TimeSpan timeout = default)
    {
        uint result = WaitForSingleObject(Handle, (uint)timeout.TotalMilliseconds);

        if (result == WAIT.WAIT_OBJECT_0)
        {
            return true;
        }
        else if (result == WAIT.WAIT_TIMEOUT)
        {
            return false;
        }
        else
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to wait for named event '{EventName}'");
        }
    }


    public void Dispose()
    {
        CloseHandle(Handle);
    }
}
