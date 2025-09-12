export interface MqttMessageRoot {
    Payload: string;
    Iv: string;
    Tag: string;
}

export type MqttMessageType = "ServerHello" | "ClientHello" | "ClientStartSession"
    | "RtcOffer" | "RtcCandidate" | "RtcAnswer";

export interface MqttMessage{
    $type: MqttMessageType;

    MessageId?: string;
    SessionId?: string;
};

export interface MqttServerHelloMessage extends MqttMessage{}
export interface MqttClientHelloMessage extends MqttMessage{}
export interface MqttClientStartSessionMessage extends MqttMessage{}

export interface MqttRtcOfferMessage extends MqttMessage{
    OfferSdp: string;
};

export interface MqttRtcCandidateMessage extends MqttMessage{
    Mid: string;
    Candidate: string;
};

export interface MqttRtcAnswerMessage extends MqttMessage{
    AnswerSdp: string;
};