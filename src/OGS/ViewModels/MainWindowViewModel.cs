using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataChannelDotnet;
using DataChannelDotnet.Bindings;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using OGS.Core.Clients;
using OGS.Core.Common;
using OGS.Core.Config;
using OGS.Core.Platform;
using OGS.Views;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using OGS.Core.Clients.ManualRtc;
using OGS.ViewModels.Dialogs;
using OGS.Views.Dialogs;
using ZeroLog;
using OGS.Core.Host;
using OGS.Core;
using OGS.Plaform.Windows.Views;
using OGS.Plaform.Windows.ViewModels;
using OGS.Core.Ui;
using OGS.Services;
using System.Diagnostics;

namespace OGS.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private static readonly Log Log = LogManager.GetLogger<MainWindowViewModel>();

    [ObservableProperty] private Control? _previewControl;
    [ObservableProperty] private Control? _quickSwitchControl;

    public string WebClientUrl { get; } = BuildInfo.ClientUrl;
    public string Version { get; } = BuildInfo.AppVersion;

    public ObservableCollection<string> LogEntries { get; } = new();
    public ObservableCollection<ClientViewModel> Clients { get; } = new();

    private ScrollViewer? _logScrollViewer;
    private bool _autoScrollLog = true;

    private readonly IPlatform _platform;
    private readonly IHostContext _hostContext;
    private readonly IConfigService _configService;
    private readonly IDialogManager _dialogManager;

    private Window _windowInstance = null!;

    public MainWindowViewModel(IPlatform platform,
        IHostContext hostContext,
        IConfigService configService,
        IDialogManager dialogManager)
    {
        _platform = platform;
        _hostContext = hostContext;
        _configService = configService;
        _dialogManager = dialogManager;

        _hostContext.Events.OnClientCreated.Subscribe(OnClientCreated);
        _hostContext.Events.OnClientRemoved.Subscribe(OnClientRemoved);

        RtcTools.OnUnhandledException += exception => { Log.Error(exception.ToString()); };
    }

    private void OnClientCreated(ClientBase client)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            Clients.Add(new ClientViewModel(client, _windowInstance, _hostContext, _dialogManager));

            if (client is ManualRtcClient manualRtcClient)
            {
                new ManualClientSetupWindow()
                {
                    DataContext = new ManualClientSetupVm(_hostContext, manualRtcClient, _dialogManager)
                }.Show();
            }
        });
    }

    private void OnClientRemoved(ClientBase client)
    {
        Dispatcher.UIThread.Invoke(() => { Clients.RemoveWhere(x => x.Client == client); });
    }

    public async void OnWindowLoaded(MainWindow windowInstance)
    {
        if (Design.IsDesignMode)
            return;

        try
        {
            _windowInstance = windowInstance;
            ((DialogManager)_dialogManager).MainWindowInstance = windowInstance;

            _logScrollViewer = windowInstance.logScrollViewer;

            ZeroLogSetup.UseCallbackLogger(str =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (LogEntries.Count > 500)
                        LogEntries.RemoveAt(0);

                    LogEntries.Add(str);

                    if (_autoScrollLog)
                        _logScrollViewer?.ScrollToEnd();
                });
            }, LogLevel.Trace);

            RtcLog.Initialize(rtcLogLevel.RTC_LOG_INFO,
                ((level, message) => { LogManager.GetLogger("RTC").Info($"RTC ({level}: {message})"); }));

            try
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to set process priority", ex);
            }

            PreviewControl = _platform.CreatePreviewControl();
            QuickSwitchControl = _platform.CreateQuickSwitchControl();
           
            _hostContext.Initialize();
        }
        catch (Exception ex)
        {
            _hostContext.Dispose();

            await _dialogManager.ShowMessageBoxAsync(MessageBoxManager.GetMessageBoxStandard("Error", $"Failed to initialize application: {ex}",
                ButtonEnum.Ok,
                Icon.Error, WindowStartupLocation.CenterOwner));

            LogManager.Shutdown();
            windowInstance.Close();
        }
    }

    public void OnWindowClosing()
    {
        _hostContext.Dispose();
        LogManager.Shutdown();
    }

    [RelayCommand]
    public Task CreateClient()
    {
        _hostContext.CreateClient("Client" + new Random().Next(1, 999));

        return Task.CompletedTask;
    }

    [RelayCommand]
    public Task ShowClientSettings()
    {
        return new ClientSettingsWindow()
        {
            DataContext = new ClientSettingsWindowVm(_configService)
        }.ShowDialog(_windowInstance);
    }

    [RelayCommand]
    public Task ShowCaptureSettings()
    {
        return new CaptureSettingsWindow()
        {
            DataContext = new CaptureSettingsWindowVm(_configService)
        }.ShowDialog(_windowInstance);
    }

    [RelayCommand]
    public async Task RestoreDefaultConfig()
    {
        var result = await _dialogManager.ShowMessageBoxAsync(MessageBoxManager.GetMessageBoxStandard("Confirm",
            $"All settings will be restored to default. Do you want to continue?",
            ButtonEnum.YesNo,
            Icon.Error, WindowStartupLocation.CenterOwner));

        if (result == ButtonResult.Yes)
        {
            _configService.RestoreDefaults();
        }
    }


    public async void OnClientUrlClicked()
    {
        try
        {
            await _windowInstance!.Clipboard!.SetTextAsync(WebClientUrl);
        }catch(Exception ex)
        {
            Log.Error("Failed to copy URL", ex);
        }
    }

    #region Designer

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public MainWindowViewModel()
    {
#pragma warning disable CA1416 // Validate platform compatibility
        QuickSwitchControl = new WinQuickSwitchControl()
        {
            DataContext = new WinQuickSwitchVm()
        };
#pragma warning restore CA1416 // Validate platform compatibility


        Clients.Add(new ClientViewModel(FakeClient.Create("Client1"), null, null, null));
        Clients.Add(new ClientViewModel(FakeClient.Create("Client2"), null, null, null));
        Clients.Add(new ClientViewModel(FakeClient.Create("Client3"), null, null, null));
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

    #endregion
}