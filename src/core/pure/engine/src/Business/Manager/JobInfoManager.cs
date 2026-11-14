using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using sailflow.data;
using sailflow.engine.Entity;
using sailflow.engine.Client;

namespace sailflow.engine.Manager
{
    /// <summary>
    /// 作业数据。终态：Client/kit，无 Session/SailDbAccess。
    /// </summary>
    public class JobInfoManager
    {
        public JobInfoManager(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }
        public List<JobInfoEntity> GetJobSubscribedByTopic(string topic)
        {
            var kit = Sail.UseRunSQL();
            return GetJobSubscribedByTopic(kit, topic);
        }

        public List<JobInfoEntity> GetJobSubscribedByTopic(SQLBuilder kit, string topic)
        {
            var clip = kit.useClip();
            clip.from<JobInfoEntity>(out var j)
                .where(() => j.Topic, topic)
                .where(() => j.JobStatus, "Subscribed")
                .orderByDesc(() => j.JobInfoId);
            return clip.select(j).queryList().ToList();
        }
    }
}
