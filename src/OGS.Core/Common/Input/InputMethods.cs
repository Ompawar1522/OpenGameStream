namespace OGS.Core.Common.Input;

[Flags]
public enum InputMethods
{
    None = 0,
    Keyboard = 1,
    Mouse = 2,
    Gamepad = 4,
    All = Keyboard | Mouse | Gamepad
}