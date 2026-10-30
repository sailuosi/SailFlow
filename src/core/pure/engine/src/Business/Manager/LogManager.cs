using System;
using System.Threading;
using Newtonsoft.Json;
using mooSQL.data;
using saiflow.data;
using saiflow.engine.Common;
using saiflow.engine.Business.Entity;
using saiflow.engine.Client;

namespace saiflow.engine.Business.Manager
{
    /// <summary>
    /// 日志记录。终态：Client/kit，无 Session/conn。
    /// </summary>
    public class LogManager
    {
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
                    Message = e.Message
                };
                if (e.StackTrace != null)
                {
                    log.StackTrace = e.StackTrace.Length > 4000
                        ? e.StackTrace.Substring(0, 4000)
                        : e.StackTrace;
                }

                if (e.InnerException?.StackTrace != null)
                {
                    log.InnerStackTrace = e.InnerException.StackTrace.Length > 4000
                        ? e.InnerException.StackTrace.Substring(0, 4000)
                        : e.InnerException.StackTrace;
                }

                if (extraObject != null)
                {
                    var jsonData = JsonConvert.SerializeObject(extraObject);
                    log.RequestData = jsonData.Length > 2000 ? jsonData.Substring(0, 2000) : jsonData;
                }

                ThreadPool.QueueUserWorkItem(Insert, log);
            }
            catch
            {
                // 记日志失败不抛
            }
        }

        private static void Insert(object state)
        {
            var log = (LogEntity)state;
            using (var kit = SailClientAccess.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    Insert(kit, log);
                    kit.commit();
                }
                catch
                {
                    throw;
                }
            }
        }

        public static void Insert(SQLBuilder kit, LogEntity log)
            => kit.useRepo<LogEntity>().Insert(log);

        public static void RecordLog(
            string title,
            LogEventType eventType,
            LogPriority priority,
            object extraObject,
            Exception e)
        {
            new LogManager().Record(title, eventType, priority, extraObject, e);
        }
    }
}
