using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OGS.Core.Config;

namespace OGS.ViewModels.Dialogs;

public sealed partial class MqttServerEditorVm : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _websocketUrl = string.Empty;
    [ObservableProperty] private string? _username;
    [ObservableProperty] private string? _password;
    [ObservableProperty] private bool _canSave;
    [ObservableProperty] private bool _isReadOnly;
    [ObservableProperty] private string? _validationError;

    private readonly IConfigService _configService;
    private readonly MqttServerConfig? _existing;
    private Window? _window;

    public MqttServerEditorVm(IConfigService configService, MqttServerConfig? existing)
    {
        _configService = configService;
        _existing = existing;

        if (existing is not null)
        {
            Name = existing.Name;
            WebsocketUrl = existing.WebsocketUrl;
            Username = existing.Username;
            Password = existing.Password;
            IsReadOnly = existing.Immutable;
        }

        UpdateCanSave();
    }

    public void OnWindowLoad(Window window) => _window = window;

    private void UpdateCanSave()
    {
        ValidationError = null;
        bool valid = !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(WebsocketUrl);
        if (!valid)
        {
            CanSave = false;
            return;
        }
        if (!(WebsocketUrl.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) ||
              WebsocketUrl.StartsWith("wss://", StringComparison.OrdinalIgnoreCase)))
        {
            ValidationError = "Websocket URL must start with ws:// or wss://";
            CanSave = false;
            return;
        }
        CanSave = true;
    }

    public bool SaveEnabled => CanSave && !IsReadOnly;
    public string Title => IsReadOnly ? "MQTT server (Read only)" : "MQTT server";

    partial void OnCanSaveChanged(bool value) => OnPropertyChanged(nameof(SaveEnabled));
    partial void OnIsReadOnlyChanged(bool value)
    {
        OnPropertyChanged(nameof(SaveEnabled));
        OnPropertyChanged(nameof(Title));
    }

    partial void OnNameChanged(string value) => UpdateCanSave();
    partial void OnWebsocketUrlChanged(string value) => UpdateCanSave();

    [RelayCommand]
    private Task Cancel()
    {
        _window?.Close(null);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task Save()
    {
        if (!CanSave)
            return Task.CompletedTask;

        var config = new MqttServerConfig
        {
            Name = Name.Trim(),
            WebsocketUrl = WebsocketUrl.Trim(),
            Username = string.IsNullOrWhiteSpace(Username) ? null : Username,
            Password = string.IsNullOrWhiteSpace(Password) ? null : Password,
            Id = _existing?.Id ?? Guid.NewGuid(),
            Immutable = _existing?.Immutable ?? false
        };

        _window?.Close(config);
        return Task.CompletedTask;
    }

    #region Designer
    public MqttServerEditorVm()
    {
        _configService = new FakeConfigService();
    }
    #endregion
}


