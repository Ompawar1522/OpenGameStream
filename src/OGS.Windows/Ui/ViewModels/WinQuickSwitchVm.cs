using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using OGS.Core;
using OGS.Core.Common;
using OGS.Core.Config;
using OGS.Core.Ui;
using OGS.Windows;
using OGS.Windows.Common;
using TerraFX.Interop.Windows;
using WinDisplayInfo = OGS.Plaform.Windows.Models.WinDisplayInfo;
using WinGameInfo = OGS.Plaform.Windows.Models.WinGameInfo;
using WinWindowInfo = OGS.Plaform.Windows.Models.WinWindowInfo;

namespace OGS.Plaform.Windows.ViewModels;

public sealed partial class WinQuickSwitchVm : ObservableObject
{
    private readonly WindowsPlatform _platform;
    private readonly IConfigService _configService;
    private readonly IDialogManager _dialogManager;

    [ObservableProperty] private object? _currentCaptureItems;
    [ObservableProperty] private object? _selectedCaptureItem;
    
    [ObservableProperty] private double _bitrateMbps = 10;

    private readonly ObservableCollection<WinDisplayInfo> _displayCaptureItems = new();
    private readonly ObservableCollection<WinWindowInfo> _windowCaptureItems = new();
    private readonly ObservableCollection<WinGameInfo> _gameCaptureItems = new();

    private readonly DispatcherTimer _timer = new DispatcherTimer();

    public WinQuickSwitchVm(WindowsPlatform platform,
        IConfigService configService,
        IDialogManager dialogManager)
    {
        _platform = platform;
        _configService = configService;
        _dialogManager = dialogManager;
        _configService.OnChanged.Subscribe(OnConfigChanged);
    }

    private void OnConfigChanged()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            BitrateMbps = _configService.Get(x => x.Bitrate.MegaBitsPerSecond);
        });
    }

    public void OnWindowLoaded()
    {
        CurrentCaptureItems = _displayCaptureItems;
        OnTimerTick(null, EventArgs.Empty);

        _timer.Tick += OnTimerTick;
        _timer.Interval = TimeSpan.FromMilliseconds(1000);
        _timer.Start();

        BitrateMbps = _configService.Get(x => x.Bitrate.MegaBitsPerSecond);
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        UpdateDisplays();
        UpdateWindows();
        UpdateGames();

        if (CurrentCaptureItems == _displayCaptureItems && SelectedCaptureItem is null)
        {
            SelectedCaptureItem = _displayCaptureItems.FirstOrDefault();
        }
        else if (CurrentCaptureItems == _windowCaptureItems && SelectedCaptureItem is null)
        {
            SelectedCaptureItem = _windowCaptureItems.FirstOrDefault();
        }
        else if (CurrentCaptureItems == _gameCaptureItems && SelectedCaptureItem is null)
        {
            SelectedCaptureItem = _gameCaptureItems.FirstOrDefault();
        }
    }

    public void OnWindowUnloaded()
    {
        _timer.Stop();
    }

    private readonly List<HMONITOR> _tempDisplayList = new();

    private void UpdateDisplays()
    {
        Win32DisplayEnumerator.EnumerateDisplays(display =>
        {
            _tempDisplayList.Add(display);

            if (_displayCaptureItems.All(x => x.Handle != display))
            {
                display.TryGetName(out string name);
                _displayCaptureItems.Add(new WinDisplayInfo(display, name));
            }
        });

        _displayCaptureItems.RemoveWhere(x => !_tempDisplayList.Contains(x.Handle));

        _tempDisplayList.Clear();
    }

    private readonly List<HWND> _tempWindowList = new();

    private void UpdateWindows()
    {
        Win32WindowEnumerator.EnumerateWindows(window =>
        {
            if (!IsValidWindow(window, out string? title))
                return;

            _tempWindowList.Add(window);
            var existing = _windowCaptureItems.FirstOrDefault(x => x.Handle == window);

            if (existing is null)
            {
                _windowCaptureItems.Add(new WinWindowInfo(window, title));
            }
            else
            {
                existing.Name = title;
            }
        });

        _windowCaptureItems.RemoveWhere(x => { return !_tempWindowList.Contains(x.Handle); });

        _tempWindowList.Clear();
    }

    private bool IsValidWindow(HWND hWnd, [NotNullWhen(true)] out string? title)
    {
        title = null;

        if (!TerraFX.Interop.Windows.Windows.IsWindowVisible(hWnd))
            return false;

        if (!hWnd.TryGetTitle(out title))
            return false;

        if (string.IsNullOrEmpty(title))
            return false;

        if (title == "Program Manager" || title == "Windows Input Experience" || title == "Settings"
            || title == "Windows Shell Experience Host" || title == "OGS.Avalonia")
            return false;

        return true;
    }

    private readonly HashSet<uint> _tempGamePids = new();

    private void UpdateGames()
    {
        foreach (var item in _windowCaptureItems)
        {
            uint pid = item.Handle.GetProcessId();

            if (_gameCaptureItems.All(x => x.Pid != pid))
            {
                string name = Process.GetProcessById((int)pid).ProcessName;

                try
                {
                    _gameCaptureItems.Add(new WinGameInfo(item.Handle.GetProcessId(), name + " (" + pid + ")"));
                }
                catch (Exception)
                {
                    //Ignore
                }
            }

            _tempGamePids.Add(pid);
        }

        _gameCaptureItems.RemoveWhere(x => !_tempGamePids.Contains(x.Pid));

        _tempGamePids.Clear();
    }

    [RelayCommand]
    public async Task Apply()
    {
        _configService.Update(x => x.Bitrate = BitrateValue.FromMegaBits(BitrateMbps));
        
        if (SelectedCaptureItem is null)
        {
            await _dialogManager.ShowMessageBoxAsync(MessageBoxManager.GetMessageBoxStandard("Failed to capture", "No capture item was selected",
                ButtonEnum.Ok,
                Icon.Error));

            return;
        }

        try
        {
            if (SelectedCaptureItem is WinDisplayInfo display)
            {
                _platform.CaptureDisplay(display.Handle);
            }
            else if (SelectedCaptureItem is WinWindowInfo window)
            {
                _platform.CaptureWindow(window.Handle);
            }
            else if (SelectedCaptureItem is WinGameInfo game)
            {
                _platform.CaptureGame(game.Pid);
            }
        }
        catch (Exception ex)
        {
            await _dialogManager.ShowMessageBoxAsync(MessageBoxManager.GetMessageBoxStandard("Failed to capture", ex.ToString(), ButtonEnum.Ok,
                Icon.Error));
        }
    }

    [RelayCommand]
    public Task SetDisplayCapture()
    {
        CurrentCaptureItems = _displayCaptureItems;

        if (SelectedCaptureItem is not WinDisplayInfo)
            SelectedCaptureItem = _displayCaptureItems.FirstOrDefault();

        return Task.CompletedTask;
    }

    [RelayCommand]
    public Task SetWindowCapture()
    {
        CurrentCaptureItems = _windowCaptureItems;

        if (SelectedCaptureItem is not WinWindowInfo)
            SelectedCaptureItem = _windowCaptureItems.FirstOrDefault();

        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task SetGameCapture()
    {
        CurrentCaptureItems = _gameCaptureItems;

        if (SelectedCaptureItem is not WinGameInfo)
            SelectedCaptureItem = _gameCaptureItems.FirstOrDefault();

        if (_configService.Get(x => x.WindowsConfig.ShowGameHookWarning))
        {
            await _dialogManager.ShowMessageBoxAsync(MessageBoxManager.GetMessageBoxStandard("Game Hook Warning",
                "The capture method will inject a DLL into the game process to directly capture the game output.\n\n" +
                "!! THIS IS EXTREMELY EXPERIMENTAL AND ONLY SUPPORTS DIRECTX11 GAMES !!\n" +
                "!! I HAVE NO IDEA HOW ANTI-CHEATS WILL REACT TO THIS, USE AT YOUR OWN RISK !!\n" +
                "!! YOU SHOULD ASSUME THAT THIS WILL CRASH YOUR GAME AT SOME POINT !!\n\n" +
                "This message will not be shown again",
                ButtonEnum.Ok, Icon.Stop));

            _configService.Update(x => x.WindowsConfig.ShowGameHookWarning = false);
        }
    }

    #region Designer

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public WinQuickSwitchVm()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        _displayCaptureItems.Add(new WinDisplayInfo(default, "Display1"));
        _displayCaptureItems.Add(new WinDisplayInfo(default, "Display2"));
        _displayCaptureItems.Add(new WinDisplayInfo(default, "Display3"));

        _windowCaptureItems.Add(new WinWindowInfo(default, "Window1"));
        _windowCaptureItems.Add(new WinWindowInfo(default, "Window2"));
        _windowCaptureItems.Add(new WinWindowInfo(default, "Window3"));

        _gameCaptureItems.Add(new WinGameInfo(0, "Game1"));
        _gameCaptureItems.Add(new WinGameInfo(0, "Game2"));
        _gameCaptureItems.Add(new WinGameInfo(0, "Game3"));

        CurrentCaptureItems = _displayCaptureItems;
        SelectedCaptureItem = _displayCaptureItems[0];
    }

    #endregion
}