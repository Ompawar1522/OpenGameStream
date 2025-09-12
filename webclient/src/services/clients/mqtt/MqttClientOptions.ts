import type RtcClientOptions from "../rtc/RtcClientOptions";

export default interface MqttClientOptions extends RtcClientOptions {
    websocketUrl: string;
    hostTopic: string;
    clientTopic: string;
    username?: string;
    password?: string;
    AesKey: string;
}