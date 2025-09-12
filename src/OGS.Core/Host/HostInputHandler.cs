using OGS.Core.Clients;
using OGS.Core.Clients.Rtc;
using OGS.Core.Common.Input;
using OGS.Core.Platform;
using System.Collections.Concurrent;

namespace OGS.Core.Host;

public sealed class HostInputHandler
{
    private static readonly Log Log = LogManager.GetLogger<HostInputHandler>();

    private readonly IConfigService _configService;
    private readonly IPlatform _platform;
    private readonly ConcurrentDictionary<ClientBase, IGamepad> _gamepads = new();

    public HostInputHandler(IConfigService configService,
        IPlatform platform)
    {
        _configService = configService;
        _platform = platform;
    }

    public void AddClient(ClientBase client)
    {
        client.Events.OnMouseMove.Subscribe(OnMouseMove);
        client.Events.OnMouseButton.Subscribe(OnMouseButton);
        client.Events.OnMouseScroll.Subscribe(OnMouseScroll);
        client.Events.OnKeyboardKey.Subscribe(OnKeyboardKey);
        client.Events.OnGamepadAxis.Subscribe(OnGamepadAxis);
        client.Events.OnGamepadButton.Subscribe(OnGamepadButton);

        try
        {
            IGamepad gamepad = _platform.CreateGamepad();
            _gamepads[client] = gamepad;

            Log.Info($"Created gamepad for client {client.Info.Name}");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to create gamepad for client {client.Info.Name}", ex);
        }
    }

    public void RemoveClient(ClientBase client)
    {
        client.Events.OnMouseMove.Unsubscribe(OnMouseMove);
        client.Events.OnMouseButton.Unsubscribe(OnMouseButton);
        client.Events.OnMouseScroll.Unsubscribe(OnMouseScroll);
        client.Events.OnKeyboardKey.Unsubscribe(OnKeyboardKey);
        client.Events.OnGamepadAxis.Unsubscribe(OnGamepadAxis);
        client.Events.OnGamepadButton.Unsubscribe(OnGamepadButton);

        if (_gamepads.Remove(client, out var gamepad))
        {
            gamepad.Dispose();
        }
    }

    private void OnGamepadButton(ClientBase client, GamepadButtonCommand command)
    {
        if (ShouldSendInput(client, InputMethods.Gamepad))
        {
            if (!_gamepads.TryGetValue(client, out var gamepad))
            {
                //Log.Warn($"No gamepad found for client {client.Info.Name}");
                return;
            }

            gamepad.SetButton((GamepadButton)command.Button, command.Pressed);
        }
    }

    private void OnGamepadAxis(ClientBase client, GamepadAxisCommand command)
    {
        if (ShouldSendInput(client, InputMethods.Gamepad))
        {
            if (!_gamepads.TryGetValue(client, out var gamepad))
            {
                //Log.Warn($"No gamepad found for client {client.Info.Name}");
                return;
            }

            gamepad.SetAxis((GamepadAxis)command.Axis, command.Value);
        }
    }

    private void OnKeyboardKey(ClientBase client, KeyboardKeyCommand command)
    {
        if (ShouldSendInput(client, InputMethods.Keyboard))
            _platform.SendKeyboardKey(command.Key, command.Pressed);
    }

    private void OnMouseScroll(ClientBase client, MouseScrollCommand command)
    {
        if (ShouldSendInput(client, InputMethods.Mouse))
            _platform.SendMouseScroll(command.Direction);
    }

    private void OnMouseButton(ClientBase client, MouseButtonCommand command)
    {
        if (ShouldSendInput(client, InputMethods.Mouse))
            _platform.SendMouseButton(command.Button, command.Pressed);
    }

    private void OnMouseMove(ClientBase client, MoveMouseCommand command)
    {
        if (ShouldSendInput(client, InputMethods.Mouse))
            _platform.MoveMouseRelative(command.X, command.Y);
    }

    private bool ShouldSendInput(ClientBase client, InputMethods type)
    {
        return !client.Removing &&
               client.Connected &&
               client.InputMethods.HasFlag(type);
    }
}
