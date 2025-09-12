using System.Text.Json.Serialization;

namespace OGS.Core.Config;

[JsonSerializable(typeof(AppConfig))]
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
public sealed partial class AppConfigJsonContext : JsonSerializerContext
{
}
