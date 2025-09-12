import { RtcCommandType, type MouseButton, type ScrollDirection, type GamepadButtonEnum, type GamepadAxisEnum } from "../../data/InputData";

export default class RtcMessageWriter{
    private readonly _commandChannel: RTCDataChannel;

    constructor(dataChannel: RTCDataChannel) {
        this._commandChannel = dataChannel;
    }

    public SendMouseMove(x: number, y: number){
        if(this._commandChannel && this._commandChannel.readyState == "open"){
            const arr = new ArrayBuffer(5);
            const view = new DataView(arr);

            view.setInt8(0, RtcCommandType.MouseMove);
            view.setInt16(1, x, true);
            view.setInt16(3, y, true);

            this._commandChannel.send(arr);
        }
    }

    public SendMouseButton(button: MouseButton, pressed: boolean){
         if(this._commandChannel && this._commandChannel.readyState == "open"){
            const arr = new ArrayBuffer(3);
            const view = new DataView(arr);

            view.setInt8(0, RtcCommandType.MouseButton);
            view.setInt8(1, button);
            view.setInt8(2, pressed ? 1 : 0);

            this._commandChannel.send(arr);
        }
    }

    public SendKeyboardKey(key: number, pressed: boolean){
        if(this._commandChannel && this._commandChannel.readyState == "open"){
            const arr = new ArrayBuffer(3);
            const view = new DataView(arr);

            view.setInt8(0, RtcCommandType.KeyboardKey);
            view.setInt8(1, key);
            view.setInt8(2, pressed ? 1 : 0);

            this._commandChannel.send(arr);
        }
    }

    public SendMouseScroll(direction: ScrollDirection){
         if(this._commandChannel && this._commandChannel.readyState == "open"){
            const arr = new ArrayBuffer(2);
            const view = new DataView(arr);

            view.setInt8(0, RtcCommandType.MouseScroll);
            view.setInt8(1, direction);

            this._commandChannel.send(arr);
        }
    }

    public SendGamepadButton(button: GamepadButtonEnum, pressed: boolean){
        if(this._commandChannel && this._commandChannel.readyState == "open"){
            const arr = new ArrayBuffer(3);
            const view = new DataView(arr);

            view.setInt8(0, RtcCommandType.GamepadButton);
            view.setInt8(1, button);
            view.setInt8(2, pressed ? 1 : 0);

            this._commandChannel.send(arr);
        }
    }

    public SendGamepadAxis(axis: GamepadAxisEnum, value: number){
        if(this._commandChannel && this._commandChannel.readyState == "open"){
            const arr = new ArrayBuffer(6);
            const view = new DataView(arr);

            view.setInt8(0, RtcCommandType.GamepadAxis);
            view.setInt8(1, axis);
            
            view.setFloat32(2, value, true);

            this._commandChannel.send(arr);
        }
    }
}