using OGS.Core.Clients.ManualRtc;
using OGS.Core.Clients.MqttRtc;
using System.Text.Json.Serialization;

namespace OGS.Core.Clients;

[JsonSerializable(typeof(ClientInviteData))]
[JsonSerializable(typeof(ManualRtcClientAnswerData))]
public sealed partial class ClientInviteDataJsonContext : JsonSerializerContext;

[JsonDerivedType(typeof(ManualRtcClientInviteData), "ManualRtc")]
[JsonDerivedType(typeof(MqttRtcClientInviteData), "MQTT")]
public abstract class ClientInviteData
{

}