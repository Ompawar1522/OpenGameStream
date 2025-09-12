using TerraFX.Interop.WinRT;
using static TerraFX.Interop.WinRT.WinRT;

namespace OGS.Windows.Native;

public sealed unsafe class WinRtString : IDisposable
{
    public HSTRING HString => _hstring;
    public string String { get; }

    private HSTRING _hstring;

    public WinRtString(string text)
    {
        fixed (char* src = text)
        {
            fixed (HSTRING* ptr = &_hstring)
            {
                WindowsCreateString(src, (uint)text.Length, ptr).ThrowIfFailed();
            }
        }

        String = text;
    }

    public WinRtString(HSTRING hs)
    {
        uint length = 0;
        var buffer = WindowsGetStringRawBuffer(hs, &length);

        String = new string(buffer, 0, (int)length);
        _hstring = hs;
    }

    public void Dispose()
    {
        WindowsDeleteString(HString);
    }
}
