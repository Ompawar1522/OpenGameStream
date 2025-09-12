import { useState } from "react";
import type { InviteData } from "../../services/clients/ClientInviteData";
import InviteInfo from "./InviteInfo";
import GetInviteData from "./GetInviteData";

export interface AppStartProps {
    connect: (invite: InviteData) => void;
}

export default function AppStart(props: AppStartProps) {
    const [inviteData, setInviteData] = useState<InviteData | undefined>();
    


    if (!inviteData) {
        return (
            <GetInviteData onInvite={setInviteData} />
        )
    } else {
        return (
            <InviteInfo invite={inviteData} onConfirm={() => props.connect(inviteData)} />
        )
    }
}