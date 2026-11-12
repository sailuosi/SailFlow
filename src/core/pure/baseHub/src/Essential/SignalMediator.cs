using System;
using System.Collections.Generic;
using mooSQL.data;
using sailflow.hub.Essential;
using sailflow.hub.Logging;

namespace sailflow.hub.Essential
{
    public class SignalMediator
    {
        private readonly Func<SQLBuilder> _useRunSql;

        public SignalMediator(Func<SQLBuilder> useRunSql)
        {
            _useRunSql = useRunSql ?? throw new ArgumentNullException(nameof(useRunSql));
        }

        public void InvokeFromSignal(Func<SignalEntity, List<SignalConsumedResult>> consumeSignalFunction,
            string topic,
            string line,
            string jobName)
        {
            try
            {
                var resultList = consumeSignalFunction?.Invoke(new SignalEntity(topic, line, jobName));
                foreach (var result in resultList)
                {
                    if (result.Status == SignalConsumedStatus.Success)
                    {
                        LogService.RecordInfoLog(_useRunSql, JobTypeEnum.Signal,
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
