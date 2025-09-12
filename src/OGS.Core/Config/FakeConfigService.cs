namespace OGS.Core.Config;

public sealed class FakeConfigService : IConfigService
{
    public Event OnChanged { get; } = new();

    private AppConfig _config = new AppConfig();
    private readonly Lock _lock = new Lock();

    public T Get<T>(Func<AppConfig, T> getter)
    {
        using (_lock.EnterScope())
        {
            return getter(_config);
        }
    }

    public void RestoreDefaults()
    {
        using (_lock.EnterScope())
        {
            _config = new AppConfig();
        }
    }

    public void Update(Action<AppConfig> setter)
    {
        using (_lock.EnterScope())
        {
            setter(_config);
        }
    }
}
