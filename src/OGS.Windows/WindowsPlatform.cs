using Avalonia.Controls;
using MsBox.Avalonia;
using Nefarius.ViGEm.Client;
using OGS.Core.Common.Audio;
using OGS.Core.Common.Input;
using OGS.Core.Common.Video;
using OGS.Core.Config;
using OGS.Core.Platform;
using OGS.Core.Ui;
using OGS.Plaform.Windows.ViewModels;
using OGS.Plaform.Windows.Views;
using OGS.Windows.Audio;
using OGS.Windows.Common;
using OGS.Windows.Input;
using OGS.Windows.Ui;
using OGS.Windows.Video;
using OGS.Windows.Video.Processing;
using TerraFX.Interop.Windows;

namespace OGS.Windows;

public sealed class WindowsPlatform : IPlatform
{ 
    private static readonly Log Log = LogManager.GetLogger<WindowsPlatform>();

    public PlatformEvents Events { get; } = new();

    private readonly IConfigService _configService;
    private readonly D3DProcessorFactory _processorFactory;
    private readonly D3DCaptureFactory _captureFactory;
    private readonly WinAudioCaptureFactory _audioCaptureFactory;
    private readonly IDialogManager _dialogManager;
    private readonly Lock _lock = new Lock();

    private IDisposable? _videoCapture;
    private IDisposable? _audioCapture;
    private ViGEmClient? _vigemClient;

    private WinPreviewControl? _previewControl;
    
    //This is used to recreate the previous selected capture.
    private Action? _recreateCaptureDelegate;
    private bool _keyFrameFlag;
    private bool _disposed;

    public WindowsPlatform(IConfigService configService,
        D3DProcessorFactory processorFactory,
        D3DCaptureFactory captureFactory,
        WinAudioCaptureFactory audioCaptureFactory,
        IDialogManager dialogManager)
    {
        _configService = configService;
        _processorFactory = processorFactory;
        _captureFactory = captureFactory;
        _audioCaptureFactory = audioCaptureFactory;
        _dialogManager = dialogManager;
    }

    public void Initialize()
    {
        using (_lock.EnterScope())
        {
            try
            {
                _vigemClient = new ViGEmClient();
            }catch(Exception)
            {
                _ = _dialogManager.ShowMessageBoxAsync(MessageBoxManager.GetMessageBoxStandard("Error",
                    "The ViGEm driver was not found. Gamepad support will be disabled", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error));
            }
        }
    }

    public void CaptureDisplay(HMONITOR display)
    {
        using (_lock.EnterScope())
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            CloseCapture();
            _recreateCaptureDelegate = () => CaptureDisplay(display);

            //The D3D processor handles frames after they have been captured.
            //The capture thread creates the D3DProcessor, and destroys it
            //either when the texture size or format has changed, or if the capture is disposed
            D3DProcessorFactoryDelegate processorFactory = 
                _processorFactory.CreateProcessorFactory(KeyFrameSelector, OnVideoFrame, GetPreviewWindowHandle());

            try
            {
                _videoCapture = _captureFactory.CreateDisplayCapture(display, processorFactory);
                _audioCapture = _audioCaptureFactory.CreateAudioCaptureForDisplayCapture(OnAudioSample);
            }
            catch (Exception)
            {
                CloseCapture();
                throw;
            }
        }
    }

    public void CaptureWindow(HWND window)
    {
        using (_lock.EnterScope())
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(WindowsPlatform));
            CloseCapture();
            _recreateCaptureDelegate = () => CaptureWindow(window);

            D3DProcessorFactoryDelegate processorFactory =
                _processorFactory.CreateProcessorFactory(KeyFrameSelector, OnVideoFrame, GetPreviewWindowHandle());

            try
            {
                _videoCapture = _captureFactory.CreateWindowCapture(window, processorFactory);
                _audioCapture = _audioCaptureFactory.CreateAudioCaptureForWindow(window, OnAudioSample);
            }
            catch (Exception)
            {
                CloseCapture();
                throw;
            }
        }
    }

    public void CaptureGame(uint pid)
    {
        using (_lock.EnterScope())
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(WindowsPlatform));
            CloseCapture();
            _recreateCaptureDelegate = () => CaptureGame(pid);

            //No point using an async processor as the game capture texture is already opened
            //from a shared context
            D3DProcessorFactoryDelegate processorFactory =
                _processorFactory.CreateBasicProcessorFactory(KeyFrameSelector, OnVideoFrame, GetPreviewWindowHandle());

            try
            {
                _videoCapture = _captureFactory.CreateGameCapture(pid, processorFactory);
                _audioCapture = _audioCaptureFactory.CreateAudioCaptureForGame(pid, OnAudioSample);
            }
            catch (Exception)
            {
                CloseCapture();
                throw;
            }
        }
    }

    private void RecreateCapture()
    {
        if(_recreateCaptureDelegate is not null)
        {
            _recreateCaptureDelegate();
            return;
        }

        CaptureDisplay(Win32Helpers.GetPrimaryMonitor());
    }

    private HWND GetPreviewWindowHandle()
    {
        return _previewControl is not null ? _previewControl.Handle : HWND.NULL;
    }

    /// <summary>
    /// This delegate is called by the D3DProcessor to allow for key frames to be requested.
    /// </summary>
    /// <returns></returns>
    private bool KeyFrameSelector() => Interlocked.Exchange(ref _keyFrameFlag, false);

    private void CloseCapture()
    {
        _videoCapture?.Dispose();
        _audioCapture?.Dispose();
        _videoCapture = null;
        _audioCapture = null;
    }

    private void OnVideoFrame(EncodedVideoFrame frame)
    {
        Events.OnVideoFrame.Raise(frame);
    }

    private void OnAudioSample(EncodedAudioSample sample)
    {
        Events.OnAudioSample.Raise(sample);
    }
    
    public void RequestKeyFrame()
    {
        _keyFrameFlag = true;
    }

    public void MoveMouseRelative(short x, short y)
    {
        WinSendInput.SendMouseInput(x, y, 0, MOUSEEVENTF.MOUSEEVENTF_MOVE);
    }

    public void MoveMouseAbsolute(int x, int y)
    {
        WinSendInput.SendMouseInput(x, y, 0, MOUSEEVENTF.MOUSEEVENTF_MOVE | MOUSEEVENTF.MOUSEEVENTF_VIRTUALDESK);
    }

    public void SendMouseButton(MouseButton button, bool pressed)
    {
        WinSendInput.SendMouseButton(button, pressed);
    }

    public void SendKeyboardKey(int key, bool pressed)
    {
        WinSendInput.SendKeyboardKey(key, pressed);
    }

    public void SendMouseScroll(MouseScrollDirection direction)
    {
        WinSendInput.SendMouseScroll(direction);
    }

    public IGamepad CreateGamepad()
    {
        using (_lock.EnterScope())
        {
            if (_vigemClient is null)
                throw new InvalidOperationException("The ViGEm driver was not found");

            return new VigemGamepad(_vigemClient);
        }
    }

    public IPreciseTimer CreateTimer(TimeSpan interval)
    {
        return new WinPreciseTimer(interval);
    }

    public Control CreatePreviewControl()
    {
        using (_lock.EnterScope())
        {
            _previewControl = new WinPreviewControl((handle) =>
            {
                using (_lock.EnterScope())
                {
                    if(!_disposed && handle == _previewControl?.Handle)
                    {
                        RecreateCapture();
                    } 
                }
            });

            return _previewControl;
        }
    }

    public Control CreateQuickSwitchControl()
    {
        return new WinQuickSwitchControl()
        {
            DataContext = new WinQuickSwitchVm(this, _configService, _dialogManager)
        };
    }

    public void Dispose()
    {
        using (_lock.EnterScope())
        {
            if (_disposed)
                return;

            _disposed = true;
            CloseCapture();
            _vigemClient?.Dispose();
        }
    }
}
