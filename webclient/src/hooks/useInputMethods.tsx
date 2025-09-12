import { useEffect, useState } from "react";
import useClient from "../contexts/useClient";
import type { ClientInputMethods } from "../services/clients/ClientBase";

export default function useInputMethods() {
    const client = useClient();

    const [mouse, setMouse] = useState(false);
    const [keyboard, setKeyboard] = useState(false);
    const [gamepad, setGamepad] = useState(false);

    const onMethodsChanged = (methods: ClientInputMethods) => {
        setMouse(methods.mouse);
        setKeyboard(methods.keyboard);
        setGamepad(methods.gamepad);
    }

    useEffect(() => {
        client.on("onInputMethods", onMethodsChanged);
        setMouse(client.inputMethods.mouse);
        setKeyboard(client.inputMethods.keyboard);
        setGamepad(client.inputMethods.gamepad);

        return (() => {
            client.off("onInputMethods", onMethodsChanged);
        });
    }, []);

    return ({
        mouse,
        keyboard,
        gamepad
    })
}