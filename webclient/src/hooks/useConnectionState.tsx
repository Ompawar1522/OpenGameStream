import { useEffect, useState } from "react";
import useClient from "../contexts/useClient"
import { ClientConnectionState } from "../services/clients/ClientBase";

export default function useConnectionState(){
    const client = useClient();

    const [connectionState, setConnectionState] = useState<ClientConnectionState>(client.state);

    useEffect(() => {
        client.on("onConnectionState", setConnectionState);

        return(() => {
            client.off("onConnectionState", setConnectionState);
        });
    }, [])

    return(
        connectionState
    )
}