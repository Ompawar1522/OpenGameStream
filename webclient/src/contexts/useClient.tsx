import { createContext, useContext } from "react";
import type ClientBase from "../services/clients/ClientBase";

const ClientContext = createContext<ClientBase | undefined>(undefined);

export const ClientProvider = ({ client, children }: { client: ClientBase, children: React.ReactNode }) => {
    return (
        <ClientContext.Provider value={client}>
            {children}
        </ClientContext.Provider>
    )
}

export default function useClient(): ClientBase {
    const ctx = useContext(ClientContext);
    if (!ctx) {
        throw new Error("useClient must be used within a ClientProvider");
    }
    return ctx;
}