namespace OGS.Core.Config.Data.Clients;

public sealed class MqttRtcClientConfig : RtcClientConfig
{
    public required MqttServerConfig ServerConfig { get; init; }
}
