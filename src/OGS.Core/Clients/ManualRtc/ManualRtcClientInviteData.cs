namespace OGS.Core.Clients.ManualRtc;

public sealed class ManualRtcClientInviteData : ClientInviteData
{
    public required string Sdp { get; init; }
}

public sealed class ManualRtcClientAnswerData
{
    public required string Sdp { get; init; }
}