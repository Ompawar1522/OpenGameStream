using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OGS.Core.Common;
using OGS.Core.Config;
using OGS.Core.Config.Data;
using OGS.Core.Config.Data.Windows;

namespace OGS.ViewModels.Dialogs;

public sealed partial class CaptureSettingsWindowVm : ObservableObject
{
    [ObservableProperty] private int _selectedFrameRate;
    [ObservableProperty] private int _bitrateMbit;
    [ObservableProperty] private bool _enableCursor;
    [ObservableProperty] private bool _enablePreview;
    
    public bool EnableAudioProcessNameEntry => DisplayCaptureAudioMode != DisplayCaptureAudioMode.CaptureAll;
    [ObservableProperty] private string? _audioProcessName;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(EnableAudioProcessNameEntry))] private DisplayCaptureAudioMode _displayCaptureAudioMode;
    public int[] SelectableFrameRates { get; } = [60, 120, 144, 180, 240];

    //Windows settings

    [ObservableProperty] private WinDisplayCaptureMethod _winDisplayCaptureMethod = WinDisplayCaptureMethod.Wgc;
    public WinDisplayCaptureMethod[] WinDisplayCaptureMethods { get; } = [WinDisplayCaptureMethod.Wgc, WinDisplayCaptureMethod.Dxgi];
    [ObservableProperty] private bool _winUseAsyncProcessor;
    [ObservableProperty] private bool _winGameHookConsole;

    private readonly IConfigService _configService;
    private Window _windowInstance = null!;

    public CaptureSettingsWindowVm(IConfigService configService)
    {
        _configService = configService;
        UpdateFromConfig();
    }

    private void UpdateFromConfig()
    {
        SelectedFrameRate = (int)_configService.Get(x => x.FramerateLimit);
        BitrateMbit = (int)_configService.Get(x => x.Bitrate.MegaBitsPerSecond);
        AudioProcessName = _configService.Get(x => x.AudioCaptureProcessName);
        DisplayCaptureAudioMode = _configService.Get(x => x.DisplayCaptureAudioMode);
        EnablePreview = _configService.Get(x => x.EnableVideoPreview);
        EnableCursor = _configService.Get(x => x.IncludeCursor);
        
        WinDisplayCaptureMethod = _configService.Get(x => x.WindowsConfig.DisplayCaptureMethod);
        WinUseAsyncProcessor = _configService.Get(x => x.WindowsConfig.UseAsyncVideoProcessor);
        WinGameHookConsole = _configService.Get(x => x.WindowsConfig.ShowGameHookConsole);
    }

    [RelayCommand]
    public Task Save()
    {
        _configService.Update(x =>
        {
            x.FramerateLimit = (uint)SelectedFrameRate;
            x.Bitrate = BitrateValue.FromMegaBits(BitrateMbit);
            x.DisplayCaptureAudioMode =  DisplayCaptureAudioMode;
            x.AudioCaptureProcessName = AudioProcessName;
            x.IncludeCursor = EnableCursor;
            x.EnableVideoPreview = EnablePreview;

            x.WindowsConfig.DisplayCaptureMethod = WinDisplayCaptureMethod;
            x.WindowsConfig.UseAsyncVideoProcessor = WinUseAsyncProcessor;
            x.WindowsConfig.ShowGameHookConsole = WinGameHookConsole;
        });

        _windowInstance!.Close();
        
        return Task.CompletedTask;
    }

    
    public void OnWindowLoad(Window instance)
    {
        _windowInstance = instance;
    }
    
    #region Designer
    public CaptureSettingsWindowVm()
    {
        _configService = new FakeConfigService();
        UpdateFromConfig();
    }
    #endregion Designer
}