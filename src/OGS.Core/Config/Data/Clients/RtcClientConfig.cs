using OGS.Core.Config.Data.Rtc;

namespace OGS.Core.Config.Data.Clients;

public class RtcClientConfig : ClientConfig
{
    public required StunServerConfig StunServer { get; set; }
    public required TurnServerConfig TurnServer { get; set; }
    public bool ForceTurnServer { get; set; }
}
