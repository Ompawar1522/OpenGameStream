import { useState } from "react";
import type { InviteData } from "./services/clients/ClientInviteData";
import { ClientProvider } from "./contexts/useClient";
import ClientBase from "./services/clients/ClientBase";
import CreateClient from "./services/clients/ClientFactory";
import Session from "./components/session/Session";
import AppStart from "./components/setup/AppStart";

export default function App() {
    const [client, setClient] = useState<ClientBase | undefined>();

    const doConnect = (invite: InviteData) => {
        setClient(CreateClient(invite));
    }

    if (!client) {
        return (
            <AppStart connect={doConnect} />
        )
    }

    return(
        <ClientProvider client={client}>
            <Session/>
        </ClientProvider>
    )
}