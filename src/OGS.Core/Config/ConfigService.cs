using System.Text.Json;

namespace OGS.Core.Config;

public sealed class ConfigService : IConfigService
{
    private static readonly Log Log = LogManager.GetLogger<ConfigService>();

    public Event OnChanged { get; } = new();

    private readonly Lock _lock = new();
    private AppConfig _config;

    public ConfigService()
    {
        try
        {
            Log.Info("Loading configuration");
            _config = LoadConfig();
            Log.Info("Loaded configuration");
        } catch (Exception ex)
        {
            Log.Error("Failed to load configuration", ex);
            _config = new AppConfig();
            TrySaveConfig();
        }
    }

    public T Get<T>(Func<AppConfig, T> getter)
    {
        using (_lock.EnterScope())
        {
            return getter(_config);
        }
    }

    public void Update(Action<AppConfig> setter)
    {
        using (_lock.EnterScope())
        {
            setter(_config);
            TrySaveConfig();
            OnChanged.Raise();
        }
    }

    public void RestoreDefaults()
    {
        using (_lock.EnterScope())
        {
            _config = new AppConfig();
            Log.Info("Restored default configuration");
            TrySaveConfig();
        }
    }

    private void TrySaveConfig()
    {
        try
        {
            Log.Info("Saving configuration");
            string json = JsonSerializer.Serialize(_config, AppConfigJsonContext.Default.AppConfig);
            File.WriteAllText(GetFilePath(), json);
            Log.Info("Saved configuration");
        }catch(Exception ex)
        {
            Log.Error("Failed to save configuration", ex);
        }
    }

    private AppConfig LoadConfig()
    {
        string json = File.ReadAllText(GetFilePath());

        AppConfig? instance = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig)
            ?? throw new IOException("Failed to serialize configuration");

        return instance;
    }

    private string GetFilePath() => Path.Combine(AppContext.BaseDirectory, "config.json");
}
