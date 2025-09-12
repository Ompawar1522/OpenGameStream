import type { InviteData, ManualRtcInviteData, MqttRtcInviteData } from "./ClientInviteData";
import ManualRtcClient from "./manual/ManualRtcClient";
import MqttRtcClient from "./mqtt/MqttRtcClient";

export default function CreateClient(inviteData: InviteData) {
    if (inviteData.$type === "ManualRtc") {
        const manualData = inviteData as ManualRtcInviteData;
        console.log("Creating manual Rtc client", manualData);

        return new ManualRtcClient({
            forceTurnServer: false,
            stunServer: { urls: "stun:stun.l.google.com:19302" },
            turnServer: undefined,
            offerSdp: manualData.Sdp,
        });
    } else if (inviteData.$type == "MQTT") {
        const mqttData = inviteData as MqttRtcInviteData;
        console.log("Creating MQTT Rtc client", mqttData);

        return new MqttRtcClient({
            forceTurnServer: false,
            stunServer: { urls: "stun:stun.l.google.com:19302" },
            turnServer: undefined,
            AesKey: mqttData.AesKey,
            clientTopic: mqttData.ClientTopic,
            hostTopic: mqttData.HostTopic,
            websocketUrl: mqttData.WebsocketUrl,
            password: mqttData.Password,
            username: mqttData.Username
        });
    }

    console.dir(inviteData);
    throw new Error("Invalid invite data");
}