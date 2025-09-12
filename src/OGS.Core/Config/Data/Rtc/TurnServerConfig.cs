namespace OGS.Core.Config.Data.Rtc;

/// <summary>
/// WebRTC TURN server configuration.
/// </summary>
public sealed class TurnServerConfig
{
    public required string Name { get; init; }
    public required string Address { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public bool Immutable { get; init; }
    public bool IsDefault { get; init; }
    public Guid Id { get; init; } = Guid.NewGuid();
    
    public static TurnServerConfig None { get; } = new ()
    {
        Name = "None (default)",
        Address = string.Empty,
        Immutable = true,
        Id = Guid.Empty,
        IsDefault = true
    };

    public override string ToString()
    {
        return Name;
    }

    public string ToIceServer() => Username is not null || Password is not null
     ? $"stun:{Username}:{Password}@{Address}"
     : $"stun:{Address}";
    
    public static bool operator== (TurnServerConfig? obj1, TurnServerConfig? obj2)
    {
        return obj1?.Id == obj2?.Id;
    }
    
    public static bool operator!= (TurnServerConfig? obj1, TurnServerConfig? obj2)
    {
        return obj1?.Id != obj2?.Id;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not TurnServerConfig other)
            return false;

        return Id.Equals(other.Id);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
