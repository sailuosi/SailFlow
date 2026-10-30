using System;
using System.Threading;
using mooSQL.data;
using saiflow.data;

namespace saiflow.hub.Logging
{
    /// <summary>
    /// Log Service
    /// 日志服务类
    /// </summary>
    public class LogService : ILogService
    {
        #region Add Record 
        /// <summary>
        /// 插入方法
        /// </summary>
        /// <param name="entity">实体对象</param>
        public void Record(object entity)
        {
            var log = (JobLogEntity)entity;
            using (var kit = SailSqlAccess.UseRunSQL())
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
        #endregion

        #region Static Method
        /// <summary>
        /// Record error log
        /// 作业日志记录
        /// </summary>
        public static void RecordErrorLog(JobTypeEnum jobType,
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
                CreatedDateTime = System.DateTime.UtcNow,
                CreatedUserId = LogAdminDefine.ADMIN_USER_ID,
                CreatedUserName = LogAdminDefine.ADMIN_USER_NAME
            };

            LogService ls = new LogService();
            ThreadPool.QueueUserWorkItem(new WaitCallback(ls.Record), entity);
        }

        /// <summary>
        /// Record info log
        /// </summary>
        public static void RecordInfoLog(JobTypeEnum jobType,
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
                CreatedDateTime = System.DateTime.UtcNow,
                CreatedUserId = LogAdminDefine.ADMIN_USER_ID,
                CreatedUserName = LogAdminDefine.ADMIN_USER_NAME
            };

            LogService ls = new LogService();
            ThreadPool.QueueUserWorkItem(new WaitCallback(ls.Record), entity);
        }
        #endregion
    }
}
