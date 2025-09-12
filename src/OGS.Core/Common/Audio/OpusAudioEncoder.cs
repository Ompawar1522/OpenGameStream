using FFmpeg.AutoGen;
using System.Runtime.InteropServices;

namespace OGS.Core.Common.Audio;

public sealed unsafe class OpusAudioEncoder : IAudioEncoder
{
    private static readonly Log Log = LogManager.GetLogger<OpusAudioEncoder>();

    private State* _state = MemoryHelper.AllocZeroed<State>();
    private readonly AudioCaptureState _captureState;
    private readonly AudioEncoderOptions _options;

    private readonly int _frameBytes;
    private readonly int _frameSamples;

    private byte* _buffer;
    private int _bufferLength = 0;
    private readonly int _maxBufferSize;

    public OpusAudioEncoder(AudioCaptureState captureState, AudioEncoderOptions options)
    {
        _captureState = captureState;
        _options = options;

        try
        {
            Initialize();

            _frameSamples = _state->CodecContext->frame_size;
            _frameBytes = _frameSamples * sizeof(short) * _state->CodecContext->ch_layout.nb_channels;

            _maxBufferSize = _frameBytes * 2;
            _buffer = (byte*)NativeMemory.AllocZeroed((nuint)_maxBufferSize);
        }
        catch (Exception)
        {
            Dispose();
            throw;
        }
    }

    private void Initialize()
    {
        _state->Codec = ffmpeg.avcodec_find_encoder_by_name("libopus");

        if (_state->Codec is null)
            throw new Exception("Could not find libopus encoder");

        _state->CodecContext = ffmpeg.avcodec_alloc_context3(_state->Codec);
        _state->CodecContext->codec_id = AVCodecID.AV_CODEC_ID_OPUS;
        _state->CodecContext->codec_type = AVMediaType.AVMEDIA_TYPE_AUDIO;
        _state->CodecContext->sample_fmt = AVSampleFormat.AV_SAMPLE_FMT_S16;
        _state->CodecContext->sample_rate = (int)_captureState.SampleRate;
        ffmpeg.av_channel_layout_default(&_state->CodecContext->ch_layout, 2);
        _state->CodecContext->bit_rate = _options.Bitrate.BitsPerSecond;

        AVDictionary* opts = null;  
        ffmpeg.av_dict_set(&opts, "application", "lowdelay", 0);
        ffmpeg.av_dict_set(&opts, "frame_duration", "5", 0);
        ffmpeg.av_dict_set(&opts, "vbr", "on", 0);

        ffmpeg.avcodec_open2(_state->CodecContext, _state->Codec, &opts).FFmpegThrowIfError();

        _state->Frame = ffmpeg.av_frame_alloc();
        _state->Frame->nb_samples = _state->CodecContext->frame_size;
        _state->Frame->ch_layout = _state->CodecContext->ch_layout;
        _state->Frame->format = (int)_state->CodecContext->sample_fmt;
        _state->Frame->sample_rate = _state->CodecContext->sample_rate;

        ffmpeg.av_frame_get_buffer(_state->Frame, 0).FFmpegThrowIfError();

        _state->Packet = ffmpeg.av_packet_alloc();

        Log.Info("Created libopus encoder");
    }

    public void Encode(ReadOnlySpan<byte> samples)
    {
        if (_state is null || _buffer is null)
            throw new ObjectDisposedException(nameof(OpusAudioEncoder));

        fixed (byte* samplesPtr = samples)
        {
            EncodeInternal(samplesPtr, samples.Length);
        }
    }

    private void EncodeInternal(byte* samples, int samplesLength)
    {
        int totalAvailable = _bufferLength + samplesLength;
        byte* currentPtr = samples;
        int remainingInput = samplesLength;

        if (_bufferLength > 0)
        {
            int needed = _frameBytes - _bufferLength;

            if (remainingInput >= needed)
            {
                Buffer.MemoryCopy(currentPtr, _buffer + _bufferLength, needed, needed);
                Buffer.MemoryCopy(_buffer, _state->Frame->data[0], _frameBytes, _frameBytes);

                EncodeFrame();

                currentPtr += needed;
                remainingInput -= needed;
                _bufferLength = 0;
            }
            else
            {
                Buffer.MemoryCopy(currentPtr, _buffer + _bufferLength, remainingInput, remainingInput);
                _bufferLength += remainingInput;
                return;
            }
        }

        while (remainingInput >= _frameBytes)
        {
            Buffer.MemoryCopy(currentPtr, _state->Frame->data[0], _frameBytes, _frameBytes);
            EncodeFrame();

            currentPtr += _frameBytes;
            remainingInput -= _frameBytes;
        }

        if (remainingInput > 0)
        {
            Buffer.MemoryCopy(currentPtr, _buffer, remainingInput, remainingInput);
            _bufferLength = remainingInput;
        }
    }

    private void EncodeFrame()
    {
        ffmpeg.avcodec_send_frame(_state->CodecContext, _state->Frame).FFmpegThrowIfError("avcodec_send_frame failed");
        ffmpeg.avcodec_receive_packet(_state->CodecContext, _state->Packet).FFmpegThrowIfError("avcodec_receive_packet failed");

        var encodedData = new ReadOnlySpan<byte>(_state->Packet->data, _state->Packet->size);

        _options.Callback(new EncodedAudioSample
        {
            Data = encodedData,
            SampleCount = (uint)_frameSamples
        });

        ffmpeg.av_packet_unref(_state->Packet);
    }

    public void Dispose()
    {
        if (_buffer != null)
        {
            NativeMemory.Free(_buffer);
            _buffer = null;
        }

        if(_state  != null)
        {
            ffmpeg.avcodec_send_frame(_state->CodecContext, null);
            ffmpeg.avcodec_receive_packet(_state->CodecContext, _state->Packet);

            ffmpeg.av_packet_free(&_state->Packet);
            ffmpeg.av_frame_free(&_state->Frame);
            ffmpeg.avcodec_free_context(&_state->CodecContext);

            NativeMemory.Free(_state);
            _state = null;
        }
    }

    private struct State
    {
        public AVCodecContext* CodecContext;
        public AVCodec* Codec;
        public AVFrame* Frame;
        public AVPacket* Packet;
    }
}