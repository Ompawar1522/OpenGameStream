using System.Text.Json.Serialization;

namespace OGS.Core.Config.Data.Clients;

[JsonDerivedType(typeof(ManualRtcClientConfig), "Manual")]
[JsonDerivedType(typeof(MqttRtcClientConfig), "MQTT")]
public abstract class ClientConfig
{
}
