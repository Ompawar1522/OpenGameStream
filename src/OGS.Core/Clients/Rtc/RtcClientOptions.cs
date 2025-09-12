using OGS.Core.Config.Data.Rtc;

namespace OGS.Core.Clients.Rtc;

public abstract class RtcClientOptions : ClientOptions
{
    public StunServerConfig StunServer { get; init; } = StunServerConfig.None;
    public TurnServerConfig TurnServer { get; init; } = TurnServerConfig.None;
    public bool ForceTurnServer { get; init; } = false;
}
