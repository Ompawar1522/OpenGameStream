using OGS.Core.Common;
using OGS.Core.Common.Audio;
using OGS.Core.Config;
using OGS.Core.Config.Data;
using System.Diagnostics;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace OGS.Windows.Audio;

public sealed class WinAudioCaptureFactory
{
    private static readonly Log Log = LogManager.GetLogger<WinAudioCaptureFactory>();

    private readonly IConfigService _configService;

    public WinAudioCaptureFactory(IConfigService configService)
    {
        _configService = configService;
    }

    public IDisposable CreateAudioCaptureForDisplayCapture(Action<EncodedAudioSample> callback)
    {
        DisplayCaptureAudioMode audioMode = _configService.Get(x => x.DisplayCaptureAudioMode);

        if (audioMode == DisplayCaptureAudioMode.CaptureAll)
        {
            return new WasapiAudioCapture(new WasapiAudioCaptureOptions()
            {
                EncoderFactory = (state) => CreateAudioEncoder(state, callback),
                Mode = DisplayCaptureAudioMode.CaptureAll
            });
        }
        else
        {
            string? audioProcessName = _configService.Get(x => x.AudioCaptureProcessName);

            if (audioProcessName is null)
                throw new InvalidOperationException($"Audio mode is {audioMode} but no process name was specified");

            if (!ProcessHelper.TryFindOldestProcess(audioProcessName, out Process? oldestProcess))
            {
                Log.Warn($"Failed to find audio target process '{audioProcessName}' - capturing all audio instead");

                return new WasapiAudioCapture(new WasapiAudioCaptureOptions()
                {
                    EncoderFactory = (state) => CreateAudioEncoder(state, callback),
                    Mode = DisplayCaptureAudioMode.CaptureAll
                });
            }

            if (audioMode == DisplayCaptureAudioMode.ExcludeProcess)
                Log.Info($"Excluding process '{audioProcessName}' (PID {oldestProcess.Id}) from audio capture");
            else if (audioMode == DisplayCaptureAudioMode.IncludeProcess)
                Log.Info($"Exclusively capturing audio from process '{audioProcessName}' (PID {oldestProcess.Id})");

            return new WasapiAudioCapture(new WasapiAudioCaptureOptions
            {
                EncoderFactory = (state) => CreateAudioEncoder(state, callback),
                Mode = audioMode,
                ProcessId = (uint)oldestProcess.Id
            });
        }
    }

    public unsafe IDisposable CreateAudioCaptureForWindow(HWND hWnd,
        Action<EncodedAudioSample> callback)
    {
        uint pid = 0;
        GetWindowThreadProcessId(hWnd, &pid);

        return new WasapiAudioCapture(new WasapiAudioCaptureOptions
        {
            EncoderFactory = (state) => CreateAudioEncoder(state, callback),
            Mode = DisplayCaptureAudioMode.IncludeProcess,
            ProcessId = pid
        });
    }

    public unsafe IDisposable CreateAudioCaptureForGame(uint pid,
        Action<EncodedAudioSample> callback)
    {
        return new WasapiAudioCapture(new WasapiAudioCaptureOptions
        {
            EncoderFactory = (state) => CreateAudioEncoder(state, callback),
            Mode = DisplayCaptureAudioMode.IncludeProcess,
            ProcessId = pid
        });
    }

    private IAudioEncoder CreateAudioEncoder(AudioCaptureState state, Action<EncodedAudioSample> callback)
    {
        return new OpusAudioEncoder(state, new AudioEncoderOptions
        {
            Bitrate = BitrateValue.FromKiloBits(500),
            Callback = callback
        });
    }
}
