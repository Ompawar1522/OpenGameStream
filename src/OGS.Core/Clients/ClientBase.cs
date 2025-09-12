using OGS.Core.Common.Audio;
using OGS.Core.Common.Input;
using OGS.Core.Common.Video;

namespace OGS.Core.Clients;

public abstract class ClientBase : IDisposable
{
    public ClientInfo Info { get; }
    public ClientsEvents Events { get; } = new();
    public bool Connected { get; protected set; }
    public bool Removing { get; set; }

    public InputMethods InputMethods
    {
        get => field;
        set{
            field = value;
            SendInputMethodsUpdate(value);
            Events.OnInputMethodsChanged.Raise(value);
        }
    }

    protected ClientBase(ClientOptions options)
    {
        Info = new ClientInfo() 
        { 
            Id = options.Id,
            Name = options.Name
        };
    }

    public abstract void TrySendVideoFrame(EncodedVideoFrame frame);
    public abstract void TrySendAudioSample(EncodedAudioSample sample);

    protected abstract void SendInputMethodsUpdate(InputMethods methods);


    public abstract void Dispose();
}
