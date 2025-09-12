using OGS.Core.Clients;
using OGS.Core.Common.Audio;
using OGS.Core.Common.Video;
using OGS.Core.Platform;

namespace OGS.Core.Host;

public sealed class HostMediaHandler
{
    private static readonly Log Log = LogManager.GetLogger<HostMediaHandler>();

    private readonly IConfigService _configService;
    private readonly IPlatform _platform;

    private readonly List<ClientBase> _videoClients = new();
    private readonly List<ClientBase> _audioClients = new();

    public HostMediaHandler(IConfigService configService,
        IPlatform platform)
    {
        _configService = configService;
        _platform = platform;

        _platform.Events.OnVideoFrame.Subscribe(OnVideoFrame);
        _platform.Events.OnAudioSample.Subscribe(OnAudioSample);
    }

    public void AddClient(ClientBase client)
    {
        client.Events.OnVideoPli.Subscribe(OnVideoPli);
        client.Events.OnConnected.Subscribe(OnClientConnected);
        client.Events.OnDisconnected.Subscribe(OnClientDisconnected);
    }

    public void RemoveClient(ClientBase client)
    {
        client.Events.OnVideoPli.Unsubscribe(OnVideoPli);
        client.Events.OnConnected.Unsubscribe(OnClientConnected);
        client.Events.OnDisconnected.Unsubscribe(OnClientDisconnected);
    }

    private void OnAudioSample(EncodedAudioSample sample)
    {
        lock (_audioClients)
        {
            foreach(var client in _audioClients)
            {
                try
                {
                    client.TrySendAudioSample(sample);
                }catch(Exception ex)
                {
                    Log.Error($"{client.Info.Name}: Failed to send audio sample", ex);
                }
            }
        }
    }

    private void OnVideoFrame(EncodedVideoFrame frame)
    {
        lock (_videoClients)
        {
            foreach (var client in _videoClients)
            {
                try
                {
                    client.TrySendVideoFrame(frame);
                }
                catch (Exception ex)
                {
                    Log.Error($"{client.Info.Name}: Failed to send video frame", ex);
                }
            }
        }
    }
    private void OnClientConnected(ClientBase client)
    {
        lock (_audioClients)
            _audioClients.Add(client);

        lock (_videoClients)
            _videoClients.Add(client);

        _platform.RequestKeyFrame();
    }

    private void OnClientDisconnected(ClientBase client)
    {
        lock (_audioClients)
            _audioClients.Remove(client);

        lock (_videoClients)
            _videoClients.Remove(client);
    }

    private void OnVideoPli(ClientBase client)
    {
        _platform.RequestKeyFrame();
        Log.Info($"{client.Info.Name}: Requested key frame");
    }
}
