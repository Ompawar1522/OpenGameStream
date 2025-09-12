namespace OGS.Core.Config.Data.Windows;

public sealed class WindowsConfig
{
    public bool UseAsyncVideoProcessor { get; set; } = true;
    public WinDisplayCaptureMethod DisplayCaptureMethod { get; set; } = WinDisplayCaptureMethod.Wgc;
    public bool ShowGameHookConsole { get; set; } = false;
    public bool ShowGameHookWarning { get; set; } = true;
}