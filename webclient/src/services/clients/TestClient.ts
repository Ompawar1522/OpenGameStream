import type { MouseButton, ScrollDirection, GamepadAxisEnum, GamepadButtonEnum } from "../data/InputData";
import ClientBase, { ClientConnectionState } from "./ClientBase";

export default class TestClient extends ClientBase {
    private _timer?: NodeJS.Timeout;

    public async start(): Promise<void> {
        this.updateState(ClientConnectionState.Connected);

        this._timer = setInterval(() => {
            this.emit("onStats", {
                downloadRate: Math.random() * 101,
                frameRate: Math.random() * 120,
                latency: Math.random() * 101
            });

            this.emit("onInputMethods", {
                gamepad: Math.random() > 0.5,
                keyboard: Math.random() > 0.5,
                mouse: Math.random() > 0.5
            });
        }, 1000);
    }
    public stop(): void {
        if(this._timer){
            clearInterval(this._timer);
        }
    }

    override getAnswer(): string | undefined {
        return "Test";
    }

    public sendMouseMove(_x: number, _y: number): void {

    }

    public sendMouseButton(_button: MouseButton, _pressed: boolean): void {

    }

    public sendMouseScroll(_direction: ScrollDirection): void {

    }

    public sendKeyboardKey(_key: number, _pressed: boolean): void {

    }

    public sendGamepadAxis(_axis: GamepadAxisEnum, _value: number): void {

    }

    public sendGamepadButton(_button: GamepadButtonEnum, _pressed: boolean): void {

    }

}