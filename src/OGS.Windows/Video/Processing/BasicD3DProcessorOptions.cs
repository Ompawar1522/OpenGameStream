using OGS.Core.Common.Video;
using OGS.Windows.Video.Capture;
using OGS.Windows.Video.Encode;
using TerraFX.Interop.Windows;

namespace OGS.Windows.Video.Processing;

public sealed class BasicD3DProcessorOptions
{
    public required D3DEncoderFactoryDelegate EncoderFactory { get; init; }
    public required KeyFrameSelectorDelegate KeyFrameSelector { get; init; }
    public required Action<EncodedVideoFrame> OnEncodedCallback { get; init; }

    public required D3DCaptureState CaptureState { get; init; }

    
    public HWND PreviewWindowHandle { get; init; }
}