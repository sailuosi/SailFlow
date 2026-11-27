using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using sailflow.data;
using sailflow.storage;

namespace sailflow.data.Storage;

/// <summary>默认 <see cref="IJobInfoStorage"/>。</summary>
public class DefaultJobInfoStorage : IJobInfoStorage
{
    public virtual IList<JobInfoEntity> ListSubscribedByTopic(SQLBuilder kit, string topic)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<JobInfoEntity>(out var j)
            .where(() => j.Topic, topic)
            .where(() => j.JobStatus, "Subscribed")
            .orderByDesc(() => j.JobInfoId);
        return clip.select(j).queryList()?.ToList() ?? new List<JobInfoEntity>();
    }
}
