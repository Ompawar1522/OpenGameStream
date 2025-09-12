namespace OGS.Core.Config;

public sealed class MqttServerConfig
{
    public required string Name { get; init; }
    public required string WebsocketUrl { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public bool Immutable { get; init; }
    public bool IsDefault { get; init; }
    public Guid Id { get; init; } = Guid.NewGuid();

    public override string ToString()
    {
        return Name;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not MqttServerConfig other)
            return false;

        return Id.Equals(other.Id);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(MqttServerConfig? obj1, MqttServerConfig? obj2)
    {
        return obj1?.Id == obj2?.Id;
    }

    public static bool operator !=(MqttServerConfig? obj1, MqttServerConfig? obj2)
    {
        return obj1?.Id != obj2?.Id;
    }

    public static MqttServerConfig HiveMqPublic { get; } =

        new()
        {
            Name = "HiveMQ public",
            WebsocketUrl = "wss://broker.hivemq.com:8884/mqtt",
            Immutable = true,
            Id = Guid.Parse("911dcde5-76a9-41b5-843b-757fa7268641")
        };

    public static MqttServerConfig EmqxPublic { get; } =
        new()
        {
            Name = "EMQX public (Default)",
            WebsocketUrl = "wss://broker.emqx.io:8084/mqtt",
            Immutable = true,
            Id = Guid.Parse("b1c8f0d2-3f5e-4a6b-9c7d-8f1e2a3b4c5d"),
            IsDefault = true
        };

    public static MqttServerConfig MosquittoOrgTesting { get; } =
        new()
        {
            Name = "test.mosquitto.org",
            WebsocketUrl = "wss://test.mosquitto.org:8081",
            Immutable = true,
            Id = Guid.Parse("4d214332-19e1-42fc-b0ad-810f21a96d99"),
        };
}