import { BiSolidMouse, BiSolidKeyboard, BiSolidJoystick } from "react-icons/bi";
import useInputMethods from "../../../hooks/useInputMethods";

export default function InputIndicators() {
    const inputMethods = useInputMethods();

    return (
        <div style={{"alignItems": "center", "justifyContent": "center", "display": "flex", "gap": "2px"}}>
            <BiSolidMouse className={inputMethods.mouse ? "inputEnabled" : "inputDisabled"} size={20} />
            <BiSolidKeyboard className={inputMethods.keyboard ? "inputEnabled" : "inputDisabled"} size={20} />
            <BiSolidJoystick className={inputMethods.gamepad ? "inputEnabled" : "inputDisabled"} size={20} />
        </div>
    )
}