using OGS.Core.Clients;

namespace OGS.Core.Host;

public sealed class HostEvents
{
    public Event<ClientBase> OnClientCreated { get; } = new();
    public Event<ClientBase> OnClientRemoved { get; } = new();
}
