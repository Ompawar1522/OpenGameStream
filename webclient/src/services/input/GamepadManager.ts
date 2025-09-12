import { GamepadAxisEnum, type GamepadButtonEnum } from "../data/InputData"
import TypedEventEmitter from "../TypedEventEmitter";
import InputMapping from "./InputMapping";

export type GamepadManagerEvents = {
    onButton: (button: GamepadButtonEnum, pressed: boolean) => void;
    onAxis: (axis: GamepadAxisEnum, value: number) => void;
}

class GamepadState {
    Buttons: boolean[] = [];
    Axis: number[] = [];
}

export default class GamepadManager extends TypedEventEmitter<GamepadManagerEvents> {
    private readonly _states: GamepadState[] = [];
    private _stop: boolean = false;

    constructor() {
        super();

        window.requestAnimationFrame(this.onAnimationFrame);
    }

    public stop() {
        this._stop = true;
    }

    private onAnimationFrame = () => {
        navigator.getGamepads().forEach((pad, index) => {
            if (!pad)
                return;

            if (!this._states[index]) {
                this._states[index] = new GamepadState();

                for (let z = 0; z < 17; z++) {
                    this._states[index].Buttons[z] = false;
                }

                this._states[index].Axis[0] = 0;
                this._states[index].Axis[1] = 0;
                this._states[index].Axis[2] = 0;
                this._states[index].Axis[3] = 0;
            }

            const state = this._states[index];
            pad.buttons.forEach((button, i) => {
                if (state.Buttons[i] != button.pressed) {
                    state.Buttons[i] = button.pressed;
                    
                    this.emit("onButton", InputMapping.convertButton(i), button.pressed);
                }
            });

            pad.axes.forEach((_axis, i) => {
                const val = pad.axes[i];
                let normalVal = this.NormalizeAxisValue(val);

                if (state.Axis[i] != normalVal) {
                    state.Axis[i] = normalVal;

                    const axis = InputMapping.convertAxis(i);

                    //Todo - why is Y inverted??
                    if(axis == GamepadAxisEnum.LeftY || axis == GamepadAxisEnum.RightY){
                        normalVal = -normalVal;
                    }

                    this.emit("onAxis", InputMapping.convertAxis(i), normalVal);
                }
            });
        });

        if (!this._stop) {
            window.requestAnimationFrame(this.onAnimationFrame);
        }
    }

    private NormalizeAxisValue(value: number): number {
        if (value > -0.1 && value <= 0)
            return 0;
        else if (value < 0.1 && value >= 0)
            return 0;

        return value;
    }
}