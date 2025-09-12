using OGS.Core.Common.Input;
using System.Buffers.Binary;

namespace OGS.Core.Clients.Rtc;

public sealed class RtcCommandParser
{
    private readonly ClientBase _client;
    private readonly ClientsEvents _events;

    public RtcCommandParser(ClientBase client)
    {
        _client = client;
        _events = client.Events;
    }

    public bool TryParse(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length == 0)
            return false;

        ClientCommandType type = (ClientCommandType)buffer[0];

        if (type == ClientCommandType.MouseMove)
            return ParseMouseMove(buffer);
        if (type == ClientCommandType.MouseButton)
            return ParseMouseButton(buffer);
        if (type == ClientCommandType.MouseScroll)
            return ParseMouseScroll(buffer);
        if (type == ClientCommandType.KeyboardKey)
            return ParseKeyboardKey(buffer);
        if (type == ClientCommandType.GamepadButton)
            return ParseGamepadButton(buffer);
        if (type == ClientCommandType.GamepadAxis)
            return ParseGamepadAxis(buffer);

        return false;
    }

    private bool ParseMouseMove(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length != 5)
            return false;

        short x = BinaryPrimitives.ReadInt16LittleEndian(buffer.Slice(1));
        short y = BinaryPrimitives.ReadInt16LittleEndian(buffer.Slice(3));

        _events.OnMouseMove.Raise(_client, new MoveMouseCommand(x, y));
        return true;
    }

    private bool ParseMouseButton(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length != 3)
            return false;

        _events.OnMouseButton.Raise(_client, new MouseButtonCommand((MouseButton)buffer[1], buffer[2] > 0));
        return true;
    }

    private bool ParseMouseScroll(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length != 2)
            return false;

        _events.OnMouseScroll.Raise(_client, new MouseScrollCommand((MouseScrollDirection)buffer[1]));
        return true;
    }

    private bool ParseKeyboardKey(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length != 3)
            return false;

        _events.OnKeyboardKey.Raise(_client, new KeyboardKeyCommand(buffer[1], buffer[2] > 0));
        return true;
    }

    private bool ParseGamepadButton(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length != 3)
            return false;

        _events.OnGamepadButton.Raise(_client, new GamepadButtonCommand(buffer[1], buffer[2] > 0));
        return true;
    }

    private bool ParseGamepadAxis(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length != 6)
            return false;

        float value = BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(2));
        _events.OnGamepadAxis.Raise(_client, new GamepadAxisCommand(buffer[1], value));
        return true;
    }
}
