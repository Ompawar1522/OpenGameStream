using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using OGS.Core.Clients;
using OGS.Core.Clients.ManualRtc;
using OGS.Core.Host;
using OGS.Core.Ui;
using OGS.Views.Dialogs;
using TerraFX.Interop.Windows;

namespace OGS.ViewModels.Dialogs;

public sealed partial class ManualClientSetupVm : ObservableObject
{
    [ObservableProperty] private bool _canPasteAnswer;
    [ObservableProperty] private bool _canCopyInvite;
    [ObservableProperty] private string _loadingText = "Generating invite code...";
    [ObservableProperty] private bool _busy = true;
    
    private readonly IHostContext _session;
    private readonly ManualRtcClient _client;
    private readonly IDialogManager _dialogManager;
    private bool _answerPasted;
    private string? _inviteCode;
    
    private ManualClientSetupWindow? _window;

    public ManualClientSetupVm(IHostContext session, ManualRtcClient client, IDialogManager dialogManager)
    {
        _session = session;
        _client = client;
        _dialogManager = dialogManager;
        _session.Events.OnClientRemoved.Subscribe(OnClientRemoved);
        client.Events.OnInviteCode.Subscribe(OnClientInviteCode);
    }

    private void OnClientInviteCode(ClientBase _, string code)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            CanCopyInvite = true;
            _inviteCode = code; 
            LoadingText = "Invite code generated...";
            Busy = false;
        });
    }

    [RelayCommand]
    public async Task CopyCode()
    {
        CanPasteAnswer = true;
        await _window!.Clipboard!.SetTextAsync(_inviteCode!);
    }

    [RelayCommand]
    public async Task PasteAnswer()
    {
        try
        {
            Busy = true;
            LoadingText = "Handling answer code";
            _client.SetAnswerCode(await _window!.Clipboard!.GetTextAsync() ?? string.Empty);
            CanPasteAnswer = false;
            CanCopyInvite = false;
            _answerPasted = true;
            Busy = false;
            this._window.Close();
        }
        catch (Exception ex)
        {
            LoadingText = "Invalid answer code";

            await _dialogManager.ShowMessageBoxAsync(MessageBoxManager.GetMessageBoxStandard("Error", $"Failed to process client answer: {ex}",
                ButtonEnum.Ok,
                Icon.Error, WindowStartupLocation.CenterOwner));
        }
        finally
        {
            Busy = false;
        }
    }

    public void OnWindowLoad(ManualClientSetupWindow windowInstance)
    {
        _window = windowInstance;
    }

    public void OnWindowClosing()
    {
        _session.Events.OnClientRemoved.Unsubscribe(OnClientRemoved);

        if (!_answerPasted)
        {
            _session.RemoveClient(_client);
        }
    }

    private void OnClientRemoved(ClientBase obj)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (obj == _client)
            {
                _window?.Close();   
            }
        });
    }

    #region Designer
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public ManualClientSetupVm()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        
    }
    #endregion

}