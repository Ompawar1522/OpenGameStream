import type { InviteData, MqttRtcInviteData } from "../../services/clients/ClientInviteData";
import Panel from "../common/Panel";

export interface InviteInfoProps {
    onConfirm: () => void;
    invite: InviteData;
}

export default function InviteInfo(props: InviteInfoProps) {
    const getInfo = () => {
        switch (props.invite.$type) {
            case "ManualRtc":
                return manualRtcInfo();
            case "MQTT":
                return mqttRtcInfo();
        }
    }

    const manualRtcInfo = () => {
        return (
            <p style={{ "display": "flex", "flexDirection": "column" }}>
                <span style={{ "marginBottom": "10px" }}>Type: WebRtc via manual signalling</span>
            </p>
        )
    }

    const mqttRtcInfo = () => {
        const data = props.invite as MqttRtcInviteData;

        return (
            <>
                <p style={{ "display": "flex", "flexDirection": "column" }}>
                    <span style={{ "marginBottom": "10px" }}>Type: WebRtc via Mqtt signalling</span>
                    <span>Mqtt Url: {data.WebsocketUrl}</span>
                </p>
            </>
        )
    }

    return (
        <Panel header="Host information">
            {getInfo()}
            <button className="simple-button" onClick={props.onConfirm}>Connect</button>
        </Panel>
    )
}