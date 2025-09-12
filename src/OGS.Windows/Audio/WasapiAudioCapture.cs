using OGS.Core.Common;
using OGS.Core.Common.Audio;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace OGS.Windows.Audio;

public sealed unsafe class WasapiAudioCapture : IDisposable
{
    private static readonly Log Log = LogManager.GetLogger<WasapiAudioCapture>();

    private readonly WasapiAudioCaptureOptions _options;
    private readonly Thread _thread;
    private readonly State* _state = MemoryHelper.AllocZeroed<State>();

    private readonly ManualResetEventSlim _activateCompleteEvent = new(false);
    private readonly AutoResetEvent _sampleReadyEvent = new(false);
    private IAudioEncoder? _encoder;

    private readonly byte[] _sourceBuffer = new byte[1024 * 256];
    private int _sourceBufferPosition = 0;

    private bool _exit;

    private const string VadLoopbackStr = "VAD\\Process_Loopback";

    public WasapiAudioCapture(WasapiAudioCaptureOptions options)
    {
        _options = options;

        _thread = new Thread(ThreadStart);
        _thread.Name = "Wasapi";
        _thread.Start();
    }

    private void ThreadStart()
    {
        Log.Info("Wasapi thread -> start");

        try
        {
            InnerThreadStart();
        }
        catch (Exception ex)
        {
            Log.Error("Wasapi thread error", ex);
        }
        finally
        {
            Cleanup();
            Log.Info("Wasapi thread -> end");
        }
    }

    private void InnerThreadStart()
    {
        Initialize();

        while (!_exit)
        {
            _sampleReadyEvent.WaitOne();
            ProcessSample();
        }
    }

    private void ProcessSample()
    {
        uint nextPacketSize = 0;
        _state->CaptureClient->GetNextPacketSize(&nextPacketSize)
            .ThrowIfFailed("GetNextPacketSize failed");

        int bytesRecorded = 0;

        while (nextPacketSize != 0)
        {
            byte* buffer = null;
            uint numFrames;
            uint _flags;

            _state->CaptureClient->GetBuffer(&buffer, &numFrames, &_flags, null, null)
                .ThrowIfFailed("GetBuffer failed");

            _AUDCLNT_BUFFERFLAGS flags = (_AUDCLNT_BUFFERFLAGS)_flags;
            var currentBufferSize = numFrames * (2 * 16 / 8);

            if (Math.Max(0, _sourceBuffer.Length - bytesRecorded) < currentBufferSize && bytesRecorded > 0)
            {
                break;
            }

            if ((flags & _AUDCLNT_BUFFERFLAGS.AUDCLNT_BUFFERFLAGS_SILENT) != _AUDCLNT_BUFFERFLAGS.AUDCLNT_BUFFERFLAGS_SILENT)
            {
                Marshal.Copy((nint)buffer, _sourceBuffer, bytesRecorded, (int)currentBufferSize);
                bytesRecorded += (int)currentBufferSize;
                _sourceBufferPosition = bytesRecorded;
            }

            _state->CaptureClient->ReleaseBuffer(numFrames)
                .ThrowIfFailed("ReleaseBuffer failed");

            _state->CaptureClient->GetNextPacketSize(&nextPacketSize)
                .ThrowIfFailed("NextPacketSize failed");
        }

        if (_encoder is null)
        {
            var inputFormat = GetInputFormat();

            _encoder = _options.EncoderFactory(new AudioCaptureState
            {
                Channels = inputFormat.nChannels,
                SampleRate = inputFormat.nSamplesPerSec
            });
        }


        if (_sourceBufferPosition > 0)
        {
            _encoder.Encode(new ReadOnlySpan<byte>(_sourceBuffer, 0, _sourceBufferPosition));
            _sourceBufferPosition = 0;
        }
    }

    private void Initialize()
    {
        WasapiCompletionHandler completionHandler = new WasapiCompletionHandler(_activateCompleteEvent);
        IActivateAudioInterfaceAsyncOperation* operation = null;

        AUDIOCLIENT_ACTIVATION_PARAMS activationParams = CreateActivationParams();
        PROPVARIANT pv = new PROPVARIANT() { vt = (ushort)VarEnum.VT_BLOB };
        pv.blob.pBlobData = (byte*)&activationParams;
        pv.blob.cbSize = (uint)sizeof(AUDIOCLIENT_ACTIVATION_PARAMS);

        IUnknown* result = null;


        try
        {
            fixed (char* ptr = VadLoopbackStr)
            {
                ActivateAudioInterfaceAsync(ptr,
                        __uuidof<IAudioClient>(),
                        &pv,
                        (IActivateAudioInterfaceCompletionHandler*)&completionHandler,
                        &operation)
                    .ThrowIfFailed("ActivateAudioInterfaceAsync failed");
            }

            _activateCompleteEvent.WaitHandle.WaitOne();

            HRESULT hr;
            operation->GetActivateResult(&hr, &result)
                .ThrowIfFailed("GetActivateResult failed");

            hr.ThrowIfFailed("GetActivateResult return error");

            result->QueryInterface(Uuidof<IAudioClient>(), (void**)&_state->AudioClient)
                     .ThrowIfFailed("QI for IAudioClient failed");

            WAVEFORMATEX format = GetInputFormat();


            _state->AudioClient->Initialize(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED,
                    AUDCLNT.AUDCLNT_STREAMFLAGS_EVENTCALLBACK | AUDCLNT.AUDCLNT_STREAMFLAGS_LOOPBACK,
                    100000,
                    0,
                    &format,
                    null)
                .ThrowIfFailed("IAudioClient->Initialize failed");

#pragma warning disable CS0618 // Type or member is obsolete
            _state->AudioClient->SetEventHandle((HANDLE)_sampleReadyEvent.Handle)
#pragma warning restore CS0618 // Type or member is obsolete
                .ThrowIfFailed("SetEventHandle failed");

            _state->AudioClient->GetService(Uuidof<IAudioCaptureClient>(), (void**)&_state->CaptureClient)
                .ThrowIfFailed("IAudioClient->GetService failed");

            _state->AudioClient->Start()
                .ThrowIfFailed("IAudioClient->Start failed");

            Log.Info("Created WASAPI capture session");
        }
        finally
        {
            completionHandler.Dispose();
            ReleaseAndNull(ref result);
            ReleaseAndNull(ref operation);
        }
    }

    private AUDIOCLIENT_ACTIVATION_PARAMS CreateActivationParams()
    {
        //Todo
        if (_options.Mode == Core.Config.Data.DisplayCaptureAudioMode.CaptureAll)
        {
            return new AUDIOCLIENT_ACTIVATION_PARAMS()
            {
                ActivationType = AUDIOCLIENT_ACTIVATION_TYPE.AUDIOCLIENT_ACTIVATION_TYPE_PROCESS_LOOPBACK,
                Anonymous = new AUDIOCLIENT_ACTIVATION_PARAMS._Anonymous_e__Union
                {
                    ProcessLoopbackParams = new AUDIOCLIENT_PROCESS_LOOPBACK_PARAMS
                    {
                        ProcessLoopbackMode = PROCESS_LOOPBACK_MODE.PROCESS_LOOPBACK_MODE_EXCLUDE_TARGET_PROCESS_TREE,
                        TargetProcessId = 1
                    }
                }
            };
        }

        return new AUDIOCLIENT_ACTIVATION_PARAMS()
        {
            ActivationType = AUDIOCLIENT_ACTIVATION_TYPE.AUDIOCLIENT_ACTIVATION_TYPE_PROCESS_LOOPBACK,
            Anonymous = new AUDIOCLIENT_ACTIVATION_PARAMS._Anonymous_e__Union
            {
                ProcessLoopbackParams = new AUDIOCLIENT_PROCESS_LOOPBACK_PARAMS
                {
                    ProcessLoopbackMode = _options.Mode == Core.Config.Data.DisplayCaptureAudioMode.IncludeProcess ?
                        PROCESS_LOOPBACK_MODE.PROCESS_LOOPBACK_MODE_INCLUDE_TARGET_PROCESS_TREE
                    : PROCESS_LOOPBACK_MODE.PROCESS_LOOPBACK_MODE_EXCLUDE_TARGET_PROCESS_TREE,
                    TargetProcessId = _options.ProcessId
                }
            }
        };
    }

    private WAVEFORMATEX GetInputFormat()
    {
        WAVEFORMATEX format = new WAVEFORMATEX();

        format.nChannels = 2;
        format.wFormatTag = 1;
        format.nSamplesPerSec = 48000;
        format.wBitsPerSample = 16;
        format.nBlockAlign = (ushort)(format.nChannels * (format.wBitsPerSample / 8));
        format.nAvgBytesPerSec = (uint)(48000 * format.nBlockAlign);
        return format;
    }

    private void Cleanup()
    {
        // Release in proper order with specific handling for each type
        if (_state->CaptureClient != null)
        {
            _state->CaptureClient->Release();
            _state->CaptureClient = null;
        }

        if (_state->AudioClient != null)
        {
            _state->AudioClient->Release();
            _state->AudioClient = null;
        }

        if (_state != null)
        {
            NativeMemory.Free(_state);
        }

        _encoder?.Dispose();
        _encoder = null;
    }

    public void Dispose()
    {
        lock (this)
        {
            if (_exit)
                return;

            _exit = true;
            _sampleReadyEvent.Set();
            _thread.Join();

            _activateCompleteEvent.Dispose();
            _sampleReadyEvent.Dispose();
        }
    }

    private struct State
    {
        public IAudioClient* AudioClient;
        public IAudioCaptureClient* CaptureClient;
    }
}