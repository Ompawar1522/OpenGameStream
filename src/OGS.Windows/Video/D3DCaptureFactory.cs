using OGS.Core.Config;
using OGS.Core.Config.Data.Windows;
using OGS.Windows.Video.Capture.Dxgi;
using OGS.Windows.Video.Capture.Hook;
using OGS.Windows.Video.Capture.Wgc;
using OGS.Windows.Video.Processing;
using TerraFX.Interop.Windows;

namespace OGS.Windows.Video;

public sealed class D3DCaptureFactory
{
    private readonly IConfigService _configService;

    public D3DCaptureFactory(IConfigService configService)
    {
        _configService = configService;
    }

    public IDisposable CreateDisplayCapture(HMONITOR display,
        D3DProcessorFactoryDelegate processorFactory)
    {
        if(_configService.Get(x => x.WindowsConfig.DisplayCaptureMethod) == WinDisplayCaptureMethod.Wgc)
        {
            return new WgcVideoCapture(new WgcVideoCaptureOptions()
            {
                ProcessorFactory = processorFactory,
                EnableBorder = false,
                EnableMouse = _configService.Get(x => x.IncludeCursor),
                FrameRate = (int)_configService.Get(x => x.FramerateLimit),
                Monitor = display,
            });
        }
        else
        {
            return new DxgiVideoCapture(new DxgiVideoCaptureOptions
            {
                Display = display,
                ProcessorFactory = processorFactory,
                FramerateLimit = _configService.Get(x => x.FramerateLimit)
            });
        }
    }

    public IDisposable CreateWindowCapture(HWND hWnd,
        D3DProcessorFactoryDelegate processorFactory)
    {
        return new WgcVideoCapture(new WgcVideoCaptureOptions()
        {
            ProcessorFactory = processorFactory,
            EnableBorder = false,
            EnableMouse = _configService.Get(x => x.IncludeCursor),
            FrameRate = (int)_configService.Get(x => x.FramerateLimit),
            Window = hWnd,
        });
    }

    public IDisposable CreateGameCapture(uint processId,
        D3DProcessorFactoryDelegate processorFactory)
    {
        return new HookVideoCapture(new HookVideoCaptureOptions
        {
            FrameRate = _configService.Get(x => x.FramerateLimit),
            ProcessId = processId,
            ProcessorFactory = processorFactory,
            LogLevel = LogLevel.Trace,
            ShowConsole = _configService.Get(x => x.WindowsConfig.ShowGameHookConsole)
        });
    }
}
