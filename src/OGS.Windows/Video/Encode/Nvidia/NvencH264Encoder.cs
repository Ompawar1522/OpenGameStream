using FFmpeg.AutoGen;
using OGS.Core.Common;
using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;

using ID3D11Device = TerraFX.Interop.DirectX.ID3D11Device;
using ID3D11DeviceContext = TerraFX.Interop.DirectX.ID3D11DeviceContext;
using ID3D11Texture2D = TerraFX.Interop.DirectX.ID3D11Texture2D;

namespace OGS.Windows.Video.Encode.Nvidia;

public sealed unsafe class NvencH264Encoder : ID3DEncoder
{
    private readonly NvencH264EncoderOptions _options;
    private static readonly Log Log = LogManager.GetLogger<NvencH264Encoder>();

    private readonly State* _state = (State*)NativeMemory.AllocZeroed((nuint)sizeof(State));
    private bool _initialized;

    private int _width;
    private int _height;

    private readonly GCHandle _freeBufferHandle;
    private readonly AvBufferFreeDelegate _freeBufferCallback;
    private delegate void AvBufferFreeDelegate(void* a, ID3D11Texture2D* b);
    private int _textureRefCount = 0;


    public NvencH264Encoder(NvencH264EncoderOptions options)
    {
        _options = options;

        _freeBufferCallback = ReleaseTextureCallback;
        _freeBufferHandle = GCHandle.Alloc(_freeBufferCallback);
    }

    public uint Encode(D3DEncodeArgs args)
    {
        if (!_initialized)
            Initialize(args.Texture);

        AVPacket* packet = null;
        AVFrame* frame = null;

        try
        {
            frame = CreateFrameFromTexture(args.Texture);

            if (args.Keyframe)
            {
                frame->flags |= ffmpeg.AV_FRAME_FLAG_KEY;
                frame->pict_type = AVPictureType.AV_PICTURE_TYPE_I;
                Log.Info("Creating key frame");
            }
            else
            {
                frame->flags &= ~ffmpeg.AV_FRAME_FLAG_KEY;
                frame->pict_type = AVPictureType.AV_PICTURE_TYPE_NONE;
            }
            
            packet = ffmpeg.av_packet_alloc();

            int ret = ffmpeg.avcodec_send_frame(_state->Context, frame).FFmpegThrowIfError();
            int bytesWritten = 0;

            if (ret >= 0)
            {
                while ((ret = ffmpeg.avcodec_receive_packet(_state->Context, packet)) >= 0)
                {
                    var packetData = new ReadOnlySpan<byte>(packet->data, packet->size);
                    packetData.CopyTo(args.Buffer.Span[bytesWritten..]);
                    bytesWritten += packet->size;
                    ffmpeg.av_packet_unref(packet);
                }
            }

            return (uint)bytesWritten;
        }
        finally
        {
            if (packet is not null)
                ffmpeg.av_packet_free(&packet);

            if (frame is not null)
                ffmpeg.av_frame_free(&frame);
        }
    }

    private void ReleaseTextureCallback(void* a, ID3D11Texture2D* texture)
    {
        var cpy = texture;
        ReleaseAndNull(ref cpy);
    }

    private AVFrame* CreateFrameFromTexture(ID3D11Texture2D* texture)
    {
        var frame = ffmpeg.av_frame_alloc();
        if (frame == null)
            throw new OutOfMemoryException("Failed to allocate AVFrame");

        try
        {
            texture->AddRef();

            var buffer = ffmpeg.av_buffer_create(
                (byte*)texture,
                (ulong)sizeof(nint),
                new av_buffer_create_free_func { Pointer = Marshal.GetFunctionPointerForDelegate(_freeBufferCallback) },
                null,
                0
            );

            if (buffer == null)
            {
                texture->Release();
                throw new OutOfMemoryException("Failed to create AVBuffer");
            }

            frame->buf[0] = buffer;
            frame->data[0] = (byte*)texture;
            frame->linesize[0] = 0;

            frame->hw_frames_ctx = ffmpeg.av_buffer_ref(_state->Context->hw_frames_ctx);

            return frame;
        }
        catch
        {
            ffmpeg.av_frame_free(&frame);
            throw;
        }
    }

    private void Initialize(ID3D11Texture2D* texture)
    {
        ID3D11Device* device;
        AVDictionary* opts = null;

        texture->GetDevice(&device);

        try
        {
            _state->Codec = ffmpeg.avcodec_find_encoder_by_name("h264_nvenc");
            D3D11_TEXTURE2D_DESC desc;
            texture->GetDesc(&desc);
            _width = (int)desc.Width;
            _height = (int)desc.Height;

            _state->CaptureDevice = device;
            device->AddRef();

            device->GetImmediateContext(&_state->CaptureDeviceContext);

            _state->Context = ffmpeg.avcodec_alloc_context3(_state->Codec);
            _state->Context->width = (int)desc.Width;
            _state->Context->height = (int)desc.Height;
            _state->Context->pix_fmt = AVPixelFormat.AV_PIX_FMT_D3D11;
            _state->Context->sw_pix_fmt = AVPixelFormat.AV_PIX_FMT_BGRA;
            _state->Context->time_base = new AVRational { num = 1, den = 1000 };
            _state->Context->framerate = new AVRational { num = (int)_options.Framerate, den = 1 };
            _state->Context->bit_rate = _options.Bitrate.BitsPerSecond;

            _state->Context->rc_max_rate = _options.Bitrate.BitsPerSecond;
            _state->Context->rc_buffer_size = (int)(_options.Bitrate.BitsPerSecond / 2);
            _state->Context->rc_initial_buffer_occupancy = _state->Context->rc_buffer_size * 3 / 4;

            //When using nvenc via FFMpeg, gop size 0 doesn't mean infinite, it means
            //every frame is a keyframe (?). So we set it to a very high value instead.
            _state->Context->gop_size = 100000;
            _state->Context->max_b_frames = 0;
            _state->Context->thread_count = 1;
            _state->Context->thread_type = 0;

            //Todo - do we need to set this in the WebRtc sdp?
            _state->Context->profile = ffmpeg.FF_PROFILE_H264_HIGH;
            _state->Context->level = 42;

            _state->Context->hw_device_ctx = ffmpeg.av_hwdevice_ctx_alloc(AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA);
            var hwDeviceCtx = (AVHWDeviceContext*)_state->Context->hw_device_ctx->data;
            var d3d11Ctx = (AVD3D11VADeviceContext*)hwDeviceCtx->hwctx;

            d3d11Ctx->device = (FFmpeg.AutoGen.ID3D11Device*)_state->CaptureDevice;
            d3d11Ctx->device_context = (FFmpeg.AutoGen.ID3D11DeviceContext*)_state->CaptureDeviceContext;

            ffmpeg.av_hwdevice_ctx_init(_state->Context->hw_device_ctx).FFmpegThrowIfError();

            AVBufferRef* hwFramesRef = ffmpeg.av_hwframe_ctx_alloc(_state->Context->hw_device_ctx);
            var framesCtx = (AVHWFramesContext*)hwFramesRef->data;

            framesCtx->format = AVPixelFormat.AV_PIX_FMT_D3D11;
            framesCtx->sw_format = AVPixelFormat.AV_PIX_FMT_BGRA;
            framesCtx->width = (int)desc.Width;
            framesCtx->height = (int)desc.Height;
            framesCtx->initial_pool_size = 1;

            ffmpeg.av_hwframe_ctx_init(hwFramesRef).FFmpegThrowIfError();

            _state->Context->hw_frames_ctx = ffmpeg.av_buffer_ref(hwFramesRef);
            ffmpeg.av_buffer_unref(&hwFramesRef);

            ffmpeg.av_dict_set(&opts, "rc", "cbr", 0);
            ffmpeg.av_dict_set(&opts, "cbr", "1", 0); 
            ffmpeg.av_dict_set(&opts, "maxrate", _options.Bitrate.BitsPerSecond.ToString(), 0);
            ffmpeg.av_dict_set(&opts, "bufsize", (_options.Bitrate.BitsPerSecond / 2).ToString(), 0);
            ffmpeg.av_dict_set(&opts, "rc-lookahead", "0", 0);  
            ffmpeg.av_dict_set(&opts, "aq-mode", "0", 0); 
            ffmpeg.av_dict_set(&opts, "spatial_aq", "0", 0);
            ffmpeg.av_dict_set(&opts, "temporal_aq", "0", 0);
            ffmpeg.av_dict_set(&opts, "multipass", "disabled", 0); 
            ffmpeg.av_dict_set(&opts, "weighted_pred", "0", 0);
            ffmpeg.av_dict_set(&opts, "b_ref_mode", "disabled", 0);
            ffmpeg.av_dict_set(&opts, "forced-idr", "1", 0);
            ffmpeg.av_dict_set(&opts, "strict_gop", "1", 0);

            ffmpeg.av_dict_set(&opts, "preset", "p1", 0);
            ffmpeg.av_dict_set(&opts, "tune", "ull", 0);
            ffmpeg.av_dict_set(&opts, "delay", "0", 0);
            ffmpeg.av_dict_set(&opts, "zerolatency", "1", 0);

            ffmpeg.avcodec_open2(_state->Context, _state->Codec, &opts).FFmpegThrowIfError();


            Log.Info($"Initialized NVENC H.264 encoder: {desc.Width}x{desc.Height}");
            _initialized = true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to initialize NVENC H.264 encoder: {ex.Message}");
            Dispose();
            throw;
        }
        finally
        {
            ReleaseAndNull(ref device);

            if (opts is not null)
                ffmpeg.av_dict_free(&opts);
        }
    }

    public void Dispose()
    {
        if(_state->Context is not null)
        {
            if (_state->Context->hw_frames_ctx != null)
                ffmpeg.av_buffer_unref(&_state->Context->hw_frames_ctx);

            if (_state->Context->hw_device_ctx != null)
                ffmpeg.av_buffer_unref(&_state->Context->hw_device_ctx);
               
            ffmpeg.avcodec_free_context(&_state->Context);
        }
        NativeMemory.Free(_state);
        _freeBufferHandle.Free();

        Log.Info("Disposed NVENC encoder");
    }

    private struct State
    {
        public AVCodec* Codec;
        public AVCodecContext* Context;

        public ID3D11Device* CaptureDevice;
        public ID3D11DeviceContext* CaptureDeviceContext;
    }
}
