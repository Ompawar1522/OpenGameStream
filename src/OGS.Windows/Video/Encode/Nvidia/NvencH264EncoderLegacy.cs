using Lennox.NvEncSharp;
using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;

namespace OGS.Windows.Video.Encode.Nvidia;

/// <summary>
/// Uses older nvenc API for use with older nvidia cards. Tested on an old GTX 680.
/// </summary>
public sealed unsafe class NvencH264EncoderLegacy : ID3DEncoder
{
    private static readonly Log Log = LogManager.GetLogger<NvencH264Encoder>();

    private NvEncoder _encoder;
    private NvEncCreateBitstreamBuffer _encoderBuffer;

    private readonly NvencH264EncoderOptions _options;

    public NvencH264EncoderLegacy(NvencH264EncoderOptions settings)
    {
        _options = settings;
    }

    public uint Encode(D3DEncodeArgs args)
    {
        if (_encoder.Handle == 0)
            Initialize(args.Texture);

        NvEncRegisterResource resourceParams = CreateRegisterResourceParams(args.Texture);
        LibNvEnc.CheckResult(_encoder, LibNvEnc.FunctionList.RegisterResource(_encoder, ref resourceParams));

        try
        {
            return EncodeWithResource(args, resourceParams);
        }
        finally
        {
            LibNvEnc.CheckResult(_encoder, LibNvEnc.FunctionList.UnregisterResource(_encoder, resourceParams.RegisteredResource));
        }
    }

    private uint EncodeWithResource(D3DEncodeArgs args, NvEncRegisterResource resource)
    {
        NvEncPicParams picParams = CreatePicParams(resource.AsInputPointer(), args.Keyframe);
        _encoder.EncodePicture(ref picParams);

        if (picParams.PictureType == NvEncPicType.Idr)
            Log.Info("Built keyframe");

        NvEncLockBitstream bitStream = default;
        bitStream.Version = LibNvEnc.NV_ENC_LOCK_BITSTREAM_VER;
        bitStream.OutputBitstream = _encoderBuffer.BitstreamBuffer.Handle;

        _encoder.LockBitstream(ref bitStream);

        try
        {
            return CopyEncodedData(args, bitStream);
        }
        finally
        {
            _encoder.UnlockBitstream(_encoderBuffer.BitstreamBuffer);
        }
    }

    private uint CopyEncodedData(D3DEncodeArgs args, NvEncLockBitstream bitStream)
    {
        void* data = (void*)bitStream.BitstreamBufferPtr;
        uint len = bitStream.BitstreamSizeInBytes;

        fixed (byte* target = args.Buffer.Span)
        {
            NativeMemory.Copy(data, target, len);
        }

        return len;
    }
    
    private NvEncPicParams CreatePicParams(NvEncInputPtr inputPtr, bool keyFrame)
    {
        return new NvEncPicParams()
        {
            Version = LibNvEnc.NV_ENC_PIC_PARAMS_VER,
            PictureStruct = NvEncPicStruct.Frame,
            BufferFmt = NvEncBufferFormat.Abgr,
            InputBuffer = inputPtr,
            OutputBitstream = _encoderBuffer.BitstreamBuffer,
            PictureType = keyFrame ? NvEncPicType.Idr : NvEncPicType.P,
            EncodePicFlags = (uint)(keyFrame ? 2 : 0),
        };
    }


    private NvEncRegisterResource CreateRegisterResourceParams(ID3D11Texture2D* texture)
    {
        return new NvEncRegisterResource()
        {
            Version = LibNvEnc.NV_ENC_REGISTER_RESOURCE_VER,
            BufferUsage = NvEncBufferUsage.NvEncInputImage,
            ResourceToRegister = (nint)texture
        };
    }

    private void Initialize(ID3D11Texture2D* texture)
    {
        Log.Info("Creating encoder devices");

        ID3D11Device* device = null;
        texture->GetDevice(&device);

        try
        {
            D3D11_TEXTURE2D_DESC textureDesc;
            texture->GetDesc(&textureDesc);

            _encoder = LibNvEnc.OpenEncoderForDirectX((nint)device);

            NvEncConfig config = _encoder.GetEncodePresetConfig(NvEncCodecGuids.H264,
                NvEncPresetGuids.LowLatencyHp).PresetCfg;
        
            config.GopLength = _options.GopLength;
            config.EncodeCodecConfig.H264Config.IdrPeriod = _options.GopLength;

            config.RcParams.RateControlMode = NvEncParamsRcMode.Cbr;
            config.RcParams.MaxBitRate = (uint)_options.Bitrate.BitsPerSecond;
            config.RcParams.AverageBitRate = (uint)_options.Bitrate.BitsPerSecond;
            config.RcParams.Version = LibNvEnc.NV_ENC_RC_PARAMS_VER;
            config.RcParams.ZeroReorderDelay = true;
            config.RcParams.VbvBufferSize = (uint)(config.RcParams.AverageBitRate * 1.5);
            config.RcParams.VbvInitialDelay = config.RcParams.VbvBufferSize / 2;
        
            config.ProfileGuid = NvEncProfileGuids.H264Main;
            //config.EncodeCodecConfig.H264Config.Level = 62;
            config.EncodeCodecConfig.H264Config.EntropyCodingMode = NvEncH264EntropyCodingMode.Cabac;
            config.EncodeCodecConfig.H264Config.SliceMode = 3;
            config.EncodeCodecConfig.H264Config.SliceModeData = 1;
            config.EncodeCodecConfig.H264Config.RepeatSPSPPS = true;
        
            NvEncInitializeParams initParams = CreateInitParams(&config, textureDesc.Width, textureDesc.Height);
            _encoder.InitializeEncoder(ref initParams);
            _encoderBuffer = _encoder.CreateBitstreamBuffer();

            Log.Info("Create nv encoder");
        }
        finally
        {
            ReleaseAndNull(ref device);
        }
    }

    private NvEncInitializeParams CreateInitParams(NvEncConfig* config,
        uint width, uint height)
    {
        return new NvEncInitializeParams()
        {
            Version = LibNvEnc.NV_ENC_INITIALIZE_PARAMS_VER,
            EncodeGuid = NvEncCodecGuids.H264,
            FrameRateNum = _options.Framerate,
            EncodeWidth = width,
            EncodeHeight = height,
            FrameRateDen = 1,
            PresetGuid = NvEncPresetGuids.LowLatencyHp,
            EnablePTD = 1,
            EncodeConfig = config,
            ReportSliceOffsets = true,
        };
    }

    public void Dispose()
    {
        if (_encoder.Handle != 0)
        {
            _encoder.DestroyBitstreamBuffer(_encoderBuffer.BitstreamBuffer);
            _encoder.DestroyEncoder();
            _encoder = default;
        }
    }
}
