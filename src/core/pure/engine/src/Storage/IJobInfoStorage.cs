using System.Collections.Generic;
using mooSQL.data;
using sailflow.data;

namespace sailflow.storage
{
    /// <summary>作业信息表纯库（kit 首参）。</summary>
    public interface IJobInfoStorage
    {
        IList<JobInfoEntity> ListSubscribedByTopic(SQLBuilder kit, string topic);
    }
}
