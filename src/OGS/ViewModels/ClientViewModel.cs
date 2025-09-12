using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OGS.Core.Clients;
using OGS.Core.Host;
using System.Threading.Tasks;
using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using OGS.Core.Clients.ManualRtc;
using OGS.Core.Clients.MqttRtc;
using ZeroLog;
using OGS.Core.Common.Input;
using OGS.Core.Ui;

namespace OGS.ViewModels;

public sealed partial class ClientViewModel : ObservableObject
{
    private static readonly Log Log = LogManager.GetLogger<ClientViewModel>();

    public ClientBase Client { get; }
    public string Name { get => Client.Info.Name; }

    [ObservableProperty] private bool _connected;
    [ObservableProperty] private bool _mouseEnabled;
    [ObservableProperty] private bool _keyboardEnabled;
    [ObservableProperty] private bool _gamepadEnabled;
    [ObservableProperty] private bool _inviteCodeGenerated;
    
    [ObservableProperty] private bool _waitingForAnswer;
    [ObservableProperty] private bool _hasInviteCode;

    [ObservableProperty] private string _connectionMessage = "...";

    private readonly Window _mainWindow;
    private readonly IHostContext _host;
    private readonly IDialogManager _dialogManager;
    private string? _inviteCode;

    public ClientViewModel(ClientBase client,
        Window mainWindow,
        IHostContext hostContext,
        IDialogManager dialogManager)
    {
        Client = client;
        _mainWindow = mainWindow;
        _host = hostContext;
        _dialogManager = dialogManager;
        client.Events.OnConnected.Subscribe(OnClientConnected);
        client.Events.OnDisconnected.Subscribe(OnClientDisconnected);
        Client.Events.OnInviteCode.Subscribe(OnClientInviteCodeGenerated);
        Client.Events.OnInputMethodsChanged.Subscribe(OnClientInputMethodsChanged);

        if (client is MqttRtcClient)
            HasInviteCode = true;

        UpdateInputMethods();
    }

    [RelayCommand]
    public Task RemoveClient()
    {
        _host.RemoveClient(Client);
        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task CopyInviteCode()
    {
        if (_inviteCode is not null)
        {
            await _mainWindow.Clipboard!.SetTextAsync(_inviteCode);
            Log.Info($"{Client.Info.Name}: Copied invite code");
        }
        else
        {
            Log.Error($"{Client.Info.Name}: Invite code was null");
        }
    }

    [RelayCommand]
    public async Task PasteAnswerCode()
    {
        try
        {
            if (Client is ManualRtcClient m)
            {
                m.SetAnswerCode(await _mainWindow.Clipboard!.GetTextAsync() ?? string.Empty);
                WaitingForAnswer = false;
            }
        }
        catch (Exception ex)
        {
            await _dialogManager.ShowMessageBoxAsync(MessageBoxManager.GetMessageBoxStandard("Error", $"Failed to process client answer: {ex}",
                ButtonEnum.Ok,
                Icon.Error, WindowStartupLocation.CenterOwner));
        }
    }

    [RelayCommand]
    public Task ToggleMouseInput()
    {
        ToggleInput(InputMethods.Mouse);
        return Task.CompletedTask;
    }

    [RelayCommand]
    public Task ToggleKeyboardInput()
    {
        ToggleInput(InputMethods.Keyboard);
        return Task.CompletedTask;
    }

    [RelayCommand]
    public Task ToggleGamepadInput()
    {
        ToggleInput(InputMethods.Gamepad);
        return Task.CompletedTask;
    }

    private void OnClientInputMethodsChanged(InputMethods methods)
    {
        Dispatcher.UIThread.Post(() =>
        {
            UpdateInputMethods();
        });
    }


    private void ToggleInput(InputMethods method)
    {
        InputMethods current = Client.InputMethods;

        if (current.HasFlag(method))
            Client.InputMethods &= ~method;
        else
            Client.InputMethods |= method;

        UpdateInputMethods();
    }

    [RelayCommand]
    public Task ShowInfo()
    {
        return Task.CompletedTask;
    }

    private void OnClientConnected(ClientBase obj)
    {
        Dispatcher.UIThread.Invoke(() => Connected = true);
    }

    private void OnClientDisconnected(ClientBase obj)
    {
        Dispatcher.UIThread.Invoke(() => Connected = false);
    }

    private void UpdateInputMethods()
    {
        MouseEnabled = Client.InputMethods.HasFlag(InputMethods.Mouse);
        KeyboardEnabled = Client.InputMethods.HasFlag(InputMethods.Keyboard);
        GamepadEnabled = Client.InputMethods.HasFlag(InputMethods.Gamepad);
    }

    private void OnClientInviteCodeGenerated(ClientBase obj, string? code)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (code != null)
            {
                _inviteCode = code;
                InviteCodeGenerated = true;
            }
        });
        
    }
}