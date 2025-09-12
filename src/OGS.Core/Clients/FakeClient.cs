using OGS.Core.Common.Audio;
using OGS.Core.Common.Input;
using OGS.Core.Common.Video;

namespace OGS.Core.Clients;

public sealed class FakeClient : ClientBase
{
    public FakeClient(FakeClientOptions options) : base(options)
    {

    }

    public sealed class FakeClientOptions : ClientOptions
    {
        
    }

    public static FakeClient Create(string name) => new FakeClient(new FakeClientOptions { Id = Guid.NewGuid(), Name = name });

    public override void TrySendAudioSample(EncodedAudioSample sample)
    {

    }

    public override void TrySendVideoFrame(EncodedVideoFrame frame)
    {

    }

    public override void Dispose()
    {

    }

    protected override void SendInputMethodsUpdate(InputMethods methods)
    {

    }
}
