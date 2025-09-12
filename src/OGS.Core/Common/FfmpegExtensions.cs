using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace OGS.Core.Common;

public static class FfmpegExtensions
{
    private static readonly Log Log = LogManager.GetLogger("ffmpeg");

    private static av_log_set_callback_callback _logCallback;

    static FfmpegExtensions()
    {
        unsafe
        {
            _logCallback = LogCallback;
        }
    }

    public static void InitLogger()
    {
        ffmpeg.av_log_set_level(ffmpeg.AV_LOG_VERBOSE);
        ffmpeg.av_log_set_callback(_logCallback);
    }

    private static unsafe void LogCallback(void* @p0, int @p1,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string @p2, byte* @p3)
    {
        //Todo - format
        Log.Info($"FFMPEG: {p2}");
    }

    public static unsafe string av_strerror(int error)
    {
        var bufferSize = 1024;
        var buffer = stackalloc byte[bufferSize];
        ffmpeg.av_strerror(error, buffer, (ulong)bufferSize);
        var message = Marshal.PtrToStringAnsi((nint)buffer);
        return message!;
    }

    public static int FFmpegThrowIfError(this int error)
    {
        if (error < 0) throw new ApplicationException(av_strerror(error));
        return error;
    }

    public static int FFmpegThrowIfError(this int error, string message)
    {
        if (error < 0) 
            throw new ApplicationException(message + " | code (" + av_strerror(error) + ")");

        return error;
    }
}
