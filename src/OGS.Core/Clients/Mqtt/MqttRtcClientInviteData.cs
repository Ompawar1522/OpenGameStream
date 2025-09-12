namespace OGS.Core.Clients.MqttRtc;

public sealed class MqttRtcClientInviteData : ClientInviteData
{
    public required string WebsocketUrl { get; init; }
    public required string HostTopic { get; init; }
    public required string ClientTopic { get; init; }
    public required string AesKey { get; init; }

    public string? Username { get; init; }
    public string? Password { get; init; }
}