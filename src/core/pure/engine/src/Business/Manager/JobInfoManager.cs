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
            return kit
                .select("*")
                .from("wf_job_info")
                .where("topic", topic)
                .where("job_status", "Subscribed")
                .orderBy("id DESC")
                .query<JobInfoEntity>()
                .ToList();
        }
    }
}
