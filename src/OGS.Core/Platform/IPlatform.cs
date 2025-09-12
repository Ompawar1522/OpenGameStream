using Avalonia.Controls;
using OGS.Core.Common.Input;

namespace OGS.Core.Platform;

public interface IPlatform : IDisposable
{
    PlatformEvents Events { get; }

    void Initialize();

    void RequestKeyFrame();

    Control CreatePreviewControl();
    Control CreateQuickSwitchControl();

    void MoveMouseRelative(short x, short y);
    void MoveMouseAbsolute(int x, int y);
    void SendMouseButton(MouseButton button, bool pressed);
    void SendKeyboardKey(int key, bool pressed);
    void SendMouseScroll(MouseScrollDirection direction);
    IGamepad CreateGamepad();

    IPreciseTimer CreateTimer(TimeSpan interval);
}
