using TerraFX.Interop;
using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;

namespace OGS.Windows.Common;

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
    
    public static unsafe bool TryCallIClosableClose<T>(T* instance)
        where T : unmanaged
    {
        if (instance is null)
            return false;

        IUnknown* iu = (IUnknown*)instance;

        IClosable* closableInterface;

        if (iu->QueryInterface(Uuidof<IClosable>(), (void**)&closableInterface).FAILED)
            return false;

        var hr = closableInterface->Close();
        var t = closableInterface->Release();
        return true;
    }
}