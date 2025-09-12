using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OGS.Core.Config.Data.Rtc;

namespace OGS.ViewModels.Dialogs;

public sealed partial class StunServerEditorVm : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string? _username;
    [ObservableProperty] private string? _password;
    [ObservableProperty] private bool _canSave;
    [ObservableProperty] private bool _isReadOnly;
    [ObservableProperty] private string? _validationError;

    public bool SaveEnabled => CanSave && !IsReadOnly;
    public string Title => IsReadOnly ? "STUN server (Read only)" : "STUN server";

    private readonly StunServerConfig? _existing;
    private Window? _window;

    public StunServerEditorVm(StunServerConfig? existing)
    {
        _existing = existing;
        if (existing is not null)
        {
            Name = existing.Name;
            Address = existing.Address;
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
        bool valid = !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Address);
        if (!valid)
        {
            CanSave = false;
            return;
        }
        CanSave = true;
    }

    partial void OnCanSaveChanged(bool value) => OnPropertyChanged(nameof(SaveEnabled));
    partial void OnIsReadOnlyChanged(bool value)
    {
        OnPropertyChanged(nameof(SaveEnabled));
        OnPropertyChanged(nameof(Title));
    }

    partial void OnNameChanged(string value) => UpdateCanSave();
    partial void OnAddressChanged(string value) => UpdateCanSave();

    [RelayCommand]
    private Task Cancel()
    {
        _window?.Close(null);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task Save()
    {
        if (!CanSave || IsReadOnly)
            return Task.CompletedTask;

        var config = new StunServerConfig
        {
            Name = Name.Trim(),
            Address = Address.Trim(),
            Username = string.IsNullOrWhiteSpace(Username) ? null : Username,
            Password = string.IsNullOrWhiteSpace(Password) ? null : Password,
            Id = _existing?.Id ?? Guid.NewGuid(),
            Immutable = _existing?.Immutable ?? false
        };

        _window?.Close(config);
        return Task.CompletedTask;
    }

    #region Designer
    public StunServerEditorVm() { }
    #endregion
}


