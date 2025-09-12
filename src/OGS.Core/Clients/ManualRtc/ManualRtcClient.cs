using DataChannelDotnet;
using DataChannelDotnet.Bindings;
using DataChannelDotnet.Data;
using OGS.Core.Clients.Rtc;
using System.Text.Json;

namespace OGS.Core.Clients.ManualRtc;

public sealed class ManualRtcClient : RtcClientBase
{
    private static readonly Log Log = LogManager.GetLogger<ManualRtcClient>();

    private bool _disposed;
    private bool _answerGenerated;

    public ManualRtcClient(ManualRtcClientOptions options) : base(options)
    {
        
    }

    public void GenerateOffer()
    {
        using (peerLock.EnterScope())
        {
            if (_disposed)
                ObjectDisposedException.ThrowIf(_disposed, this);

            if (_answerGenerated)
                return;

            _answerGenerated = true;
            CreatePeer(Common.Video.VideoCodec.H264);
            peer!.SetLocalDescription(RtcDescriptionType.Offer);
        }
    }

    public void SetAnswerCode(string answerBase64)
    {
        using (peerLock.EnterScope())
        {
            if (_disposed)
                ObjectDisposedException.ThrowIf(_disposed, this);

            string json = Base64Helpers.DecodeBase64String(answerBase64);

            ManualRtcClientAnswerData data = JsonSerializer.Deserialize(json, ClientInviteDataJsonContext.Default.ManualRtcClientAnswerData)
                ?? throw new JsonException("Failed to deserialize ManualRtcClientAnswerData");

            if (peer is null)
                throw new InvalidOperationException("Peer is null");

            peer.SetRemoteDescription(new RtcDescription
            {
                Sdp = data.Sdp,
                Type = RtcDescriptionType.Answer
            });

            Log.Info($"{Info.Name}: Set remote answer from client.");
        }
    }

    protected override void OnPeerGatheringStateChange(IRtcPeerConnection sender, rtcGatheringState state)
    {
        using (peerLock.EnterScope())
        {
            if (_disposed)
                return;

            if(state == rtcGatheringState.RTC_GATHERING_COMPLETE)
            {
                try
                {
                    Log.Info($"{Info.Name}: Rtc gathering complete. Generating invite code");

                    string? description = peer!.LocalDescription;

                    if (string.IsNullOrEmpty(description))
                    {
                        Log.Error($"{Info.Name}: local description was null or empty. Could not generate invite code");
                        return;
                    }

                    ManualRtcClientInviteData inviteData = new ManualRtcClientInviteData {
                        Sdp = AppendPlayoutDelayExtensionToSdp(description) 
                    };
                    string inviteCode = Base64Helpers.EncodeBase64String(JsonSerializer.Serialize(inviteData, ClientInviteDataJsonContext.Default.ClientInviteData));
                    Log.Info($"{Info.Name}: Generated invite code");
                    Events.OnInviteCode.Raise(this, inviteCode);
                }
                catch(Exception ex)
                {
                    Log.Error($"{Info.Name}: Failed to generate invite code", ex);
                }
            }
        }
    }

    public override void Dispose()
    {
        using (peerLock.EnterScope())
        {
            _disposed = true;
            ClosePeer();
        } 
    }
}
