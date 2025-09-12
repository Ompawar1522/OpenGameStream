using DataChannelDotnet;
using DataChannelDotnet.Bindings;
using DataChannelDotnet.Data;
using DataChannelDotnet.Events;
using DataChannelDotnet.Impl;
using OGS.Core.Common.Audio;
using OGS.Core.Common.Input;
using OGS.Core.Common.Video;
using OGS.Core.Config.Data.Rtc;

namespace OGS.Core.Clients.Rtc;

public abstract class RtcClientBase : ClientBase
{
    private static readonly Log Log = LogManager.GetLogger<RtcClientBase>();

    private readonly RtcClientOptions _options;

    protected IRtcPeerConnection? peer;
    protected IRtcTrack? videoTrack;
    protected IRtcTrack? audioTrack;
    protected IRtcDataChannel? commandChannel;
    protected RtcCommandWriter? commandWriter;
    protected readonly Lock peerLock = new();

    private readonly RtcCommandParser _commandParser;

    protected RtcClientBase(RtcClientOptions options) : base(options)
    {
        _options = options;
        _commandParser = new RtcCommandParser(this);
    }

    public override void TrySendAudioSample(EncodedAudioSample sample)
    {
        IRtcTrack? track = audioTrack;

        if (track is not null && track.IsOpen)
        {
            try
            {
                track.Write(sample.Data);
                track.Timestamp += sample.SampleCount;
            }
            catch (Exception ex)
            {
                Log.Error($"{Info.Name}: Failed to write audio sample", ex);
            }
        }
    }

    public override void TrySendVideoFrame(EncodedVideoFrame frame)
    {
        IRtcTrack? track = videoTrack;

        if (track is not null && track.IsOpen)
        {
            try
            {
                track.Timestamp = (uint)(frame.Timestamp.TotalSeconds * 90000);
                track.Write(frame.Data);
            }
            catch (Exception ex)
            {
                Log.Error($"{Info.Name}: Failed to write video frame", ex);
            }
        }
    }

    protected override void SendInputMethodsUpdate(InputMethods methods)
    {
        var writer = commandWriter;

        if(Connected && writer is not null)
        {
            try
            {
                writer.SendInputMethodsUpdate(methods);
            }catch(Exception ex)
            {
                Log.Error($"{Info.Name}: Failed to send input methods update", ex);
            }
        }
    }

    protected string AppendPlayoutDelayExtensionToSdp(string sdp)
    {
        //Todo - this is very hacky
        return sdp.Replace("a=mid:video\r\n",
            "a=mid:video\r\na=extmap:11 http://www.webrtc.org/experiments/rtp-hdrext/playout-delay\r\n");
    }

    protected void RecreatePeer(VideoCodec codec)
    {
        using var _ = peerLock.EnterScope();

        ClosePeer();
        CreatePeer(codec);
    }

    protected void CreatePeer(VideoCodec videoCodec)
    {
        using var _ = peerLock.EnterScope();

        Log.Info($"{Info.Name}: Creating peer");

        try
        {
            peer = new RtcPeerConnection(GetPeerConfiguration());
            peer.OnCandidateSafe += OnPeerCandidateGenerated;
            peer.OnLocalDescriptionSafe += OnPeerLocalDescription;
            peer.OnConnectionStateChange += OnPeerConnectionStateChange;
            peer.OnGatheringStateChange += OnPeerGatheringStateChange;
            peer.OnSignalingStateChange += OnPeerSignalingStateChange;

            CreateH264Track();

            if (videoTrack is null)
                throw new NullReferenceException(nameof(videoTrack));

            videoTrack.OnOpen += OnVideoTrackOpen;
            videoTrack.OnClose += OnVideoTrackClosed;
            videoTrack.OnError += OnVideoTrackError;

            videoTrack.SetPliHandler(OnVideoTrackPli);
            videoTrack.AddRtcpNackResponder(1024);
            videoTrack.AddRtcpSrReporter();

            audioTrack = peer.CreateTrack(new RtcCreateTrackArgs()
            {
                Codec = rtcCodec.RTC_CODEC_OPUS,
                Direction = rtcDirection.RTC_DIRECTION_SENDONLY,
                Mid = "audio",
                Name = "opus",
                PayloadType = 111,
                Ssrc = 3,
            });

            audioTrack.AddOpusPacketizer(new RtcPacketizerInitArgs()
            {
                Clockrate = 48000,
                PayloadType = 111,
                Ssrc = 3,
                Cname = "opus"
            });

            audioTrack.OnOpen += OnAudioTrackOpen;
            audioTrack.OnClose += OnAudioTrackClosed;
            audioTrack.OnError += OnAudioTrackError;

            commandChannel = peer.CreateDataChannel(new RtcCreateDataChannelArgs()
            {
                Label = "command",
                Protocol = RtcDataChannelProtocol.Binary
            });

            commandChannel.OnOpen += OnCommandChannelOpen;
            commandChannel.OnClose += OnCommandChannelClosed;
            commandChannel.OnError += OnCommandChannelError;
            commandChannel.OnBinaryReceivedSafe += OnCommandChannelBinaryData;

            commandWriter = new RtcCommandWriter(commandChannel);

            peer.SetLocalDescription(RtcDescriptionType.Offer);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to create peer", ex);
            ClosePeer();
            throw;
        }

        Log.Info($"{Info.Name}: Peer created");
    }

    private void CreateH264Track()
    {
        if (peer is null)
            throw new NullReferenceException(nameof(peer));

        videoTrack = peer.CreateTrack(new RtcCreateTrackArgs()
        {
            Codec = rtcCodec.RTC_CODEC_H264,

            Direction = rtcDirection.RTC_DIRECTION_SENDONLY,
            Ssrc = 1,
            PayloadType = 96,
            Mid = "video",
            Name = "video",
        });

        videoTrack.AddH264Packetizer(new RtcPacketizerInitArgs()
        {
            NalUnitSeparator = rtcNalUnitSeparator.RTC_NAL_SEPARATOR_START_SEQUENCE,
            Clockrate = 90 * 1000,
            Ssrc = 1,
            PayloadType = 96,
            Timestamp = 0,
            SequenceNumber = 0,
            PlayoutDelayId = 11,
            PlayoutDelayMax = 0,
            PlayoutDelayMin = 0,
            Cname = "video"
        });
    }

    protected void ClosePeer()
    {
        using var _ = peerLock.EnterScope();
        Log.Info($"{Info.Name}: Closing peer");

        commandWriter = null;

        if (commandChannel is not null)
        {
            commandChannel.OnOpen -= OnCommandChannelOpen;
            commandChannel.OnClose -= OnCommandChannelClosed;
            commandChannel.OnError -= OnCommandChannelError;
            commandChannel.OnBinaryReceivedSafe -= OnCommandChannelBinaryData;

            commandChannel.Dispose();
            commandChannel = null;
        }

        if (videoTrack is not null)
        {
            videoTrack.OnOpen -= OnVideoTrackOpen;
            videoTrack.OnClose -= OnVideoTrackClosed;
            videoTrack.OnError -= OnVideoTrackError;

            videoTrack.Dispose();
            videoTrack = null;
        }

        if (audioTrack is not null)
        {
            audioTrack.OnOpen -= OnAudioTrackOpen;
            audioTrack.OnClose -= OnAudioTrackClosed;
            audioTrack.OnError -= OnAudioTrackError;

            audioTrack?.Dispose();
            audioTrack = null;
        }

        if (peer is not null)
        {
            peer.OnCandidateSafe -= OnPeerCandidateGenerated;
            peer.OnConnectionStateChange -= OnPeerConnectionStateChange;
            peer.OnGatheringStateChange -= OnPeerGatheringStateChange;
            peer.OnLocalDescriptionSafe -= OnPeerLocalDescription;
            peer.OnSignalingStateChange -= OnPeerSignalingStateChange;

            peer?.Dispose();
            peer = null;
        }

        Log.Info($"{Info.Name}: Peer closed");
    }

    protected virtual void OnPeerLocalDescription(IRtcPeerConnection sender, RtcDescription description)
    {
        if (Log.IsDebugEnabled)
        {
            Log.Debug($"{Info.Name}: Local description set: {description.Type.ToString()}");
        }
    }

    protected virtual void OnCommandChannelOpen(IRtcDataChannel sender)
    {
        SendInputMethodsUpdate(this.InputMethods);
        UpdateConnectedState();
    }

    protected virtual void OnCommandChannelError(IRtcDataChannel sender, string? e)
    {
        UpdateConnectedState();
    }

    protected virtual void OnCommandChannelClosed(IRtcDataChannel sender)
    {
        UpdateConnectedState();
    }

    protected virtual void OnCommandChannelBinaryData(IRtcDataChannel sender, RtcBinaryReceivedEventSafe @event)
    {
        try
        {
            _commandParser.TryParse(@event.Data);
        }
        catch (Exception ex)
        {
            Log.Error($"{Info.Name}: failed to parse command", ex);
        }
    }

    protected virtual void OnAudioTrackOpen(IRtcTrack sender)
    {
        UpdateConnectedState();
    }

    protected virtual void OnAudioTrackClosed(IRtcTrack sender)
    {
        UpdateConnectedState();
    }

    protected virtual void OnAudioTrackError(IRtcTrack sender, string? e)
    {
        UpdateConnectedState();
    }

    protected virtual void OnVideoTrackPli()
    {
        Log.Info($"{Info.Name}: Client requested key frame");
        Events.OnVideoPli.Raise(this);
    }

    protected virtual void OnVideoTrackError(IRtcTrack sender, string? e)
    {
        UpdateConnectedState();
    }

    protected virtual void OnVideoTrackOpen(IRtcTrack sender)
    {
        UpdateConnectedState();
    }

    protected virtual void OnVideoTrackClosed(IRtcTrack sender)
    {
        UpdateConnectedState();
    }

    protected virtual void OnPeerSignalingStateChange(IRtcPeerConnection sender, rtcSignalingState e)
    {
        if (Log.IsDebugEnabled)
        {
            Log.Debug($"{Info.Name}: Signaling state changed to {e.ToString()}");
        }
    }

    protected virtual void OnPeerGatheringStateChange(IRtcPeerConnection sender, rtcGatheringState e)
    {
        if (Log.IsDebugEnabled)
        {
            Log.Debug($"{Info.Name}: Gathering state changed to {e.ToString()}");
        }
    }

    protected virtual void OnPeerCandidateGenerated(IRtcPeerConnection sender, RtcCandidate e)
    {
        if (Log.IsDebugEnabled)
        {
            Log.Debug($"{Info.Name}: Candidate generated: {e.Mid}:{e.Content}");
        }
    }

    protected virtual void OnPeerConnectionStateChange(IRtcPeerConnection sender, rtcState e)
    {
        if (Log.IsDebugEnabled)
        {
            Log.Debug($"{Info.Name}: Connection state changed to {e.ToString()}");
        }

        UpdateConnectedState();
    }

    private void UpdateConnectedState()
    {
        using (peerLock.EnterScope())
        {
            IRtcPeerConnection? peer = this.peer;
            IRtcTrack? audioTrack = this.videoTrack;
            IRtcTrack? videoTrack = this.audioTrack;
            IRtcDataChannel? commandChannel = this.commandChannel;

            bool isConnected =
                peer is not null &&
                videoTrack is not null &&
                audioTrack is not null &&
                commandChannel is not null &&
                peer.ConnectionState == rtcState.RTC_CONNECTED &&
                audioTrack.IsOpen &&
                videoTrack.IsOpen &&
                commandChannel.IsOpen;

            if (isConnected && !Connected)
            {
                Connected = true;
                Events.OnConnected.Raise(this);
                SendInputMethodsUpdate(InputMethods);
                OnRtcConnected();
            }
            else if (!isConnected && Connected)
            {
                Connected = false;
                Events.OnDisconnected.Raise(this);
                OnRtcDisconnected();
            }
        }
    }

    protected virtual void OnRtcConnected() { }
    protected virtual void OnRtcDisconnected() { }

    protected virtual RtcPeerConfiguration GetPeerConfiguration()
    {
        List<string> iceServers = new List<string>();

        if (_options.StunServer != StunServerConfig.None)
        {
            string ice = _options.StunServer.ToIceServer();
            Log.Info($"Using STUN server {_options.StunServer.Name} ({_options.StunServer.Address})");
            iceServers.Add(ice);
        }

        if (_options.TurnServer != TurnServerConfig.None)
        {
            string ice = _options.TurnServer.ToIceServer();
            Log.Info($"Using TURN server {_options.TurnServer.Name} ({_options.TurnServer.Address})");
            iceServers.Add(ice);
        }

        rtcTransportPolicy transportPolicy = _options.ForceTurnServer
            ? rtcTransportPolicy.RTC_TRANSPORT_POLICY_RELAY
            : rtcTransportPolicy.RTC_TRANSPORT_POLICY_ALL;

        Log.Info($"Using transport policy {transportPolicy.ToString()}");


        return new RtcPeerConfiguration()
        {
            IceServers = iceServers.ToArray(),
            TransportPolicy = transportPolicy,
            DisableAutoNegotiation = true
        };
    }
}
