namespace OGS.Core.Common.Input;

public interface IGamepad : IDisposable
{
    void SetButton(GamepadButton button, bool state);
    void SetAxis(GamepadAxis axis, float value);
}