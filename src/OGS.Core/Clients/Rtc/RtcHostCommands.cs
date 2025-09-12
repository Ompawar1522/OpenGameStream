namespace OGS.Core.Clients.Rtc;

public enum HostCommandType
{
    Unknown = 0,
    AllowedInputsUpdate
}

public readonly ref struct AllowedInputsUpdateCommand(bool mouse, bool keyboard, bool gamepad)
{
    public bool Mouse { get; init; } = mouse;
    public bool Keyboard { get; init; } = keyboard;
    public bool Gamepad { get; init; } = gamepad;
}