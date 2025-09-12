using OGS.Core.Common.Input;

namespace OGS.Core.Clients.Rtc;

public enum ClientCommandType
{
    Unknown = 0,
    MouseMove = 1,
    MouseButton = 2,
    MouseScroll = 3,
    KeyboardKey = 4,
    GamepadAxis = 5,
    GamepadButton = 6,
}

public readonly ref struct MoveMouseCommand(short x, short y)
{
    public short X { get; } = x;
    public short Y { get; } = y;
}

public readonly ref struct MoveMouseAbsoluteCommand(short x, short y)
{
    public short X { get; } = x;
    public short Y { get; } = y;
}

public readonly ref struct MouseButtonCommand(MouseButton button, bool pressed)
{
    public MouseButton Button { get; } = button;
    public bool Pressed { get; } = pressed;
}

public readonly ref struct MouseScrollCommand(MouseScrollDirection direction)
{
    public MouseScrollDirection Direction { get; } = direction;
}

public readonly ref struct KeyboardKeyCommand(byte key, bool pressed)
{
    public byte Key { get; } = key;
    public bool Pressed { get; } = pressed;
}

public readonly ref struct GamepadAxisCommand(byte axis, float value)
{
    public byte Axis { get; } = axis;
    public float Value { get; } = value;
}

public readonly ref struct GamepadButtonCommand(byte button, bool pressed)
{
    public byte Button { get; } = button;
    public bool Pressed { get; } = pressed;
}