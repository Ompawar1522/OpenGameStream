using FFmpeg.AutoGen;
using OGS.Core.Common;
using OGS.Windows.Common;
using OGS.Windows.Video.Encode.Convert;
using System.Diagnostics;
using TerraFX.Interop.DirectX;
using static FFmpeg.AutoGen.ffmpeg;
using ID3D11Device = TerraFX.Interop.DirectX.ID3D11Device;
using ID3D11DeviceContext = TerraFX.Interop.DirectX.ID3D11DeviceContext;
using ID3D11Texture2D = TerraFX.Interop.DirectX.ID3D11Texture2D;

namespace OGS.Windows.Video.Encode.Intel;

public sealed unsafe class QsvH264Encoder : ID3DEncoder
{
    private static readonly Log Log = LogManager.GetLogger<QsvH264Encoder>();

    private readonly QsvH264EncoderOptions _options;
    private readonly State* _state = MemoryHelper.AllocZeroed<State>();

    private IBgraToNv12Converter? _bgraToNv12;

    public QsvH264Encoder(QsvH264EncoderOptions options)
    {
        _options = options;
    }

    private void Initialize(D3DEncodeArgs args)
    {
        args.Texture->GetDevice(&_state->Device);
        _state->Device->GetImmediateContext(&_state->DeviceContext);
        D3DHelpers.EnableDeviceThreadProtection(_state->Device);

        _bgraToNv12 = new BgraToNv12Converter(_state->Device, _state->DeviceContext);

        D3D11_TEXTURE2D_DESC desc;
        args.Texture->GetDesc(&desc);

        _state->Codec = avcodec_find_encoder_by_name("h264_qsv");
        _state->CodecContext = avcodec_alloc_context3(_state->Codec);
        _state->CodecContext->bit_rate = _options.Bitrate.BitsPerSecond;
        _state->CodecContext->width = (int)desc.Width;
        _state->CodecContext->height = (int)desc.Height;
        _state->CodecContext->pix_fmt = AVPixelFormat.AV_PIX_FMT_QSV;
        _state->CodecContext->sw_pix_fmt = AVPixelFormat.AV_PIX_FMT_NV12;
        _state->CodecContext->framerate = av_make_q((int)_options.Framerate, 1);
        _state->CodecContext->time_base = av_make_q(1, 1000);
        _state->HwDeviceCtx = av_hwdevice_ctx_alloc(AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA);
        _state->CodecContext->gop_size = 1000000;
        _state->CodecContext->max_b_frames = 0;
        _state->CodecContext->slices = 16;
        _state->CodecContext->thread_count = 4;

        AVHWDeviceContext* deviceContext =
(AVHWDeviceContext*)_state->HwDeviceCtx->data;
        AVD3D11VADeviceContext* d3d11VaDeviceContext =
            (AVD3D11VADeviceContext*)deviceContext->hwctx;
        d3d11VaDeviceContext->device = (FFmpeg.AutoGen.ID3D11Device*)_state->Device;
        d3d11VaDeviceContext->device_context = (FFmpeg.AutoGen.ID3D11DeviceContext*)_state->DeviceContext;

        av_hwdevice_ctx_init(_state->HwDeviceCtx).FFmpegThrowIfError();

        av_hwdevice_ctx_create_derived(&_state->DerivedContext, AVHWDeviceType.AV_HWDEVICE_TYPE_QSV,
            _state->HwDeviceCtx, 0).FFmpegThrowIfError();

        av_buffer_unref(&_state->HwDeviceCtx);
        _state->HwDeviceCtx = _state->DerivedContext;

        _state->HwDeviceCtx = av_buffer_ref(_state->HwDeviceCtx);
        SetHwContext(desc.Width, desc.Height);

        AVDictionary* d;

        av_opt_set(_state->CodecContext->priv_data, "preset", "veryfast", 0).FFmpegThrowIfError();
        av_opt_set(_state->CodecContext->priv_data, "look_ahead", "0", 0).FFmpegThrowIfError();
        av_opt_set(_state->CodecContext->priv_data, "async_depth", "1", 0).FFmpegThrowIfError();
        av_opt_set(_state->CodecContext->priv_data, "profile", "main", 0).FFmpegThrowIfError();
        av_opt_set(_state->CodecContext->priv_data, "forced_idr", "1", 0).FFmpegThrowIfError();

        av_opt_set(_state->CodecContext->priv_data, "low_delay", "1", 0);
        av_opt_set(_state->CodecContext->priv_data, "low_power", "1", 0);
        av_opt_set(_state->CodecContext->priv_data, "rdo", "0", 0);

        avcodec_open2(_state->CodecContext, _state->Codec, &d).FFmpegThrowIfError();

        _state->Frame = av_frame_alloc();

        _state->Frame->format = (int)_state->CodecContext->pix_fmt;
        _state->Frame->width = (int)desc.Width;
        _state->Frame->height = (int)desc.Height;

        av_hwframe_get_buffer(_state->CodecContext->hw_frames_ctx, _state->Frame, 0).FFmpegThrowIfError();

        _state->MappedFrame = av_frame_alloc();
        _state->MappedFrame->format = (int)AVPixelFormat.AV_PIX_FMT_D3D11;

        av_hwframe_map(_state->MappedFrame, _state->Frame, 1 | 1 << 2).FFmpegThrowIfError();
        _state->EncodeTexture = (ID3D11Texture2D*)_state->MappedFrame->data[0];

        Log.Info("Created QSV encoder");
    }
    private void SetHwContext(uint width, uint height)
    {
        AVHWFramesContext* framesCtx;

        var hwFramesRef = av_hwframe_ctx_alloc(_state->HwDeviceCtx);

        framesCtx = (AVHWFramesContext*)hwFramesRef->data;
        framesCtx->format = AVPixelFormat.AV_PIX_FMT_QSV;
        framesCtx->sw_format = AVPixelFormat.AV_PIX_FMT_NV12;
        framesCtx->width = (int)width;
        framesCtx->height = (int)height;
        framesCtx->initial_pool_size = 1;
        AVD3D11VAFramesContext* framesHwctx =
            (AVD3D11VAFramesContext*)framesCtx->hwctx;
        framesHwctx->MiscFlags = 0;

        av_hwframe_ctx_init(hwFramesRef).FFmpegThrowIfError();

        _state->CodecContext->hw_frames_ctx = av_buffer_ref(hwFramesRef);
        av_buffer_unref(&hwFramesRef);
    }

    private long _startTime;
    private long _previousTime;

    public uint Encode(D3DEncodeArgs args)
    {
        if (_state->CodecContext is null)
        {
            Initialize(args);
            _startTime = Stopwatch.GetTimestamp();
            _previousTime = _startTime;
        }

        _state->Frame->pict_type = args.Keyframe ? AVPictureType.AV_PICTURE_TYPE_I : AVPictureType.AV_PICTURE_TYPE_P;

        _bgraToNv12?.Convert(args.Texture, _state->EncodeTexture);

        _state->MappedFrame->pts = (long)Stopwatch.GetElapsedTime(_startTime).TotalMilliseconds;
        _state->MappedFrame->duration = (long)Stopwatch.GetElapsedTime(_previousTime).TotalMilliseconds;
        _state->Frame->pts = (long)Stopwatch.GetElapsedTime(_startTime).TotalMilliseconds;
        _state->Frame->duration = (long)Stopwatch.GetElapsedTime(_previousTime).TotalMilliseconds;

        avcodec_send_frame(_state->CodecContext, _state->Frame).FFmpegThrowIfError();
        AVPacket* packet = av_packet_alloc();

        int size = 0;
        while (true)
        {
            var hr = avcodec_receive_packet(_state->CodecContext, packet);


            if (hr == 0)
            {
                var data = new Span<byte>(packet->data, packet->size);
                data.CopyTo(args.Buffer.Span.Slice(size));

                size += packet->size;
            }
            else
            {

                break;
            }
        }

        _previousTime = Stopwatch.GetTimestamp();
        av_packet_free(&packet);

        return (uint)size;
    }

    public void Dispose()
    {
        _bgraToNv12?.Dispose();

        if (_state->CodecContext is not null)
        {
            av_buffer_unref(&_state->DerivedContext);
            av_buffer_unref(&_state->HwDeviceCtx);

            avcodec_free_context(&_state->CodecContext);
            av_frame_free(&_state->Frame);
            av_frame_free(&_state->MappedFrame);
        }
    }

    private struct State
    {
        public ID3D11Device* Device;
        public ID3D11DeviceContext* DeviceContext;
        public AVCodec* Codec;
        public AVCodecContext* CodecContext;
        public AVBufferRef* HwDeviceCtx;
        public AVBufferRef* DerivedContext;
        public AVFrame* Frame;
        public AVFrame* MappedFrame;
        public ID3D11Texture2D* EncodeTexture;
    }
}
