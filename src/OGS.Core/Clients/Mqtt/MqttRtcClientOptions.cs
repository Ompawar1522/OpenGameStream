using OGS.Core.Clients.Rtc;

namespace OGS.Core.Clients.MqttRtc;

public sealed class MqttRtcClientOptions : RtcClientOptions
{
    public required string MqttWebsocketUrl { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
}