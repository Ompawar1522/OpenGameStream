using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using OGS.Core.Common;
using TerraFX.Interop.DirectX;
using ID3D11Device = TerraFX.Interop.DirectX.ID3D11Device;
using ID3D11DeviceContext = TerraFX.Interop.DirectX.ID3D11DeviceContext;
using ID3D11Texture2D = TerraFX.Interop.DirectX.ID3D11Texture2D;


namespace OGS.Windows.Video.Encode.Software;

public sealed unsafe class SoftwareH264Encoder : ID3DEncoder
{
    private static readonly Log Log = LogManager.GetLogger<SoftwareH264Encoder>();

    private readonly State* _state = MemoryHelper.AllocZeroed<State>();
    private readonly SoftwareH264EncoderOptions _options;
    
    private bool _isInitialized;
    private TimeSpan _lastTimestamp = TimeSpan.Zero;
    private bool _isFirstFrame = true;

    public SoftwareH264Encoder(SoftwareH264EncoderOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public uint Encode(D3DEncodeArgs args)
    {
        if (!_isInitialized)
        {
            Initialize(args.Texture);
            _isInitialized = true;
        }

        if (_state->StagingTexture is null)
            InitializeStagingTexture(args.Texture);

        _state->DeviceContext->CopyResource(
            (ID3D11Resource*)_state->StagingTexture,
            (ID3D11Resource*)args.Texture);

        _state->DeviceContext->Map(
            (ID3D11Resource*)_state->StagingTexture,
            0,
            D3D11_MAP.D3D11_MAP_READ,
            0,
            &_state->StagingResource).ThrowIfFailed();

        try
        {
            if (_state->SwsContext is null)
                InitializeSwsContext();

            ConvertAndScaleFrame();

            return EncodeFrame(args.Buffer, args.Keyframe, args.Timestamp);
        }
        finally
        {
            _state->DeviceContext->Unmap((ID3D11Resource*)_state->StagingTexture, 0);
        }
    }

    private void Initialize(ID3D11Texture2D* texture)
    {
        Log.Info("Initializing software H264 encoder");
        
        texture->GetDevice(&_state->Device);
        _state->Device->GetImmediateContext(&_state->DeviceContext);
        
        InitializeCodec(texture);
        InitializeFrames();

        Log.Info("Software H264 encoder initialized successfully");
    }
    private void InitializeCodec(ID3D11Texture2D* texture)
    {
        D3D11_TEXTURE2D_DESC textureDesc;
        texture->GetDesc(&textureDesc);

        _state->Codec = ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_H264);
        if (_state->Codec is null)
            throw new InvalidOperationException("H264 encoder not found");

        string codecName = Marshal.PtrToStringAnsi((nint)_state->Codec->name)!;
        Log.Info().Append("Using software H264 codec: ").Append(codecName).Log();

        _state->CodecContext = ffmpeg.avcodec_alloc_context3(_state->Codec);
        if (_state->CodecContext is null)
            throw new OutOfMemoryException("Failed to allocate codec context");

        ConfigureCodec(textureDesc);
        OpenCodec();
    }

    private void ConfigureCodec(D3D11_TEXTURE2D_DESC textureDesc)
    {
        var ctx = _state->CodecContext;
        
        ctx->width = (int)textureDesc.Width;
        ctx->height = (int)textureDesc.Height;
        ctx->time_base = new AVRational { den = 1000, num = 1 };
        ctx->framerate = new AVRational { num = (int)_options.Framerate, den = 1 };
        ctx->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
        ctx->profile = ffmpeg.FF_PROFILE_H264_MAIN;
        ctx->max_b_frames = 0;
        ctx->gop_size = 1000000;
        ctx->bit_rate = _options.Bitrate.BitsPerSecond;
        ctx->global_quality = 1;
        ctx->thread_count = Math.Min(8, Environment.ProcessorCount);
        
        ctx->flags |= ffmpeg.AV_CODEC_FLAG_LOW_DELAY;
        ctx->flags2 |= ffmpeg.AV_CODEC_FLAG2_FAST;
    }

    private void OpenCodec()
    {
        AVDictionary* opts = null;
        try
        {
            ffmpeg.av_dict_set(&opts, "preset", "ultrafast", 0);
            ffmpeg.av_dict_set(&opts, "tune", "zerolatency", 0);
            ffmpeg.av_dict_set(&opts, "x264-params", "keyint=infinite:min-keyint=0", 0);

            ffmpeg.avcodec_open2(_state->CodecContext, _state->Codec, &opts).FFmpegThrowIfError();
        }
        finally
        {
            ffmpeg.av_dict_free(&opts);
        }
    }

    private void InitializeFrames()
    {
        _state->TransferredFrame = ffmpeg.av_frame_alloc();
        if (_state->TransferredFrame is null)
            throw new OutOfMemoryException("Failed to allocate transferred frame");
        
        _state->EncoderFrame = ffmpeg.av_frame_alloc();
        if (_state->EncoderFrame is null)
            throw new OutOfMemoryException("Failed to allocate encoder frame");

        _state->EncoderFrame->width = _state->CodecContext->width;
        _state->EncoderFrame->height = _state->CodecContext->height;
        _state->EncoderFrame->format = (int)_state->CodecContext->pix_fmt;

        ffmpeg.av_frame_get_buffer(_state->EncoderFrame, 0).FFmpegThrowIfError();
    }

    private void InitializeStagingTexture(ID3D11Texture2D* sourceTexture)
    {
        D3D11_TEXTURE2D_DESC sourceDesc;
        sourceTexture->GetDesc(&sourceDesc);

        _state->StagingTextureDesc = new D3D11_TEXTURE2D_DESC
        {
            Width = sourceDesc.Width,
            Height = sourceDesc.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = sourceDesc.Format,
            SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
            Usage = D3D11_USAGE.D3D11_USAGE_STAGING,
            CPUAccessFlags = (uint)D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ,
            BindFlags = 0,
            MiscFlags = 0
        };

        _state->Device->CreateTexture2D(&_state->StagingTextureDesc, null, &_state->StagingTexture)
            .ThrowIfFailed();
    }

    private void InitializeSwsContext()
    {
        _state->SwsContext = ffmpeg.sws_getContext(
            (int)_state->StagingTextureDesc.Width,
            (int)_state->StagingTextureDesc.Height,
            AVPixelFormat.AV_PIX_FMT_BGRA,
            _state->CodecContext->width,
            _state->CodecContext->height,
            _state->CodecContext->pix_fmt,
            ffmpeg.SWS_BILINEAR,
            null,
            null,
            null);

        if (_state->SwsContext is null)
            throw new InvalidOperationException("Failed to create SWS context");
    }

    private void ConvertAndScaleFrame()
    {
        _state->TransferredFrame->width = (int)_state->StagingTextureDesc.Width;
        _state->TransferredFrame->height = (int)_state->StagingTextureDesc.Height;
        _state->TransferredFrame->data[0] = (byte*)_state->StagingResource.pData;
        _state->TransferredFrame->linesize[0] = (int)_state->StagingResource.RowPitch;
        _state->TransferredFrame->format = (int)AVPixelFormat.AV_PIX_FMT_BGRA;
        
        ffmpeg.sws_scale(
            _state->SwsContext,
            _state->TransferredFrame->data,
            _state->TransferredFrame->linesize,
            0,
            _state->TransferredFrame->height,
            _state->EncoderFrame->data,
            _state->EncoderFrame->linesize).FFmpegThrowIfError();
    }

    private uint EncodeFrame(Memory<byte> outputBuffer, bool forceKeyFrame, TimeSpan timestamp)
    {
        long pts = (long)timestamp.TotalMilliseconds;
        long durationMs = (long)(timestamp - _lastTimestamp).TotalMilliseconds;
        
        _state->EncoderFrame->pts = pts;
        _state->EncoderFrame->duration = durationMs;
        
        if (forceKeyFrame || _isFirstFrame)
        {
            Log.Info("Force key frame!");
            _state->EncoderFrame->pict_type = AVPictureType.AV_PICTURE_TYPE_I;
            _state->EncoderFrame->flags |= ffmpeg.AV_FRAME_FLAG_KEY;
            _isFirstFrame = false;
        }
        else
        {
            _state->EncoderFrame->pict_type = AVPictureType.AV_PICTURE_TYPE_NONE;
            _state->EncoderFrame->flags &= ~ffmpeg.AV_FRAME_FLAG_KEY;
        }
        
        _lastTimestamp = timestamp;
        
        ffmpeg.avcodec_send_frame(_state->CodecContext, _state->EncoderFrame).FFmpegThrowIfError();
        return ReceiveEncodedPackets(outputBuffer);
    }

    private uint ReceiveEncodedPackets(Memory<byte> outputBuffer)
    {
        int totalSize = 0;
        var outputSpan = outputBuffer.Span;

        while (true)
        {
            AVPacket* packet = ffmpeg.av_packet_alloc();
            if (packet is null)
                break;

            try
            {
                int ret = ffmpeg.avcodec_receive_packet(_state->CodecContext, packet);
                if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
                    break;

                if (ret < 0)
                {
                    ret.FFmpegThrowIfError();
                    break;
                }
                
                if (totalSize + packet->size > outputSpan.Length)
                {
                    Log.Warn().Append("Output buffer too small for encoded packet").Log();
                    break;
                }
                
                var packetData = new ReadOnlySpan<byte>(packet->data, packet->size);
                packetData.CopyTo(outputSpan.Slice(totalSize));
                totalSize += packet->size;
            }
            finally
            {
                ffmpeg.av_packet_free(&packet);
            }
        }

        return (uint)totalSize;
    }

    public void Dispose()
    {
        if (_state->EncoderFrame is not null)
        {
            ffmpeg.av_frame_free(&_state->EncoderFrame);
        }

        if (_state->TransferredFrame is not null)
        {
            ffmpeg.av_frame_free(&_state->TransferredFrame);
        }

        if (_state->CodecContext is not null)
        {
            ffmpeg.avcodec_free_context(&_state->CodecContext);
        }

        if (_state->SwsContext is not null)
        {
            ffmpeg.sws_freeContext(_state->SwsContext);
        }

        if (_state->StagingTexture is not null)
        {
            _state->StagingTexture->Release();
        }

        if (_state->DeviceContext is not null)
        {
            _state->DeviceContext->Release();
        }

        if (_state->Device is not null)
        {
            _state->Device->Release();
        }

        NativeMemory.Free(_state);
    }

    private struct State
    {
        public ID3D11Device* Device;
        public ID3D11DeviceContext* DeviceContext;

        public AVCodec* Codec;
        public AVCodecContext* CodecContext;

        public AVFrame* TransferredFrame;
        public AVFrame* EncoderFrame;

        public D3D11_TEXTURE2D_DESC StagingTextureDesc;
        public ID3D11Texture2D* StagingTexture;
        public D3D11_MAPPED_SUBRESOURCE StagingResource;

        public SwsContext* SwsContext;
    }
}