using OGS.Windows.Common;
using OGS.Windows.HookShared;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace OGS.Windows.Video.Capture.Hook;

public static unsafe class HookInjector
{
    private static Log _log = LogManager.GetLogger(typeof(HookInjector));

    public static void Inject(uint targetPid, HookInitArgs startArgs)
    {
        HANDLE hProc = default;
        void* remoteBuffer = null;
        HMODULE kernel32Module = default;
        void* loadLibraryPtr = default;
        HANDLE remoteThreadHandle = default;
        void* remoteStartArgsBuffer = null;
        HANDLE remoteStartThread = default;

        try
        {
            string dllPath = GetHookDllPath();
            hProc = OpenTargetProcess(targetPid);

            remoteBuffer = AllocDllPathBuffer(hProc, dllPath);
            WriteDllPath(hProc, remoteBuffer, dllPath);

            kernel32Module = GetKernel32Module();
            loadLibraryPtr = GetLoadLibraryPtr(kernel32Module);

            //Create a remote thread in the target process to call LoadLibraryW with out DLL path
            remoteThreadHandle = CreateRemoteThread(hProc, null, 0,
                (delegate* unmanaged<void*, uint>)loadLibraryPtr,
                remoteBuffer, 0, null);

            if (remoteThreadHandle == HANDLE.NULL)
                Win32Helpers.ThrowWin32Exception("CreateRemoteThread failed");

            _log.Info("Waiting for remote thread to start");

            ((HRESULT)WaitForSingleObject(remoteThreadHandle, 3000))
                .ThrowIfFailed("WaitForSingleObject failed for remote loadlibrary thread");

            _log.Info("Remote thread started");

            //We loaded out DLL into the remote process, now we need to call our start function.
            void* startFunctionAddr = GetStartFunctionAddress(dllPath, "HookStart");

            //Write the start function arguments to the remote process memory
            remoteStartArgsBuffer = VirtualAllocEx(hProc, null,
                (uint)sizeof(HookInitArgs),
                MEM.MEM_COMMIT | MEM.MEM_RESERVE, PAGE.PAGE_READWRITE);

            if (remoteStartArgsBuffer is null)
                Win32Helpers.ThrowWin32Exception("VirtualAllocEx failed for start args buffer");

            nuint bytesWritten = 0;

            if (WriteProcessMemory(hProc, remoteStartArgsBuffer, &startArgs,
                (nuint)sizeof(HookInitArgs), &bytesWritten) == 0) 
            {
                Win32Helpers.ThrowWin32Exception("WriteProcessMemory failed for start args");
            }

            remoteStartThread = CreateRemoteThread(hProc, default, 0,
                (delegate* unmanaged<void*, uint>)startFunctionAddr, remoteStartArgsBuffer, 0, null);

            _log.Info("Waiting for remote function to finish");

            ((HRESULT)WaitForSingleObject(remoteStartThread, 3000))
                .ThrowIfFailed("WaitForSingleObject failed for remote start function thread");

            _log.Info("Remote function finished");
        }
        finally
        {
            if (hProc.Value is not null)
                CloseHandle(hProc);

            if (remoteBuffer is not null)
                VirtualFreeEx(hProc, remoteBuffer, 0, MEM.MEM_RELEASE);

            if (kernel32Module.Value is not null)
                FreeLibrary(kernel32Module);

            if (remoteThreadHandle != HANDLE.NULL)
                CloseHandle(remoteThreadHandle);

            if (remoteStartArgsBuffer is not null)
                VirtualFreeEx(hProc, remoteStartArgsBuffer, 0, MEM.MEM_RELEASE);

            if (remoteStartThread != HANDLE.NULL)
                CloseHandle(remoteStartThread);
        }
    }

    private static HANDLE OpenTargetProcess(uint targetPid)
    {
        HANDLE hProc = OpenProcess(PROCESS.PROCESS_ALL_ACCESS, false, targetPid);

        if (hProc.Value is null)
        {
            throw new InvalidOperationException($"Failed to open process with PID {targetPid}");
        }
        return hProc;
    }

    private static void* AllocDllPathBuffer(HANDLE hProc, string dllPath)
    {
        void* remoteMemoryPtr = VirtualAllocEx(hProc, null,
            (uint)((dllPath.Length + 1) * Marshal.SizeOf<char>()),
            MEM.MEM_COMMIT | MEM.MEM_RESERVE, PAGE.PAGE_READWRITE);

        if (remoteMemoryPtr is null)
        {
            Win32Helpers.ThrowWin32Exception("VirtualAllocEx failed");
        }

        return remoteMemoryPtr;
    }

    private static void WriteDllPath(HANDLE hProc, void* remoteBuffer, string dllPath)
    {
        nuint bytesWritten = 0;
        Span<byte> dllPathBytes = stackalloc byte[1024];
        int dllPathSize = Encoding.Unicode.GetBytes(dllPath, dllPathBytes);

        fixed (byte* dllPathBuffer = dllPathBytes)
        {
            if (!WriteProcessMemory(hProc,
                remoteBuffer, dllPathBuffer, (nuint)dllPathSize, &bytesWritten))
            {
                Win32Helpers.ThrowWin32Exception("WriteProcessMemory failed");
            }
        }
    }

    private static HMODULE GetKernel32Module()
    {
        fixed (char* k32 = "kernel32.dll")
        {
            HMODULE hk32 = GetModuleHandle(k32);

            if (hk32 is { Value: null })
                Win32Helpers.ThrowWin32Exception("GetModuleHandle failed for kernel32");

            return hk32;
        }
    }

    private static void* GetLoadLibraryPtr(HMODULE kernel32Module)
    {
        ReadOnlySpan<byte> loadLibraryWBytes = "LoadLibraryW"u8;

        fixed (byte* loadLibraryTextPtr = loadLibraryWBytes)
        {
            void* procAddr = GetProcAddress(kernel32Module, (sbyte*)loadLibraryTextPtr);

            if (procAddr is null)
                Win32Helpers.ThrowWin32Exception("GetProcAddress failed for LoadLibraryW");

            return procAddr;
        }
    }

    private static void* GetStartFunctionAddress(string dllPath, string functionName)
    {
        HMODULE dllModule = default;

        fixed (char* moduleName = dllPath)
        {
            dllModule = LoadLibraryW(moduleName);
        }

        if (dllModule.Value is null)
        {
            Win32Helpers.ThrowWin32Exception("Failed to get DLL module handle");
        }

        byte[] b = Encoding.ASCII.GetBytes("HookStart");

        fixed (byte* _b = b)
        {
            var procAddr = GetProcAddress(dllModule, (sbyte*)_b);
            FreeLibrary(dllModule);

            if (procAddr is null)
                Win32Helpers.ThrowWin32Exception("Failed to get start function address");

            return procAddr;
        }
    }

    private static string GetHookDllPath()
    {
        try
        {
            if (Environment.GetEnvironmentVariable("OGS_HOOK_PATH") is string envPath && !string.IsNullOrEmpty(envPath))
            {
                _log.Info($"Using hook dll path from environment variable: '{envPath}'");
                return envPath;
            }
        }
        catch (Exception)
        {
            //Ignore
        }

        string lib = AppDomain.CurrentDomain.BaseDirectory + "OGS.Windows.HookLib.dll";
        _log.Info($"Using default hook dll path: '{lib}'");
        return lib;
    }
}