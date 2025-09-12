using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace OGS.Windows.HookLib.Com;

public static unsafe class ComHooks
{
    public static IUnknownReleaseDelegate OriginalReleaseDelegate = null!;
    public static IdxgiSwapChainResizeTargetDelegate OriginalSwapChainResizeTargetDelegate = null!;
    public static IdxgiSwapChainResizeBuffersDelegate OriginalSwapChainResizeBuffersDelegate = null!;
    public static IdxgiSwapChainPresentDelegate OriginalSwapChainPresentDelegate = null!;
    public static IdxgiSwapChainGetBufferDelegate OriginalSwapChainGetBufferDelegate = null!;

    public delegate uint IUnknownReleaseDelegate(IUnknown* @this);
    public delegate HRESULT IdxgiSwapChainResizeTargetDelegate(IDXGISwapChain* @this, DXGI_MODE_DESC* desc);
    public delegate HRESULT IdxgiSwapChainResizeBuffersDelegate(IDXGISwapChain* @this, uint bufferCount, uint width, uint height, DXGI_FORMAT format, uint flags);
    public delegate HRESULT IdxgiSwapChainPresentDelegate(IDXGISwapChain* @this, uint a, uint b);
    public delegate HRESULT IdxgiSwapChainGetBufferDelegate(IDXGISwapChain* @this, uint buffer, Guid* riid, void** ppSurface);

    public static void HookIUknownRelease(IUnknownReleaseDelegate hook)
    {
        PatchVTable((IUnknown*)ComFakeObject.GetFakeSwapChain(), 2, hook, ref OriginalReleaseDelegate);
    }

    public static void RestoreIUnknownRelease()
    {
        if (OriginalReleaseDelegate is not null)
        {
            PatchVTable((IUnknown*)ComFakeObject.GetFakeSwapChain(), 2, OriginalReleaseDelegate);
        }
    }

    public static void RestoreSwapChainResizeTargets()
    {
        if (OriginalSwapChainResizeTargetDelegate is not null)
        {
            PatchVTable((IUnknown*)ComFakeObject.GetFakeSwapChain(), 14, OriginalSwapChainResizeTargetDelegate);
        }
    }
    public static void RestoreSwapChainResizeBuffers()
    {
        if (OriginalSwapChainResizeBuffersDelegate is not null)
        {
            PatchVTable((IUnknown*)ComFakeObject.GetFakeSwapChain(), 13, OriginalSwapChainResizeBuffersDelegate);
        }
    }

    public static void RestoreSwapChainGetBuffer()
    {
        if (OriginalSwapChainGetBufferDelegate is not null)
        {
            PatchVTable((IUnknown*)ComFakeObject.GetFakeSwapChain(), 9, OriginalSwapChainGetBufferDelegate);
        }
    }

    public static void HookSwapChainGetBuffer(IdxgiSwapChainGetBufferDelegate hook)
    {
        PatchVTable((IUnknown*)ComFakeObject.GetFakeSwapChain(), 9, hook, ref OriginalSwapChainGetBufferDelegate);
    }

    public static void HookSwapChainResizeTargets(IdxgiSwapChainResizeTargetDelegate hook)
    {
        PatchVTable((IUnknown*)ComFakeObject.GetFakeSwapChain(), 14, hook, ref OriginalSwapChainResizeTargetDelegate);
    }

    public static void HookSwapChainResizeBuffers(IdxgiSwapChainResizeBuffersDelegate hook)
    {
        PatchVTable((IUnknown*)ComFakeObject.GetFakeSwapChain(), 13, hook, ref OriginalSwapChainResizeBuffersDelegate);
    }

    public static void HookSwapChainPresent(IdxgiSwapChainPresentDelegate hook)
    {
        PatchVTable((IUnknown*)ComFakeObject.GetFakeSwapChain(), 8, hook, ref OriginalSwapChainPresentDelegate);
    }

    public static void RestoreSwapChainPresent()
    {
        if (OriginalSwapChainPresentDelegate is not null)
        {
            PatchVTable((IUnknown*)ComFakeObject.GetFakeSwapChain(), 8, OriginalSwapChainPresentDelegate);
        }
    }

    private static void PatchVTable<TDelegate>(IUnknown* ptr, int offset, TDelegate hook, ref TDelegate original)
        where TDelegate : notnull
    {
        TDelegate previousDelegate = PatchVTable(ptr, offset, hook);

        if (original is null)
            original = previousDelegate;
    }

    private static TDelegate PatchVTable<TDelegate>(IUnknown* ptr, int offset, TDelegate hook)
        where TDelegate : notnull
    {
        void** vTable = *(void***)ptr;
        TDelegate old = Marshal.GetDelegateForFunctionPointer<TDelegate>((nint)vTable[offset]);

        uint oldProtect = 0;
        VirtualProtect(&vTable[offset], (nuint)sizeof(void*), PAGE.PAGE_EXECUTE_READWRITE, &oldProtect);
        vTable[offset] = (void*)Marshal.GetFunctionPointerForDelegate(hook);
        VirtualProtect(&vTable[offset], (nuint)sizeof(void*), oldProtect, &oldProtect);
        return old;
    }


    public static readonly Lock SyncRoot = new();
}
