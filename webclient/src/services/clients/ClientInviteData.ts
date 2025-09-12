export type InviteDataType = "ManualRtc" | "MQTT";

export interface InviteData{
    $type: InviteDataType;
}

export interface ManualRtcInviteData extends InviteData{
    Sdp: string;
}

export interface ManualRtcAnswerData {
    Sdp: string;
}

export interface MqttRtcInviteData extends InviteData{
    WebsocketUrl: string;
    HostTopic: string;
    ClientTopic: string;
    AesKey: string;
    Username?: string;
    Password?: string;
}