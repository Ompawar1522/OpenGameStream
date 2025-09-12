namespace OGS.Core.Config;

public interface IConfigService
{
    Event OnChanged { get; }

    T Get<T>(Func<AppConfig, T> getter);
    void Update(Action<AppConfig> setter);

    void RestoreDefaults();
}