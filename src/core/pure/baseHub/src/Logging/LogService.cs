using System;
using System.Threading;
using mooSQL.data;
using saiflow.data;

namespace saiflow.hub.Logging
{
    /// <summary>
    /// 作业日志服务（baseHub 无引擎引用）。
    /// 默认可经 <see cref="Func{SQLBuilder}"/> 写表；也可注入 <see cref="Action{JobLogEntity}"/> 对接引擎 <c>IEngineLogSink</c>。
    /// </summary>
    public class LogService : ILogService
    {
        private readonly Func<SQLBuilder> _useRunSql;
        private readonly Action<JobLogEntity> _writer;

        public LogService(Func<SQLBuilder> useRunSql)
        {
            _useRunSql = useRunSql ?? throw new ArgumentNullException(nameof(useRunSql));
        }

        /// <summary>自定义写入（例如转发到引擎 LogSink.WriteJob）。</summary>
        public LogService(Action<JobLogEntity> writer)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public void Record(object entity)
        {
            var log = (JobLogEntity)entity;
            if (_writer != null)
            {
                _writer(log);
                return;
            }

            using (var kit = _useRunSql())
            {
                kit.beginTransaction();
                try
                {
                    kit.useRepo<JobLogEntity>().Insert(log);
                    kit.commit();
                }
                catch
                {
                    throw;
                }
            }
        }

        public static void RecordErrorLog(Func<SQLBuilder> useRunSql,
            JobTypeEnum jobType,
            string jobName,
            string jobKey,
            string refClass,
            string refIDs,
            short status,
            string message,
            string stackTrace,
            string innerStackTrace,
            string requestData)
        {
            Enqueue(useRunSql, null, BuildEntity(
                jobType, jobName, jobKey, refClass, refIDs, status, message,
                stackTrace, innerStackTrace, requestData));
        }

        public static void RecordInfoLog(Func<SQLBuilder> useRunSql,
            JobTypeEnum jobType,
            string jobName,
            string jobKey,
            string refClass,
            string refIDs,
            short status,
            string message,
            string requestData)
        {
            Enqueue(useRunSql, null, BuildEntity(
                jobType, jobName, jobKey, refClass, refIDs, status, message,
                null, null, requestData));
        }

        /// <summary>经自定义 writer 异步记录（对接引擎 Sink）。</summary>
        public static void RecordErrorLog(Action<JobLogEntity> writer,
            JobTypeEnum jobType,
            string jobName,
            string jobKey,
            string refClass,
            string refIDs,
            short status,
            string message,
            string stackTrace,
            string innerStackTrace,
            string requestData)
        {
            Enqueue(null, writer, BuildEntity(
                jobType, jobName, jobKey, refClass, refIDs, status, message,
                stackTrace, innerStackTrace, requestData));
        }

        public static void RecordInfoLog(Action<JobLogEntity> writer,
            JobTypeEnum jobType,
            string jobName,
            string jobKey,
            string refClass,
            string refIDs,
            short status,
            string message,
            string requestData)
        {
            Enqueue(null, writer, BuildEntity(
                jobType, jobName, jobKey, refClass, refIDs, status, message,
                null, null, requestData));
        }

        private static JobLogEntity BuildEntity(
            JobTypeEnum jobType,
            string jobName,
            string jobKey,
            string refClass,
            string refIDs,
            short status,
            string message,
            string stackTrace,
            string innerStackTrace,
            string requestData)
        {
            return new JobLogEntity
            {
                JobType = jobType.ToString(),
                JobName = jobName,
                JobKey = jobKey,
                RefClass = refClass,
                RefIDs = refIDs,
                Status = status,
                Message = message,
                StackTrace = stackTrace,
                InnerStackTrace = innerStackTrace,
                RequestData = requestData,
                CreatedDateTime = DateTime.UtcNow,
                CreatedUserId = LogAdminDefine.ADMIN_USER_ID,
                CreatedUserName = LogAdminDefine.ADMIN_USER_NAME
            };
        }

        private static void Enqueue(Func<SQLBuilder> useRunSql, Action<JobLogEntity> writer, JobLogEntity entity)
        {
            LogService ls = writer != null ? new LogService(writer) : new LogService(useRunSql);
            ThreadPool.QueueUserWorkItem(ls.Record, entity);
        }
    }
}
