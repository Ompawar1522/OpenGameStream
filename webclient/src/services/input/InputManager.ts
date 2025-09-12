import { ClientConnectionState } from "../clients/ClientBase";
import type ClientBase from "../clients/ClientBase";
import { GamepadAxisEnum, GamepadButtonEnum, ScrollDirection } from "../data/InputData";
import TypedEventEmitter from "../TypedEventEmitter";
import GamepadManager from "./GamepadManager";
import InputMapping from "./InputMapping";

export type InputManagerEvents = {
    onGrabState: (grabbed: boolean) => void;
}

export default class InputManager extends TypedEventEmitter<InputManagerEvents> {
    private _subscribed: boolean = false;
    private readonly _gamepadManager: GamepadManager = new GamepadManager();

    constructor(private _client: ClientBase) {
        super();

        document.addEventListener("pointerlockchange", this.onPointerLockChanged);
        this._gamepadManager.on("onAxis", this.onGamepadAxis);
        this._gamepadManager.on("onButton", this.onGamepadButton);
    }

    public stop() {
        document.removeEventListener("pointerlockchange", this.onPointerLockChanged);
        this._gamepadManager.off("onAxis", this.onGamepadAxis);
        this._gamepadManager.off("onButton", this.onGamepadButton);
        this._gamepadManager.stop();

        if (this._subscribed) {
            this.unsubscribeEvents();
        }
    }

    public doGrab = async () => {
        try {
            await document.body.requestPointerLock();
        } catch (err) {
            console.error("Grab failed ", err);
        }
    }

    private onPointerLockChanged = (_ev: Event) => {
        this.emit("onGrabState", document.pointerLockElement != undefined);

        if (!this._subscribed && document.pointerLockElement) {
            this.subscribeEvents();
        } else if (this._subscribed && !document.pointerLockElement) {
            this.unsubscribeEvents();
        }
    }

    private subscribeEvents = () => {
        document.addEventListener("mousemove", this.onMouseMove);
        document.addEventListener("mousedown", this.onMouseDown);
        document.addEventListener("mouseup", this.onMouseUp);
        document.addEventListener("wheel", this.onScroll);
        document.addEventListener("keydown", this.onKeyDown);
        document.addEventListener("keyup", this.onKeyUp);

        this._subscribed = true;
    }

    private unsubscribeEvents = () => {
        document.removeEventListener("mousemove", this.onMouseMove);
        document.removeEventListener("mousedown", this.onMouseDown);
        document.removeEventListener("mouseup", this.onMouseUp);
        document.removeEventListener("wheel", this.onScroll);
        document.removeEventListener("keydown", this.onKeyDown);
        document.removeEventListener("keyup", this.onKeyUp);

        this._subscribed = false;
    }

    private onMouseMove = (ev: MouseEvent) => {
        if (this.shouldSend()) {
            this._client.sendMouseMove(ev.movementX, ev.movementY);
        }
    }

    private onMouseDown = (ev: MouseEvent) => {
        if (this.shouldSend()) {
            this._client.sendMouseButton(InputMapping.ConvertMouseButton(ev.button), true);
        }
    }

    private onMouseUp = (ev: MouseEvent) => {
        if (this.shouldSend()) {
            this._client.sendMouseButton(InputMapping.ConvertMouseButton(ev.button), false);
        }
    }

    private onScroll = (ev: WheelEvent) => {
        if (this.shouldSend()) {
            if (ev.deltaY < 0) {
                this._client.sendMouseScroll(ScrollDirection.Up);
            } else {
                this._client.sendMouseScroll(ScrollDirection.Down);
            }
        }
    }

    private onKeyDown = (ev: KeyboardEvent) => {
        if (this.shouldSend()) {
            this._client.sendKeyboardKey(ev.keyCode, true);
        }
    }

    private onKeyUp = (ev: KeyboardEvent) => {
        if (this.shouldSend()) {
            this._client.sendKeyboardKey(ev.keyCode, false);
        }
    }

    private onGamepadButton = (button: GamepadButtonEnum, pressed: boolean) => {
        if (this._client.state == ClientConnectionState.Connected) {
            this._client.sendGamepadButton(button, pressed);
        }
    }

    private onGamepadAxis = (axis: GamepadAxisEnum, value: number) => {
        if (this._client.state == ClientConnectionState.Connected) {
            this._client.sendGamepadAxis(axis, value);
        }
    }

    private shouldSend = () => {
        return this._subscribed && this._client.state == ClientConnectionState.Connected;
    }
}