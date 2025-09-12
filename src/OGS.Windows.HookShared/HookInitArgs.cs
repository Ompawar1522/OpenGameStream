using System.Text;
using ZeroLog;

namespace OGS.Windows.HookShared;

/// <summary>
/// Data that is injected into the game process
/// to initialize the hook.
/// </summary>
public unsafe struct HookInitArgs
{
    public int PipeNameLength;
    public fixed byte PipeNameUnicode[64];

    public bool ShowConsole;
    public LogLevel LogLevel;

    public string GetPipeName()
    {
        fixed (byte* pipeNamePtr = PipeNameUnicode)
        {
            return Encoding.Unicode.GetString(pipeNamePtr, PipeNameLength);
        }
    }
}