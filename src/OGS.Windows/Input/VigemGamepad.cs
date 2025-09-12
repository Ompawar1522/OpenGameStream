using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using OGS.Core.Common.Input;

namespace OGS.Windows.Input;

public sealed class VigemGamepad : IGamepad
{
    private readonly IXbox360Controller _controller;

    public VigemGamepad(ViGEmClient client)
    {
        _controller = client.CreateXbox360Controller();
        _controller.Connect();
    }

    public void SetAxis(GamepadAxis axis, float value)
    {
        switch (axis)
        {
            case GamepadAxis.LeftX:
                _controller.SetAxisValue(Xbox360Axis.LeftThumbX, (FloatToShortRange(value)));
                break;
            case GamepadAxis.LeftY:
                _controller.SetAxisValue(Xbox360Axis.LeftThumbY, (FloatToShortRange(value)));
                break;
            case GamepadAxis.RightX:
                _controller.SetAxisValue(Xbox360Axis.RightThumbX, (FloatToShortRange(value)));
                break;
            case GamepadAxis.RightY:
                _controller.SetAxisValue(Xbox360Axis.RightThumbY, (FloatToShortRange(value)));
                break;
        }
    }

    public static short FloatToShortRange(float value)
    {
        if (value <= -1f) return short.MinValue;
        if (value >= 1f) return short.MaxValue;

        return value < 0
            ? (short)(value * -short.MinValue) // 32768 for negatives
            : (short)(value * short.MaxValue); // 32767 for positives
    }

    public void SetButton(GamepadButton button, bool state)
    {
        switch (button)
        {
            case GamepadButton.Cross:
                _controller.SetButtonState(Xbox360Button.A, state);
                return;
            case GamepadButton.Circle:
                _controller.SetButtonState(Xbox360Button.B, state);
                return;
            case GamepadButton.Square:
                _controller.SetButtonState(Xbox360Button.X, state);
                return;
            case GamepadButton.Triangle:
                _controller.SetButtonState(Xbox360Button.Y, state);
                return;
            case GamepadButton.L1:
                _controller.SetButtonState(Xbox360Button.LeftShoulder, state);
                return;
            case GamepadButton.L2:
                _controller.SetSliderValue(Xbox360Slider.LeftTrigger, (byte)(state ? 255 : 0));
                return;
            case GamepadButton.L3:
                _controller.SetButtonState(Xbox360Button.LeftThumb, state);
                return;
            case GamepadButton.R1:
                _controller.SetButtonState(Xbox360Button.RightShoulder, state);
                return;
            case GamepadButton.R2:
                _controller.SetSliderValue(Xbox360Slider.RightTrigger, (byte)(state ? 255 : 0));
                return;
            case GamepadButton.R3:
                _controller.SetButtonState(Xbox360Button.RightThumb, state);
                return;
            case GamepadButton.Start:
                _controller.SetButtonState(Xbox360Button.Start, state);
                return;
            case GamepadButton.Select:
                _controller.SetButtonState(Xbox360Button.Back, state);
                return;
            case GamepadButton.DpadUp:
                _controller.SetButtonState(Xbox360Button.Up, state);
                return;
            case GamepadButton.DpadLeft:
                _controller.SetButtonState(Xbox360Button.Left, state);
                return;
            case GamepadButton.DpadRight:
                _controller.SetButtonState(Xbox360Button.Right, state);
                return;
            case GamepadButton.DpadDown:
                _controller.SetButtonState(Xbox360Button.Down, state);
                return;
        }
    }

    public void Dispose()
    {
        _controller.Disconnect();
    }
}
