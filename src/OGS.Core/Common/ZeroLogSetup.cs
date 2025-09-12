using ZeroLog.Appenders;
using ZeroLog.Configuration;
using ZeroLog.Formatting;

namespace OGS.Core.Common;

public static class ZeroLogSetup
{
    public static void UseConsoleLogger(LogLevel level)
    {
        LogManager.Initialize(new ZeroLogConfiguration
        {
            RootLogger =
            {
                Appenders =
                {
                    new ConsoleAppender()
                    {
                        ColorOutput = true
                    }
                },
                Level = level,
                LogMessagePoolExhaustionStrategy = LogMessagePoolExhaustionStrategy.Allocate
            }
        });
    }
    
    public static void UseCallbackLogger(Action<string> callback, LogLevel level = LogLevel.Info)
    {
        LogManager.Initialize(new ZeroLogConfiguration
        {
            RootLogger =
            {
                Appenders =
                {
                    new CallbackAppender(callback),
                    new ConsoleAppender()
                    {
                        ColorOutput = true
                    },
                    new DateAndSizeRollingFileAppender("logs")
                    {
                        FileNamePrefix = "OGS",
                        Level = level
                    }
                },
                Level = level
            }
        });
    }

    class CallbackAppender : Appender
    {
        private readonly Action<string> _callback;

        private readonly DefaultFormatter _formatter;

        public CallbackAppender(Action<string> callback)
        {
            _callback = callback;
            
            _formatter = new DefaultFormatter()
            {
                PrefixPattern = "(%thread) %{time:\\:hh\\:mm\\:ss\\.ff} | ",
            };
        }

        public override void WriteMessage(LoggedMessage message)
        {
            _callback(new string(_formatter.FormatMessage(message)));

            if (message.Exception is not null)
                _callback("\t" + message.Exception.ToString() + "\n\t" + message.Exception.StackTrace);
        }
    }
}