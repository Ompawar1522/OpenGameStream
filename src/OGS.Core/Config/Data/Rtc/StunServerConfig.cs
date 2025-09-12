namespace OGS.Core.Config.Data.Rtc;

/// <summary>
/// WebRTC STUN server configuration.
/// </summary>
public sealed class StunServerConfig
{
    public required string Name { get; init; }
    public required string Address { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public bool IsDefault { get; init; }
    public bool Immutable { get; init; }

    public Guid Id { get; init; } = Guid.NewGuid();

    public override string ToString()
    {
        return Name;
    }

    public string ToIceServer() => Username is not null || Password is not null
        ? $"stun:{Username}:{Password}@{Address}"
        : $"stun:{Address}";

    public static StunServerConfig None { get; } = new ()
    {
        Name = "None",
        Address = string.Empty,
        Immutable = true,
        Id = Guid.Empty
    };

    public static StunServerConfig GoogleStun1 { get; } = new()
    {
        Name = "Google public STUN (default)",
        Address = "stun.l.google.com:19302",
        Immutable = true,
        Id = Guid.Parse("946160bb-e90f-4a9c-8a81-7a3fb30d53bf"),
        IsDefault = true
    };

    public static StunServerConfig GoogleStun2 { get; } = new()
    {
        Name = "Google public STUN 2",
        Address = "stun2.l.google.com:19302",
        Immutable = true,
        Id = Guid.Parse("51d1ed6a-576f-4420-b4a6-b4a7187bdef3")
    };

    public static StunServerConfig GoogleStun3 { get; } = new()
    {
        Name = "Google public STUN 3",
        Address = "stun3.l.google.com:19302",
        Immutable = true,
        Id = Guid.Parse("d40b3d94-afee-49fd-b52f-008a86729e61")
    };

    public static bool operator ==(StunServerConfig? obj1, StunServerConfig? obj2)
    {
        return obj1?.Id == obj2?.Id;
    }

    public static bool operator !=(StunServerConfig? obj1, StunServerConfig? obj2)
    {
        return obj1?.Id != obj2?.Id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not StunServerConfig other)
            return false;

        return Id.Equals(other.Id);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}