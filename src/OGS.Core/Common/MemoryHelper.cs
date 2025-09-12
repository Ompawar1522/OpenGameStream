using System.Runtime.InteropServices;

namespace OGS.Core.Common;

public static class MemoryHelper
{
    public static unsafe T* AllocZeroed<T>()
        where T : unmanaged
    {
        return (T*)NativeMemory.AllocZeroed((nuint)Marshal.SizeOf<T>());
    }
}
