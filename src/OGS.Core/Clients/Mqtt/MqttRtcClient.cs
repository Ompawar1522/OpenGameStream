using System.Text.Json;
using DataChannelDotnet;
using DataChannelDotnet.Bindings;
using DataChannelDotnet.Data;
using MQTTnet;
using OGS.Core.Clients.Rtc;
using OGS.Core.Common.Video;

namespace OGS.Core.Clients.MqttRtc;

/// <summary>
/// Webtc client that uses MQTT over Websockets for signaling.
/// 
/// Invite links are generated that contain the MQTT connection details and AES key for encryption.
/// </summary>
public sealed class MqttRtcClient : RtcClientBase
{
    private static readonly Log Log = LogManager.GetLogger<MqttRtcClient>();

    private readonly MqttRtcClientOptions _clientOptions;

    private readonly IMqttClient _client;
    private readonly MqttClientOptions _mqttClientOptions;
    private readonly Lock _lock = new Lock();

    private readonly CancellationTokenSource _disposeTokenSource = new();
    private readonly CancellationToken _disposeToken;
    private readonly HashSet<Guid> _processedMessageIds = new();

    private readonly string _hostTopic = Guid.NewGuid().ToString();
    private readonly string _clientTopic = Guid.NewGuid().ToString();
    private readonly AesContext _aesContext;

    private Guid _currentSessionId;
    private Task? _backgroundTask;

    public MqttRtcClient(MqttRtcClientOptions options) : base(options)
    {
        _clientOptions = options;
        _disposeToken = _disposeTokenSource.Token;
        
        _aesContext = AesContext.GenerateRandom();

        _mqttClientOptions = new MqttClientOptionsBuilder()
            .WithWebSocketServer(o => o.WithUri(_clientOptions.MqttWebsocketUrl))
            .WithCredentials(_clientOptions.Username, _clientOptions.Password)
            .WithTlsOptions(o => o.UseTls())
            .WithCleanStart(true)
            .Build();

        _client = new MqttClientFactory().CreateMqttClient();
        _client.ApplicationMessageReceivedAsync += OnMqttMessage;
        _client.DisconnectedAsync += _client_DisconnectedAsync;
    }

    private Task _client_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        Log.Info($"{Info.Name}: MQTT disconnected");
        return Task.CompletedTask;
    }

    public void Initialize()
    {
        using (_lock.EnterScope())
        {
            if (_disposeToken.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(MqttRtcClient));

            if (_backgroundTask is not null)
                throw new InvalidOperationException();

            Events.OnInviteCode.Raise(this, GenerateInviteLink());
            _backgroundTask = BackgroundTaskStart();
        }
    }

    private string GenerateInviteLink()
    {
        MqttRtcClientInviteData data = new MqttRtcClientInviteData
        {
            ClientTopic = _clientTopic,
            HostTopic = _hostTopic,
            WebsocketUrl = _clientOptions.MqttWebsocketUrl,
            Password = _clientOptions.Password,
            Username = _clientOptions.Username,
            AesKey = _aesContext.SecretBase64
        };

        return Base64Helpers.EncodeBase64String(JsonSerializer.Serialize(data, ClientInviteDataJsonContext.Default.ClientInviteData));
    }

    private async Task BackgroundTaskStart()
    {
        while (!_disposeTokenSource.IsCancellationRequested)
        {
            try
            {
                if (!await _client.TryPingAsync(_disposeToken))
                {
                    Log.Info($"{Info.Name}: MQTT -> Client connecting to {_clientOptions.MqttWebsocketUrl}...");
                    var result = await _client.ConnectAsync(_mqttClientOptions, _disposeToken);

                    if (result.ResultCode == MqttClientConnectResultCode.Success)
                    {
                        Log.Info($"{Info.Name}: MQTT -> connected to {_clientOptions.MqttWebsocketUrl}");
                        await _client.SubscribeAsync(_hostTopic, cancellationToken: _disposeToken);
                        await TrySendAsync(new MqttHostHelloMessage());
                    }
                    else
                    {
                        Log.Error($"{Info.Name}: MQTT connection failed: {result.ReasonString}");
                    }
                }
            }
            catch (TaskCanceledException)
            {
                Log.Info($"{Info.Name}:MQTT -> connection closed");
            }
            catch (Exception ex)
            {
                Log.Error($"{Info.Name}: MQTT -> connection failed", ex);
            }
            finally
            {
                await TryWait(TimeSpan.FromSeconds(2));
            }
        }

        Log.Info($"{Info.Name} -> Background task stopped");
    }

    private async Task<bool> TryWait(TimeSpan timeSpan)
    {
        try
        {
            await Task.Delay(timeSpan, _disposeToken);
            return true;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    private async Task<bool> TrySendAsync(MqttMessage message)
    {
        try
        {
            message.MessageId = Guid.NewGuid();
            var encryptResult = EncryptMessage(message);

            await _client.PublishStringAsync(_clientTopic, encryptResult,
                MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce, false, _disposeToken);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"{Info.Name}: Failed to send mqtt message", ex);
            return false;
        }
    }

    private async Task OnMqttMessage(MqttApplicationMessageReceivedEventArgs arg)
    {
        if (arg.ApplicationMessage.Topic != _hostTopic)
            return;
        try
        {
            string payload = arg.ApplicationMessage.ConvertPayloadToString();
            MqttMessage message = DecryptMessage(payload);

            lock (_processedMessageIds)
            {
                if (message.MessageId != Guid.Empty && _processedMessageIds.Contains(message.MessageId))
                {
                    Log.Warn($"{Info.Name}: Duplicate message received: {message.MessageId}");
                    return;
                }

                _processedMessageIds.Add(message.MessageId);
            }

            await HandleMqttMessage(message);
        }
        catch (Exception ex)
        {
            Log.Error($"{Info.Name}: Failed to handle MQTT message", ex);
        }
    }

    private async Task HandleMqttMessage(MqttMessage message)
    {
        if (message is MqttClientHelloMessage helloMsg)
            await HandleMqttClientHello(helloMsg);
        else if (message is MqttClientStartSessionMessage startMsg)
            await HandleMqttClientStartSession(startMsg);
        else if (message is MqttRtcCandidateMessage candidateMsg)
            await HandleMqttCandidate(candidateMsg);
        else if (message is MqttRtcAnswerMessage answerMsg)
            await HandleMqttAnswerMessage(answerMsg);
    }

    private async Task HandleMqttClientHello(MqttClientHelloMessage m)
    {
        await TrySendAsync(new MqttHostHelloMessage());
    }

    private Task HandleMqttClientStartSession(MqttClientStartSessionMessage m)
    {
        using (_lock.EnterScope())
        {
            if (_disposeToken.IsCancellationRequested)
                return Task.CompletedTask;

            _currentSessionId = m.SessionId;
            Log.Info($"{Info.Name} -> start session {_currentSessionId}");

            RecreatePeer(VideoCodec.H264);
        }
        
        return Task.CompletedTask;
    }

    private Task HandleMqttCandidate(MqttRtcCandidateMessage m)
    {
        using (_lock.EnterScope())
        {
            if (_disposeToken.IsCancellationRequested)
                return Task.CompletedTask;
            
            if (_currentSessionId != m.SessionId)
            {
                Log.Warn($"{Info.Name}: Ignoring candidate for wrong session ID");
                return Task.CompletedTask;
            }

            if (peer is null)
            {
                Log.Warn($"{Info.Name}: Received candidate but peer was null");
                return Task.CompletedTask;
            }
            
            try
            {
                peer.AddRemoteCandidate(new RtcCandidate{Content = m.Candidate, Mid = m.Mid});
                Log.Info($"{Info.Name}: Added remote candidate");
            }
            catch (Exception ex)
            {
                Log.Error($"{Info.Name}: Failed to add RTC candidate", ex);
            }
        }
        
        return Task.CompletedTask;
    }

    private Task HandleMqttAnswerMessage(MqttRtcAnswerMessage m)
    {
        using (_lock.EnterScope())
        {
            if (_disposeToken.IsCancellationRequested)
                return Task.CompletedTask;
            
            if (_currentSessionId != m.SessionId)
            {
                Log.Warn($"{Info.Name}: Ignoring answer for wrong session ID");
                return Task.CompletedTask;
            }

            if (peer is null)
            {
                Log.Warn($"{Info.Name}: Received answer but peer was null");
                return Task.CompletedTask;
            }

            try
            {
                peer.SetRemoteDescription(new RtcDescription{Sdp = m.AnswerSdp, Type = RtcDescriptionType.Answer});
            }
            catch (Exception ex)
            {
                Log.Error($"{Info.Name}: Failed to set RTC answer", ex);
            }
        }
        
        return Task.CompletedTask;
    }

    protected override void OnPeerLocalDescription(IRtcPeerConnection sender, RtcDescription description)
    {
        Log.Info("Peer local description generated!");

        if (description.Type == RtcDescriptionType.Offer)
        {
            using (_lock.EnterScope())
            {
                if (sender != peer)
                    return;
            
                if(_disposeToken.IsCancellationRequested)
                    return;
            
                Log.Info($"{Info.Name}: Generated RTC offer");
            
                _ = TrySendAsync(new MqttRtcOfferMessage()
                {
                    SessionId = _currentSessionId,
                    OfferSdp = AppendPlayoutDelayExtensionToSdp(description.Sdp)
                });
            }
        }
    }

    protected override void OnPeerCandidateGenerated(IRtcPeerConnection sender, RtcCandidate e)
    {
        Log.Info("Peer local candidate generated!");

        using (_lock.EnterScope())
        {
            if (sender != peer)
                return;
            
            if (_disposeToken.IsCancellationRequested)
                return;
            
            Log.Info($"{Info.Name}: Generated RTC candidate");
            
            _ = TrySendAsync(new MqttRtcCandidateMessage()
            {
                Candidate = e.Content,
                SessionId = _currentSessionId,
                Mid = e.Mid
            });
        }
    }

    protected override void OnPeerConnectionStateChange(IRtcPeerConnection sender, rtcState e)
    {
        using (_lock.EnterScope())
        {
            if (sender == peer)
            {
                Log.Info($"{Info.Name}: RTC connection state -> {e.ToString()}");
            }
            
            base.OnPeerConnectionStateChange(sender, e);
        }
    }

    internal string EncryptMessage(MqttMessage message)
    {
        string json = JsonSerializer.Serialize(message, MqttJsonContext.Default.MqttMessage);
        string encrypted = _aesContext.EncryptStringToBase64String(json, out string iv, out string tag);

        MqttMessageRoot root = new MqttMessageRoot
        {
            Iv = iv,
            Payload = encrypted,
            Tag = tag
        };

        return Base64Helpers.EncodeBase64String(JsonSerializer.Serialize(root, MqttJsonContext.Default.MqttMessageRoot));
    }

    internal MqttMessage DecryptMessage(string payload)
    {
        payload = Base64Helpers.DecodeBase64String(payload);

        MqttMessageRoot root = JsonSerializer.Deserialize(payload, MqttJsonContext.Default.MqttMessageRoot)
            ?? throw new InvalidDataException("Invalid Mqtt message");

        string body = _aesContext.DecryptStringFromBase64String(root.Payload, root.Iv, root.Tag);

        MqttMessage message = JsonSerializer.Deserialize(body, MqttJsonContext.Default.MqttMessage)
            ?? throw new InvalidDataException("Invalid Mqtt message");

        return message;
    }

    public override void Dispose()
    {
        using (_lock.EnterScope())
        {
            if (_disposeToken.IsCancellationRequested)
                return;

            try
            {
                _disposeTokenSource.Cancel();
            }
            catch (Exception)
            {
                //Ignore?
            }

            ClosePeer();
            _client.Dispose();

            if (Connected)
            {
                Connected = false;
                Events.OnDisconnected.Raise(this);
            }
        }
    }
}