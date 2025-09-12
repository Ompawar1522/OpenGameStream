using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace OGS.Windows.HookLib.Common;
internal static class Win32Extensions
{
    public static void ThrowIfFailed(this HRESULT hr)
    {
        Marshal.ThrowExceptionForHR(hr);
    }

    public static void ThrowIfFailed(this HRESULT hr, string message)
    {
        Exception? ex = Marshal.GetExceptionForHR(hr);

        if (ex is not null)
        {
            throw new AggregateException(message, ex);
        }
    }
}
