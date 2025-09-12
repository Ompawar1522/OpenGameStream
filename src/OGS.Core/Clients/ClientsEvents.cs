using OGS.Core.Clients.Rtc;
using OGS.Core.Common.Input;

namespace OGS.Core.Clients;

public sealed class ClientsEvents
{
    public Event<ClientBase> OnConnected { get; } = new();
    public Event<ClientBase> OnDisconnected { get; } = new();

    public Event<InputMethods> OnInputMethodsChanged { get; } = new();

    public Event<ClientBase, string> OnInviteCode { get; } = new();
    public Event<ClientBase> OnVideoPli { get; } = new();

    public Event<ClientBase, MoveMouseCommand> OnMouseMove { get; } = new();
    public Event<ClientBase, MouseButtonCommand> OnMouseButton { get; } = new();
    public Event<ClientBase, KeyboardKeyCommand> OnKeyboardKey { get; } = new();
    public Event<ClientBase, MouseScrollCommand> OnMouseScroll { get; } = new();
    public Event<ClientBase, GamepadButtonCommand> OnGamepadButton { get; } = new();
    public Event<ClientBase, GamepadAxisCommand> OnGamepadAxis { get; } = new();
}
