using System;
using System.Threading;
using sailflow.engine.Entity;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.engine.Logging;

namespace sailflow.engine.Manager
{
    /// <summary>
    /// 引擎日志门面：组装 <see cref="LogEntity"/> 后交给 <see cref="IEngineLogSink"/>（默认写表）。
    /// </summary>
    public class LogManager
    {
        public LogManager(SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        public void Record(
            string title,
            LogEventType eventType,
            LogPriority priority,
            object extraObject,
            Exception e)
        {
            try
            {
                var log = new LogEntity
                {
                    EventTypeId = (int)eventType,
                    Priority = (int)priority,
                    Severity = priority.ToString().ToUpper(),
                    Title = title,
                    Timestamp = DateTime.UtcNow,
                    Message = e?.Message
                };
                if (e?.StackTrace != null)
                {
                    log.StackTrace = e.StackTrace.Length > 4000
                        ? e.StackTrace.Substring(0, 4000)
                        : e.StackTrace;
                }

                if (e?.InnerException?.StackTrace != null)
                {
                    log.InnerStackTrace = e.InnerException.StackTrace.Length > 4000
                        ? e.InnerException.StackTrace.Substring(0, 4000)
                        : e.InnerException.StackTrace;
                }

                if (extraObject != null)
                {
                    var jsonData = Sail.JsonSerializer.Serialize(extraObject);
                    log.RequestData = jsonData.Length > 2000 ? jsonData.Substring(0, 2000) : jsonData;
                }

                var client = Sail;
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        client.GetLogSink().Write(log);
                    }
                    catch
                    {
                        // 记日志失败不抛
                    }
                });
            }
            catch
            {
                // 记日志失败不抛
            }
        }

        public static void RecordLog(
            SailClient client,
            string title,
            LogEventType eventType,
            LogPriority priority,
            object extraObject,
            Exception e)
        {
            if (client?.ManagerFactory?.Logs == null)
                return;
            client.ManagerFactory.Logs.Record(title, eventType, priority, extraObject, e);
        }
    }
}
