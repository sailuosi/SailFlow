using System;
using System.Threading;
using mooSQL.data;
using saiflow.data;

namespace saiflow.hub.Logging
{
    /// <summary>
    /// Log Service — takes a SQL factory so baseHub stays free of engine references.
    /// </summary>
    public class LogService : ILogService
    {
        private readonly Func<SQLBuilder> _useRunSql;

        public LogService(Func<SQLBuilder> useRunSql)
        {
            _useRunSql = useRunSql ?? throw new ArgumentNullException(nameof(useRunSql));
        }

        public void Record(object entity)
        {
            var log = (JobLogEntity)entity;
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
            var entity = new JobLogEntity
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

            var ls = new LogService(useRunSql);
            ThreadPool.QueueUserWorkItem(ls.Record, entity);
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
            var entity = new JobLogEntity
            {
                JobType = jobType.ToString(),
                JobName = jobName,
                JobKey = jobKey,
                RefClass = refClass,
                RefIDs = refIDs,
                Status = status,
                Message = message,
                RequestData = requestData,
                CreatedDateTime = DateTime.UtcNow,
                CreatedUserId = LogAdminDefine.ADMIN_USER_ID,
                CreatedUserName = LogAdminDefine.ADMIN_USER_NAME
            };

            var ls = new LogService(useRunSql);
            ThreadPool.QueueUserWorkItem(ls.Record, entity);
        }
    }
}
