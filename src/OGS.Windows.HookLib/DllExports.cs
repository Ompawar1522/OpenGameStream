using OGS.Windows.HookLib.Common;
using OGS.Windows.HookShared;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ZeroLog;
using ZeroLog.Appenders;
using ZeroLog.Configuration;

namespace OGS.Windows.HookLib;

public static unsafe class DllExports
{
    private static bool _logInitialized;
    private static HookServer? _instance;
    private static readonly Lock _lock = new Lock();

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = "HookStart")]
    public static int HookStart(HookInitArgs* args)
    {
        using (_lock.EnterScope())
        {
            if (args->ShowConsole)
                ConsoleHelper.CreateConsole();

            InitializeLog(args->LogLevel);

            _instance?.Dispose();
            _instance = new HookServer(*args);
        }

        return 0;
    }

    private static void InitializeLog(LogLevel level)
    {
        if (_logInitialized)
            return;

        _logInitialized = true;

        LogManager.Initialize(new ZeroLogConfiguration
        {
            RootLogger =
            {
                Appenders =
                {
                    new ConsoleAppender()
                    {
                        ColorOutput = true,
                        Level = level
                    },
                    new DateAndSizeRollingFileAppender("logs")
                    {
                        FileNamePrefix = "OGSHOOK",
                        Level = level
                    }
                },
                Level =level
            }
        });
    }
}
