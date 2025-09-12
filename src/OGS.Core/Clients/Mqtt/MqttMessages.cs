using System.Text.Json.Serialization;

namespace OGS.Core.Clients.MqttRtc;

public sealed class MqttMessageRoot
{
    public required string Iv { get; init; }
    public required string Tag { get; init; }

    public required string Payload { get; init; }
}

[JsonSerializable(typeof(MqttMessage))]
[JsonSerializable(typeof(MqttMessageRoot))]
public sealed partial class MqttJsonContext : JsonSerializerContext
{
}

[JsonDerivedType(typeof(MqttHostHelloMessage), "ServerHello")]
[JsonDerivedType(typeof(MqttClientHelloMessage), "ClientHello")]
[JsonDerivedType(typeof(MqttClientStartSessionMessage), "ClientStartSession")]
[JsonDerivedType(typeof(MqttRtcOfferMessage), "RtcOffer")]
[JsonDerivedType(typeof(MqttRtcCandidateMessage), "RtcCandidate")]
[JsonDerivedType(typeof(MqttRtcAnswerMessage), "RtcAnswer")]

public abstract class MqttMessage
{
    public Guid MessageId { get; set; }
    public Guid SessionId { get; set; }
}

public sealed class MqttHostHelloMessage : MqttMessage;
public sealed class MqttClientHelloMessage : MqttMessage;
public sealed class MqttClientStartSessionMessage : MqttMessage;

public sealed class MqttRtcOfferMessage : MqttMessage
{
    public required string OfferSdp { get; init; }
}

public sealed class MqttRtcCandidateMessage : MqttMessage
{
    public required string Mid { get; init; }
    public required string Candidate { get; init; }
}

public sealed class MqttRtcAnswerMessage : MqttMessage
{
    public required string AnswerSdp { get; init; }
}