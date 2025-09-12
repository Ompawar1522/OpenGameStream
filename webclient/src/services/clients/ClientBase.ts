import type { MouseButton, ScrollDirection, GamepadAxisEnum, GamepadButtonEnum } from "../data/InputData";
import TypedEventEmitter from "../TypedEventEmitter";
import type { ClientStats } from "./ClientStats";

export enum ClientConnectionState {
    Idle,
    Connecting,
    HasAnswer,
    Connected,
    Closed
}

export interface ClientInputMethods{
    mouse: boolean;
    keyboard: boolean;
    gamepad: boolean;
}

export type ClientEvents = {
    onConnectionState: (state: ClientConnectionState) => void;
    onInputMethods: (methods: ClientInputMethods) => void;
    onStats: (stats: ClientStats) => void;
}

export default abstract class ClientBase extends TypedEventEmitter<ClientEvents> {
    public mediaStream: MediaStream = new MediaStream();
    public state: ClientConnectionState = ClientConnectionState.Idle;
    public inputMethods: ClientInputMethods = {gamepad: false, keyboard: false, mouse: false};

    public abstract start(): void;
    public abstract stop(): void;
    public getAnswer() : string | undefined {return undefined};

    public abstract sendMouseMove(x: number, y: number): void;
    public abstract sendMouseButton(button: MouseButton, pressed: boolean): void;
    public abstract sendMouseScroll(direction: ScrollDirection): void;
    public abstract sendKeyboardKey(key: number, pressed: boolean): void;
    public abstract sendGamepadAxis(axis: GamepadAxisEnum, value: number): void;
    public abstract sendGamepadButton(button: GamepadButtonEnum, pressed: boolean): void;

    protected updateState(state: ClientConnectionState) {
        this.state = state;
        console.log("Client state -> " + ClientConnectionState[state]);
        this.emit("onConnectionState", state);
    }
}