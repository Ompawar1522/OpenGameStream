namespace OGS.Core.Clients;

public abstract class ClientOptions
{
    public required string Name { get; init; }
    public required Guid Id { get; init; }
}
