using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CL = Common.Logging;
using ML = Microsoft.Extensions.Logging;

namespace Ipfs.Engine
{
    /// <summary>
    ///   A bridge between MS logging and Commong Logging
    /// </summary>
    class MSCommonLoggingProvider : ML.ILogger, ML.ILoggerProvider
    {
        class Scope : IDisposable
        {
            public void Dispose()
            {
            }
        }

        CL.ILog logger;
        ML.LogLevel notBelow;

        public MSCommonLoggingProvider(CL.ILog logger, ML.LogLevel notBelow = ML.LogLevel.Information)
        {
            this.logger = logger;
            this.notBelow = notBelow;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new Scope();
        }

        public bool IsEnabled(ML.LogLevel logLevel)
        {
            if ((int) logLevel < (int) notBelow)
                return false;

            switch (logLevel)
            {
                case ML.LogLevel.Critical: return logger.IsFatalEnabled;
                case ML.LogLevel.Debug: return logger.IsDebugEnabled;
                case ML.LogLevel.Error: return logger.IsErrorEnabled;
                case ML.LogLevel.Information: return logger.IsInfoEnabled;
                case ML.LogLevel.Trace: return logger.IsTraceEnabled;
                case ML.LogLevel.Warning: return logger.IsWarnEnabled;
                case ML.LogLevel.None:
                default:
                    return false;

            }
        }

        public void Log<TState>(ML.LogLevel logLevel, ML.EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var msg = formatter(state, exception);
            switch (logLevel)
            {
                case ML.LogLevel.Critical:
                    logger.Fatal(msg);
                    return;
                case ML.LogLevel.Debug:
                    logger.Debug(msg);
                    return;
                case ML.LogLevel.Error:
                    logger.Error(msg);
                    return;
                case ML.LogLevel.Information:
                    logger.Info(msg);
                    return;
                case ML.LogLevel.Trace:
                    logger.Trace(msg);
                    return;
                case ML.LogLevel.Warning:
                    logger.Warn(msg);
                    return;
            }
        }

        public ML.ILogger CreateLogger(string categoryName)
        {
            return this;
        }

        public void Dispose()
        {
        }
    }
}
