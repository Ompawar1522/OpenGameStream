import type { InviteData } from "../../services/clients/ClientInviteData";
import { ParseInviteData } from "../../services/data/InviteDataParser";
import Panel from "../common/Panel";

export interface GetCodeProps {
    onInvite: (invite: InviteData) => void;
}

export default function GetInviteData(props: GetCodeProps) {
    const doPasteCode = async () => {
        try {
            const code = await navigator.clipboard.readText();
            const data: InviteData = ParseInviteData(code);
            props.onInvite(data);
        } catch (err) {
            console.error(err);
            alert("Invalid invite code");
        }
    }

    const ver = (import.meta as any).env.VITE_OGS_VER;

    return (
        <Panel header="OpenGameStream" >
            <p>version: {ver}</p>
            <button className="simple-button" onClick={doPasteCode}>Paste invite code</button>
        </Panel>
    )
}