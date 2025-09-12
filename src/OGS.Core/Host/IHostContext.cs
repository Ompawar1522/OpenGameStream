using OGS.Core.Clients;

namespace OGS.Core.Host;

public interface IHostContext : IDisposable
{
    HostEvents Events { get; }

    void Initialize();

    void RemoveClient(ClientBase client);
    void CreateClient(string name);
}
