import { GamepadAxisEnum, GamepadButtonEnum, MouseButton } from "../data/InputData";

export default class InputMapping{
    private static readonly _buttonMap = new Map<Number, GamepadButtonEnum>([
       [0, GamepadButtonEnum.Cross],
       [1, GamepadButtonEnum.Circle],
       [2, GamepadButtonEnum.Square],
       [3, GamepadButtonEnum.Triangle],
       [4, GamepadButtonEnum.L1],
       [5, GamepadButtonEnum.R1],
       [6, GamepadButtonEnum.L2],
       [7, GamepadButtonEnum.R2],
       [8, GamepadButtonEnum.Select],
       [9, GamepadButtonEnum.Start],
       [10, GamepadButtonEnum.L3],
       [11, GamepadButtonEnum.R3],
       [12, GamepadButtonEnum.DpadUp],
       [13, GamepadButtonEnum.DpadDown],
       [14, GamepadButtonEnum.DpadLeft],
       [15, GamepadButtonEnum.DpadRight]
    ]);

    private static readonly _axisMap = new Map<Number, GamepadAxisEnum>([
       [0, GamepadAxisEnum.LeftX],
       [1, GamepadAxisEnum.LeftY],
       [2, GamepadAxisEnum.RightX],
       [3, GamepadAxisEnum.RightY] 
    ]);

    public static convertAxis(axis: number): GamepadAxisEnum{
        if(this._axisMap.has(axis))
            return this._axisMap.get(axis)!;
           

        console.error("Unknown gamepad button " + axis);
        return GamepadAxisEnum.None;
    }

    public static convertButton(button: number) : GamepadButtonEnum{
         if(this._buttonMap.has(button))
            return this._buttonMap.get(button)!;

        console.error("Unknown gamepad button " + button);
        return GamepadButtonEnum.None;
    }

    public static ConvertMouseButton(button: number) : MouseButton{
        if(button == 0)
            return MouseButton.Left;
        else if(button == 1)
            return MouseButton.Middle;
        else if(button == 2)
            return MouseButton.Right;
        else if(button == 3)
            return MouseButton.X1;
        else if(button == 4)
            return MouseButton.X2;

        return MouseButton.None;
    }
}