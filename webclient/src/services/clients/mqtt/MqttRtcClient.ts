import mqtt from "mqtt";
import AesContext from "../../AesContext";
import type MqttClientOptions from "./MqttClientOptions";
import type { MqttMessage, MqttMessageRoot, MqttRtcAnswerMessage, MqttRtcCandidateMessage, MqttRtcOfferMessage } from "./MqttMessages";
import RtcClientBase from "../rtc/RtcClientBase";
import { ClientConnectionState } from "../ClientBase";

export default class MqttRtcClient extends RtcClientBase {
    private _stop: boolean = false;

    private readonly _sessionId: string = window.crypto.randomUUID();
    private readonly _processedMessageIds = new Map<string, boolean>();
    private _aesContext: AesContext = undefined!;
    private _mqttClient!: mqtt.MqttClient;
    private _backgroundTask?: Promise<void>;

    constructor(private _mqttOptions: MqttClientOptions) {
        super(_mqttOptions);
    }

    public start(): void {
        if (this._stop || this._backgroundTask) {
            console.warn("Cannot restart client");
            return;
        }

        this._backgroundTask = this.internalStart();
    }

    private internalStart = async () => {
        console.log("Mqtt client -> starting");

        try {
            this._aesContext = await AesContext.CreateFromSecretAsync(this._mqttOptions.AesKey);

            if (this._stop)
                return;

            this._mqttClient = mqtt.connect(this._mqttOptions.websocketUrl, {
                username: this._mqttOptions.username,
                password: this._mqttOptions.password
            });

            this._mqttClient.on("connect", this.onMqttConnected);
            this._mqttClient.on("disconnect", this.onMqttDisconnected);
            this._mqttClient.on("message", this.onMqttMessage);
            this._mqttClient.on("error", this.onMqttError);
        } catch (err) {
            console.error("Failed to start Mqtt client: " + err);
            this.stop();

            alert("Failed to create client: " + err);
            this.updateState(ClientConnectionState.Closed);
        }
    }

    public stop(): void {
        console.log("Mqtt client -> stop");
        this._stop = true;

        if (this._mqttClient) {
            this._mqttClient.end(true);
        }

        this.closePeer();
        this._processedMessageIds.clear();
    }

    protected onConnectionState(): void {
        this.updateConnectedState();
    }

    protected onSignalingState(): void {

    }

    protected onIceConnectionState(): void {
        this.updateConnectedState();
    }

    protected onIceGatheringState(): void {

    }

    protected onRtcConnected = () => {

    };

    protected onRtcDisconnected = () => {

    }

    protected onLocalCandidate = async (event: RTCPeerConnectionIceEvent) => {
        if (!event.candidate)
            return;

        const msg: MqttRtcCandidateMessage = {
            $type: "RtcCandidate",
            Candidate: event.candidate.candidate,
            Mid: event.candidate.sdpMid!
        };

        await this.sendEncrypted(msg);
    }

    private onMqttConnected = async () => {
        if (this._stop)
            return;

        console.log("MQTT -> connected to " + this._mqttOptions.websocketUrl);

        try {
            await this._mqttClient.subscribeAsync(this._mqttOptions.clientTopic);
            console.log("Subscribed to " + this._mqttOptions.clientTopic);

            await this.sendEncrypted({ "$type": "ClientHello" });
        } catch (err) {
            console.error("Failed to subsacribe to " + this._mqttOptions.clientTopic);
        }
    }

    private onMqttDisconnected = async () => {
        console.log("MQTT -> disconnected");

        if (this._stop)
            return;

        this._mqttClient.connect();
    }

    private onMqttError = async (err: Error) => {
        if (this._stop)
            return;

        console.error("MQTT error -> " + err);
    }

    private onMqttMessage = async (topic: string, _payload: Buffer<ArrayBufferLike>, packet: mqtt.IPublishPacket) => {
        if (this._stop)
            return;

        try {
            if (topic != this._mqttOptions.clientTopic)
                return;

            const decryptedMessage = await MqttRtcClient.DecryptBase64(packet.payload as string, this._aesContext!);

            if (this._stop)
                return;

            await this.onMqttDecryptedMessage(decryptedMessage);
        } catch (err) {
            console.error("Failed to handle MQTT message: " + err);
        }
    };

    private onMqttDecryptedMessage = async (message: MqttMessage) => {
        console.log("MQTT Msg -> " + message.$type);

        if (!message.MessageId) {
            console.error("Received message without message ID");
            console.dir(message);
            return;
        }

        if (this._processedMessageIds.has(message.MessageId)) {
            console.error("Received duplicate message ID");
            console.dir(message);
            return;
        }

        if (message.$type == "ServerHello") {
            await this.sendEncrypted({ $type: "ClientStartSession" });
            return;
        }

        if (message.SessionId != this._sessionId) {
            console.error("Received message for wrong session ID");
            console.dir(message);
            return;
        }

        this._processedMessageIds.set(message.MessageId, true);
        if (message.$type == "RtcOffer") {
            await this.onMqttOfferMessage(message as MqttRtcOfferMessage);
        } else if (message.$type == "RtcCandidate") {
            await this.onMqttCandidateMessage(message as MqttRtcCandidateMessage);
        }
    }

    private onMqttCandidateMessage = async (message: MqttRtcCandidateMessage) => {
        try {
            if (this.peer) {
                await this.peer.addIceCandidate({ candidate: message.Candidate, sdpMid: message.Mid });
                console.log("Added remote candidate " + message.Candidate);
            } else {
                console.error("Received Rtc candidate but peer was undefined");
            }
        } catch (err) {
            console.dir(message);
            console.error("Failed to handle remote candidate", err);
        }
    }

    private onMqttOfferMessage = async (message: MqttRtcOfferMessage) => {
        try {
            if (this._stop)
                return;

            this.RecreatePeer();

            await this.peer!.setRemoteDescription({ type: "offer", "sdp": message.OfferSdp });

            if (this._stop)
                return;

            const answer = await this.peer!.createAnswer();

            if (this._stop)
                return;

            await this.peer!.setLocalDescription(answer);

            if (this._stop)
                return;

            const msg: MqttRtcAnswerMessage = {
                $type: "RtcAnswer",
                AnswerSdp: answer!.sdp!
            };

            await this.sendEncrypted(msg);
            console.log("Handled Rtc offer");
        } catch (err) {
            console.error("Failed to handle Rtc offer: " + err);
        }
    }

    private async sendEncrypted(message: MqttMessage): Promise<void> {
        message.MessageId = crypto.randomUUID();
        message.SessionId = this._sessionId;

        try {
            const msg = await MqttRtcClient.EncryptBase64(message, this._aesContext!);
            await this._mqttClient!.publishAsync(this._mqttOptions.hostTopic, msg);
        } catch (err) {
            console.error("Failed to sent MQTT message: " + err);
        }
    }

    private static async DecryptBase64(input: string, aes: AesContext): Promise<MqttMessage> {
        const root: MqttMessageRoot = JSON.parse(atob(input)) as MqttMessageRoot;

        const dec = await aes.DecryptBase64(root.Payload, root.Iv, root.Tag);

        return JSON.parse(dec) as MqttMessage;
    }

    private static async EncryptBase64(message: MqttMessage, aes: AesContext): Promise<string> {
        const result = await aes.EncryptBase64(JSON.stringify(message));

        const root: MqttMessageRoot = {
            Iv: result.Iv,
            Payload: result.Payload,
            Tag: result.Tag
        };

        return btoa(JSON.stringify(root));
    }
}