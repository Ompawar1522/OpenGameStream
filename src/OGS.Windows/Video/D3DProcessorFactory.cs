using OGS.Core.Common.Video;
using OGS.Core.Config;
using OGS.Windows.Common;
using OGS.Windows.Video.Encode;
using OGS.Windows.Video.Encode.Intel;
using OGS.Windows.Video.Encode.Nvidia;
using OGS.Windows.Video.Encode.Software;
using OGS.Windows.Video.Processing;
using TerraFX.Interop.Windows;

namespace OGS.Windows.Video;

public sealed class D3DProcessorFactory
{
    private static readonly Log Log = LogManager.GetLogger<D3DProcessorFactory>();

    private readonly IConfigService _configService;

    public D3DProcessorFactory(IConfigService configService)
    {
        _configService = configService;
    }

    public D3DProcessorFactoryDelegate CreateProcessorFactory(KeyFrameSelectorDelegate keyFrameSelectorDelegate,
        Action<EncodedVideoFrame> callback,
        HWND previewWindowHandle)
    {
        if (_configService.Get(x => x.WindowsConfig.UseAsyncVideoProcessor))
            return CreateAsyncProcessorFactory(keyFrameSelectorDelegate, callback, previewWindowHandle);
        else
            return CreateBasicProcessorFactory(keyFrameSelectorDelegate, callback, previewWindowHandle);
    }

    public D3DProcessorFactoryDelegate CreateBasicProcessorFactory(
        KeyFrameSelectorDelegate keyFrameSelector,
        Action<EncodedVideoFrame> callback,
        HWND previewWindowHandle)
    {
        return state =>
        {
            return new BasicD3DProcessor(new BasicD3DProcessorOptions
            {
                EncoderFactory = GetEncoderFactory(),
                KeyFrameSelector = keyFrameSelector,
                CaptureState = state,
                OnEncodedCallback = callback,
                PreviewWindowHandle = _configService.Get(x => x.EnableVideoPreview)
                ? previewWindowHandle
                : HWND.NULL,
            });
        };
    }

    public D3DProcessorFactoryDelegate CreateAsyncProcessorFactory(
      KeyFrameSelectorDelegate keyFrameSelector,
      Action<EncodedVideoFrame> callback,
      HWND previewWindowHandle)
    {
        return state =>
        {
            return new AsyncD3DProcessor(new AsyncD3DProcessorOptions
            {
                EncoderFactory = GetEncoderFactory(),
                KeyFrameSelector = keyFrameSelector,
                CaptureState = state,
                OnEncodedCallback = callback,
                PreviewWindowHandle = _configService.Get(x => x.EnableVideoPreview)
                ? previewWindowHandle
                : HWND.NULL,
            });
        };
    }

    private unsafe D3DEncoderFactoryDelegate GetEncoderFactory()
    {
        return device =>
        {
            string? gpuName = D3DHelpers.GetGpuDeviceName(device);

            if (gpuName is null)
            {
                Log.Error("Failed to get GPU name. Falling back to software encoder");
                return CreateSoftwareEncoder();
            }

            if (_configService.Get(x => x.ForceSoftwareEncoder))
            {
                Log.Warn("Forcing software encoder");
                return CreateSoftwareEncoder();
            }

            Log.Info($"Using GPU '{gpuName}'");

            if (gpuName.Contains("nvidia", StringComparison.OrdinalIgnoreCase))
            {
                Log.Info("Selected Nvidia Nvenc h264 encoder");
                return CreateNvencEncoder();
            }
            else if (gpuName.Contains("intel", StringComparison.OrdinalIgnoreCase))
            {
                Log.Info("Selected Intel QSV H264 encoder");
                return CreateQsvEncoder();
            }

            Log.Error($"Unknown GPU '{gpuName}'. Falling back to software encoder");
            return CreateSoftwareEncoder();
        };
    }

    private ID3DEncoder CreateSoftwareEncoder()
    {
        return new SoftwareH264Encoder(new SoftwareH264EncoderOptions
        {
            Bitrate = _configService.Get(x => x.Bitrate),
            Framerate = _configService.Get(x => x.FramerateLimit)
        });
    }

    private ID3DEncoder CreateNvencEncoder()
    {
        return new NvencH264Encoder(new NvencH264EncoderOptions
        {
            Bitrate = _configService.Get(x => x.Bitrate),
            Framerate = _configService.Get(x => x.FramerateLimit)
        });
    }

    private ID3DEncoder CreateQsvEncoder()
    {
        return new QsvH264Encoder(new QsvH264EncoderOptions
        {
            Bitrate = _configService.Get(x => x.Bitrate),
            Framerate = _configService.Get(x => x.FramerateLimit)
        });
    }
}
