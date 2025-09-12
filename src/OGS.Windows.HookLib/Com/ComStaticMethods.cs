using TerraFX.Interop;
using TerraFX.Interop.Windows;

namespace OGS.Windows.HookLib.Com;

public static class ComStaticMethods
{
    public static unsafe Guid* Uuidof<T>() where T : unmanaged, INativeGuid
    {
        return TerraFX.Interop.Windows.Windows.__uuidof<T>();
    }
    
    /// <summary>
    /// Calls free on a COM object pointer, and sets the pointer to null
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ptr"></param>
    public static unsafe void ReleaseAndNull<T>(ref T* ptr)
        where T : unmanaged
    {
        if (ptr is not null && (nint)ptr != 0)
        {
            IUnknown* r = (IUnknown*)ptr;
            r->Release();
            ptr = null;
        }
    }
}