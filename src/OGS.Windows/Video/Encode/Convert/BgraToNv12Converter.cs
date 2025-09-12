using System.Runtime.InteropServices;
using OGS.Core.Common;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace OGS.Windows.Video.Encode.Convert;

internal sealed unsafe class BgraToNv12Converter : IBgraToNv12Converter
{
    private State* _state = MemoryHelper.AllocZeroed<State>();

    public BgraToNv12Converter(ID3D11Device* device,
        ID3D11DeviceContext* deviceContext)
    {
        device->QueryInterface(Uuidof<ID3D11VideoDevice>(), (void**)&_state->VideoDevice).ThrowIfFailed();
        deviceContext->QueryInterface(Uuidof<ID3D11VideoContext1>(), (void**)&_state->VideoContext).ThrowIfFailed();
    }

    public void Convert(ID3D11Texture2D* bgraTexture, ID3D11Texture2D* nv12Texture)
    {
        if (_state is null)
            throw new ObjectDisposedException(nameof(BgraToNv12Converter));

        D3D11_TEXTURE2D_DESC bgraDesc, nv12Desc;
        bgraTexture->GetDesc(&bgraDesc);
        nv12Texture->GetDesc(&nv12Desc);

        D3D11_VIDEO_PROCESSOR_CONTENT_DESC contentDesc = new();
        contentDesc.InputFrameFormat = D3D11_VIDEO_FRAME_FORMAT.D3D11_VIDEO_FRAME_FORMAT_PROGRESSIVE;
        contentDesc.InputWidth = bgraDesc.Width;
        contentDesc.OutputWidth = bgraDesc.Width;
        contentDesc.OutputHeight = bgraDesc.Height;
        contentDesc.InputHeight = bgraDesc.Height;

        contentDesc.Usage = D3D11_VIDEO_USAGE.D3D11_VIDEO_USAGE_OPTIMAL_SPEED;
        
        if (_state->VideoProcessor is null || _state->VideoProcessorEnumerator is null)
        {
            _state->VideoDevice->CreateVideoProcessorEnumerator(&contentDesc, &_state->VideoProcessorEnumerator).ThrowIfFailed();
            _state->VideoDevice->CreateVideoProcessor(_state->VideoProcessorEnumerator, 0, &_state->VideoProcessor).ThrowIfFailed();
        }

        RECT rec = new RECT();
        rec.right = (int)bgraDesc.Width;
        rec.bottom = (int)bgraDesc.Height;

        _state->VideoContext->VideoProcessorSetStreamSourceRect(_state->VideoProcessor,
            0, true, &rec);
        _state->VideoContext->VideoProcessorSetStreamDestRect(_state->VideoProcessor,
            0, true, &rec);

        D3D11_VIDEO_PROCESSOR_INPUT_VIEW_DESC inputViewDesc = new();
        inputViewDesc.FourCC = 0;
        inputViewDesc.ViewDimension = D3D11_VPIV_DIMENSION.D3D11_VPIV_DIMENSION_TEXTURE2D;
        inputViewDesc.Texture2D.MipSlice = 0;
        inputViewDesc.Texture2D.ArraySlice = 0;
        ID3D11VideoProcessorInputView* inputView;
        _state->VideoDevice->CreateVideoProcessorInputView((ID3D11Resource*)bgraTexture, _state->VideoProcessorEnumerator,
            &inputViewDesc, &inputView).ThrowIfFailed("CreateVideoProcessorInputView failed");

        D3D11_VIDEO_PROCESSOR_OUTPUT_VIEW_DESC outputViewDesc = new();
        outputViewDesc.ViewDimension = D3D11_VPOV_DIMENSION.D3D11_VPOV_DIMENSION_TEXTURE2D;
        outputViewDesc.Texture2D.MipSlice = 0;

        ID3D11VideoProcessorOutputView* outputView;
        _state->VideoDevice->CreateVideoProcessorOutputView(
            (ID3D11Resource*)nv12Texture, _state->VideoProcessorEnumerator,
            &outputViewDesc, &outputView).ThrowIfFailed("CreateVideoProcessorOutputView failed");

        D3D11_VIDEO_PROCESSOR_STREAM streamData = new();
        streamData.Enable = true;
        streamData.pInputSurface = inputView;
        _state->VideoContext->VideoProcessorBlt(_state->VideoProcessor,
            outputView, 0, 1, &streamData).ThrowIfFailed("VideoProcessBlt failed");

        inputView->Release();
        outputView->Release();
    }

    public void Dispose()
    {
        ReleaseAndNull(ref _state->VideoDevice);
        ReleaseAndNull(ref _state->VideoContext);
        ReleaseAndNull(ref _state->VideoProcessor);
        ReleaseAndNull(ref _state->VideoProcessorEnumerator);

        NativeMemory.Free(_state);
        _state = null;
    }

    private struct State
    {
        public ID3D11VideoDevice* VideoDevice;
        public ID3D11VideoContext1* VideoContext;

        public ID3D11VideoProcessor* VideoProcessor;
        public ID3D11VideoProcessorEnumerator* VideoProcessorEnumerator;
    }
}
