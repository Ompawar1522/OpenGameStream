export enum RtcCommandType {
    Unknown = 0,
    MouseMove = 1,
    MouseButton = 2,
    MouseScroll = 3,
    KeyboardKey = 4,
    GamepadAxis = 5,
    GamepadButton = 6
}

export enum MouseButton{
    None = 0,
    Left = 1,
    Right = 2,
    Middle = 3,
    X1 = 4,
    X2 = 5,
}

export enum ScrollDirection{
    None = 0,
    Up = 1,
    Down = 2,
    Left = 3,
    Right = 4
}

export enum GamepadAxisEnum {
    None = 0,
    LeftX,
    LeftY,
    RightX,
    RightY,
}

export enum GamepadButtonEnum {
    None = 0,
    Cross,
    Circle,
    Triangle,
    Square,
    L1,
    L2,
    L3,
    R1,
    R2,
    R3,
    Start,
    Select,
    DpadUp,
    DpadDown,
    DpadLeft,
    DpadRight,
}