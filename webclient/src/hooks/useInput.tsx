import { useEffect, useMemo, useState } from "react";
import useClient from "../contexts/useClient";
import InputManager from "../services/input/InputManager";

export default function useInput() {
    const [isGrabbed, setIsGrabbed] = useState(false);
    const client = useClient();
    const inputManager = useMemo(() => new InputManager(client), []);

    useEffect(() => {
        inputManager.on("onGrabState", setIsGrabbed);

        return (() => {
            inputManager.off("onGrabState", setIsGrabbed);

            inputManager.stop();
        });
    }, []);

    const doGrab = () => {
        inputManager.doGrab();
    }

    return ({
        isGrabbed,
        doGrab
    })
}