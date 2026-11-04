using System;
using mooSQL.data;
using saiflow.hub.Essential;
using saiflow.hub.Logging;

namespace saiflow.hub.Essential
{
    public class MessageMediator
    {
        private readonly Func<SQLBuilder> _useRunSql;

        public MessageMediator(Func<SQLBuilder> useRunSql)
        {
            _useRunSql = useRunSql ?? throw new ArgumentNullException(nameof(useRunSql));
        }

        public void InvokeFromMessage(Func<MessageEntity, MessageConsumedResult> consumeMessageFunction,
            string topic,
            string line,
            string jobName)
        {
            try
            {
                var result = consumeMessageFunction?.Invoke(new MessageEntity(topic, line, jobName));
                if (result.Status == MessageConsumedStatus.Success)
                {
                    LogService.RecordInfoLog(_useRunSql, JobTypeEnum.Message,
                        jobName, topic, string.Empty, string.Empty, 1,
                        LogAdminDefine.ADMIN_MESSAGE_SUCCESS, line);
                }
                else
                {
                    LogService.RecordInfoLog(_useRunSql, JobTypeEnum.Message,
                        jobName, topic, string.Empty, string.Empty, -1,
                        result.Message, line);
                }
            }
            catch (Exception ex)
            {
                LogService.RecordErrorLog(_useRunSql, JobTypeEnum.Message,
                    jobName, topic, string.Empty, string.Empty, -1,
                    string.Format("Exception Message:{0}", ex.Message),
                    ex.StackTrace,
                    ex.InnerException != null ? ex.InnerException.StackTrace : null,
                    line);
                throw;
            }
        }
    }
}
