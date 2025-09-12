using OGS.Core.Config.Data;
using OGS.Core.Config.Data.Clients;
using OGS.Core.Config.Data.Rtc;
using OGS.Core.Config.Data.Windows;

namespace OGS.Core.Config;

public sealed class AppConfig
{
    public IEnumerable<TurnServerConfig> TurnServers { get; set; } = [TurnServerConfig.None];
    public IEnumerable<StunServerConfig> StunServers { get; set; } = [StunServerConfig.None, StunServerConfig.GoogleStun1, StunServerConfig.GoogleStun2, StunServerConfig.GoogleStun3];

    public IEnumerable<MqttServerConfig> MqttServers { get; set; } =
        [MqttServerConfig.MosquittoOrgTesting, MqttServerConfig.EmqxPublic, MqttServerConfig.HiveMqPublic];

    public ClientConfig ClientConfig { get; set; } = new MqttRtcClientConfig
    {
        ServerConfig = MqttServerConfig.EmqxPublic,
        ForceTurnServer = false,
        StunServer = StunServerConfig.GoogleStun2,
        TurnServer = TurnServerConfig.None
    };

    public uint FramerateLimit { get; set; } = 60;
    public bool IncludeCursor { get; set; } = true;
    public bool ForceSoftwareEncoder { get; set; } = false;
    public BitrateValue Bitrate { get; set; } = BitrateValue.FromMegaBits(15);
    public bool EnableVideoPreview { get; set; } = true;

    public DisplayCaptureAudioMode DisplayCaptureAudioMode { get; set; } = DisplayCaptureAudioMode.ExcludeProcess;
    public string? AudioCaptureProcessName { get; set; } = "discord";

    public WindowsConfig WindowsConfig { get; set; } = new WindowsConfig();
}