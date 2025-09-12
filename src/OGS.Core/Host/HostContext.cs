using OGS.Core.Clients;
using OGS.Core.Clients.ManualRtc;
using OGS.Core.Clients.MqttRtc;
using OGS.Core.Common.Input;
using OGS.Core.Config.Data.Clients;
using OGS.Core.Platform;

namespace OGS.Core.Host;

public sealed class HostContext : IHostContext
{
    private static readonly Log Log = LogManager.GetLogger<HostContext>();

    public HostEvents Events { get; } = new();

    private readonly IPlatform _platform;
    private readonly IConfigService _configService;
    private readonly HostMediaHandler _mediaHandler;
    private readonly HostInputHandler _inputHandler;

    private readonly Lock _lock = new Lock();
    private readonly List<ClientBase> _clients = new();

    private bool _disposed;

    public HostContext(IPlatform platform,
        IConfigService configService,
        HostMediaHandler mediaHandler,
        HostInputHandler inputHandler)
    {
        _platform = platform;
        _configService = configService;
        _mediaHandler = mediaHandler;
        _inputHandler = inputHandler;
    }

    public void Initialize()
    {
        try
        {
            using (_lock.EnterScope())
            {
                _platform.Initialize();
            }
        }
        catch (Exception ex)
        {
            Log.Error("HostContext failed to initialize", ex);
            Dispose();
            throw;
        }
    }

    public void CreateClient(string name)
    {
        using (_lock.EnterScope())
        {
            var clientConfig = _configService.Get(x => x.ClientConfig);

            if (clientConfig is ManualRtcClientConfig manualConfig)
            {
                CreateManualRtcClient(new ManualRtcClientOptions
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    ForceTurnServer = manualConfig.ForceTurnServer,
                    StunServer = manualConfig.StunServer,
                    TurnServer = manualConfig.TurnServer
                });
            }else if(clientConfig is MqttRtcClientConfig mqttConfig)
            {
                CreateMqttRtcClient(new MqttRtcClientOptions
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    ForceTurnServer = mqttConfig.ForceTurnServer,
                    StunServer = mqttConfig.StunServer,
                    TurnServer = mqttConfig.TurnServer,
                    MqttWebsocketUrl = mqttConfig.ServerConfig.WebsocketUrl,
                    Password = mqttConfig.ServerConfig.Password,
                    Username = mqttConfig.ServerConfig.Username
                });
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }

    public void CreateManualRtcClient(ManualRtcClientOptions options)
    {
        using (_lock.EnterScope())
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            ManualRtcClient client = new ManualRtcClient(options);

            try
            {
                SetupClient(client);
                client.GenerateOffer();
            }
            catch (Exception ex)
            {
                Log.Error("Failed to create manual Rtc client", ex);
                RemoveClient(client);
                throw;
            }
        }
    }

    public void CreateMqttRtcClient(MqttRtcClientOptions options)
    {
        using (_lock.EnterScope())
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            MqttRtcClient client = new MqttRtcClient(options);

            try
            {
                SetupClient(client);
                client.Initialize();
            }
            catch (Exception ex)
            {
                Log.Error("Failed to create MQTT Rtc client", ex);
                RemoveClient(client);
                throw;
            }
        }
    }

    private void SetupClient(ClientBase client)
    {
        client.Events.OnDisconnected.Subscribe(OnClientDisconnected);
        client.Events.OnConnected.Subscribe(OnClientConnected);

        _clients.Add(client);
        _mediaHandler.AddClient(client);
        _inputHandler.AddClient(client);
        client.InputMethods = InputMethods.Gamepad;

        Events.OnClientCreated.Raise(client);
        Log.Info($"Created client {client.Info.Name}");
    }

    public void RemoveClient(ClientBase client)
    {
        using (_lock.EnterScope())
        {
            if (client.Removing)
                return;

            Log.Info($"Removing client {client.Info.Name}");
            client.Removing = true;

            client.Events.OnDisconnected.Unsubscribe(OnClientDisconnected);
            client.Events.OnConnected.Unsubscribe(OnClientConnected);

            if (_clients.Remove(client))
            {
                _inputHandler.RemoveClient(client);
                _mediaHandler.RemoveClient(client);
                client.Dispose();
                Log.Info($"Removed client {client.Info.Name}");
                Events.OnClientRemoved.Raise(client);
            }
        }
    }

    private void OnClientDisconnected(ClientBase client)
    {
        using (_lock.EnterScope())
        {
            if (_disposed || client.Removing)
                return;

            //Manual clients can't reconnect, so can can just remove them
            //when disconnected
            if (client is ManualRtcClient)
            {
                RemoveClient(client);
            }
        }
    }

    private void OnClientConnected(ClientBase client)
    {
        using (_lock.EnterScope())
        {
            if (_disposed || client.Connected)
                return;


        }
    }

    public void Dispose()
    {
        using (_lock.EnterScope())
        {
            if (_disposed)
                return;

            _disposed = true;

            foreach (var client in _clients.ToArray())
            {
                RemoveClient(client);
            }

            _clients.Clear();
            _platform.Dispose();
        }
    }
}
