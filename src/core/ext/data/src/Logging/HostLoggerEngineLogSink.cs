using System;
using Microsoft.Extensions.Logging;
using saiflow.engine.Entity;
using saiflow.engine.Logging;
using saiflow.hub.Logging;

namespace saiflow.data.Logging
{
    /// <summary>
    /// 转发到 <see cref="ILogger"/> 的引擎日志 Sink。由宿主在 <c>LogSinkKind=HostLogger</c> 时注入。
    /// </summary>
    public sealed class HostLoggerEngineLogSink : IEngineLogSink
    {
        private readonly ILogger _engine;
        private readonly ILogger _job;

        public HostLoggerEngineLogSink(ILoggerFactory loggerFactory)
        {
            ArgumentNullException.ThrowIfNull(loggerFactory);
            _engine = loggerFactory.CreateLogger("saiflow.engine");
            _job = loggerFactory.CreateLogger("saiflow.engine.job");
        }

        public HostLoggerEngineLogSink(ILogger engineLogger, ILogger jobLogger = null)
        {
            _engine = engineLogger ?? throw new ArgumentNullException(nameof(engineLogger));
            _job = jobLogger ?? engineLogger;
        }

        public void Write(LogEntity entry)
        {
            if (entry == null)
                return;

            var level = MapLevel(entry.Severity, entry.Priority);
            _engine.Log(level, "[{Title}] {Message}{Stack}",
                entry.Title ?? "",
                entry.Message ?? "",
                string.IsNullOrEmpty(entry.StackTrace) ? "" : " | " + entry.StackTrace);
        }

        public void WriteJob(JobLogEntity entry)
        {
            if (entry == null)
                return;

            var level = entry.Status is short s && s < 0 ? LogLevel.Error : LogLevel.Information;
            if (!string.IsNullOrEmpty(entry.StackTrace))
                level = LogLevel.Error;

            _job.Log(level, "[{JobType}/{JobName}] status={Status} {Message}{Stack}",
                entry.JobType ?? "",
                entry.JobName ?? "",
                entry.Status,
                entry.Message ?? "",
                string.IsNullOrEmpty(entry.StackTrace) ? "" : " | " + entry.StackTrace);
        }

        private static LogLevel MapLevel(string severity, int? priority)
        {
            if (!string.IsNullOrEmpty(severity))
            {
                switch (severity.Trim().ToLowerInvariant())
                {
                    case "critical":
                    case "fatal":
                        return LogLevel.Critical;
                    case "error":
                        return LogLevel.Error;
                    case "warning":
                    case "warn":
                        return LogLevel.Warning;
                    case "debug":
                    case "trace":
                        return LogLevel.Debug;
                    default:
                        return LogLevel.Information;
                }
            }

            if (priority is int p)
            {
                if (p >= 4) return LogLevel.Error;
                if (p == 3) return LogLevel.Warning;
                if (p <= 1) return LogLevel.Debug;
            }

            return LogLevel.Information;
        }
    }
}
