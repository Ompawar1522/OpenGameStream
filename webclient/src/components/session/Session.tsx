import { useEffect } from "react";
import useClient from "../../contexts/useClient";
import useConnectionState from "../../hooks/useConnectionState";
import { ClientConnectionState } from "../../services/clients/ClientBase";
import SessionMain from "./SessionMain";
import SessionCopyAnswer from "./SessionCopyAnswer";
import SessionLoading from "./SessionLoading";
import { PlayerProvider } from "../../contexts/usePlayer";

export default function Session() {
    const client = useClient();
    const conState = useConnectionState();

    useEffect(() => {
        client.start();
        
        return (() => {
            client.stop();
        });
    }, []);

    if (conState == ClientConnectionState.Connected) {
        return (
            <PlayerProvider>
                <SessionMain />
            </PlayerProvider>
        )
    }

    if (conState == ClientConnectionState.HasAnswer) {
        return <SessionCopyAnswer answer={client.getAnswer() ?? ""} />
    }

    if (conState == ClientConnectionState.Closed) {
        return (
            <h1>Session closed :(</h1>
        )
    }

    return (
        <SessionLoading />
    )
}