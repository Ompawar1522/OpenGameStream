using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace OGS.Windows.HookLib.Common;

public static unsafe class ConsoleHelper
{
    public static void CreateConsole()
    {
        AllocConsole();

        fixed(char* buff = "CONOUT$")
        {
            nint handle = CreateFile(buff, GENERIC_WRITE, FILE.FILE_SHARE_WRITE, null, OPEN.OPEN_EXISTING, 0, HANDLE.NULL);
            SetStdHandle(STD.STD_ERROR_HANDLE, (HANDLE)handle);
            SetStdHandle(STD.STD_OUTPUT_HANDLE, (HANDLE)handle);
        }
    }

    public static void FreeConsole()
    {
        FreeConsole();
    }
}
