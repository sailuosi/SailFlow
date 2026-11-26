using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using sailflow.data;
using sailflow.storage;
using sailflow.utils;

namespace sailflow.runtime.storage;

/// <summary>默认 <see cref="ITransitionInstanceStorage"/>。</summary>
public class DefaultTransitionInstanceStorage : ITransitionInstanceStorage
{
    public virtual string Insert(SQLBuilder kit, TrackEntity entity, string physicalTable = null)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var newId = kit.insertReturnId<TrackEntity, string>(entity, physicalTable);
        entity.TrackId = newId;
        return newId;
    }

    public virtual void DeleteByProcessInstance(SQLBuilder kit, string physicalTable, string processInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(processInstanceId))
            return;
        try
        {
            var clip = kit.useClip();
            clip.setTable<TrackEntity>(out var t)
                .useSQL(b => b.setTable(physicalTable))
                .where(() => t.ProcessInstanceId, processInstanceId.Trim())
                .doDelete();
        }
        catch
        {
            // 分表未创建时忽略
        }
    }

    public virtual IList<TrackEntity> ListWorkItemByBiz(
        SQLBuilder kit,
        string physicalTable,
        string bizPK,
        string processId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<TrackEntity>(physicalTable, out var t)
            .InnerJoin<ActivityInstanceEntity>(out var a).on(() => t.ToActInstanceId == a.ActivityInstanceId)
            .where(() => t.BizPK, bizPK)
            .where(() => t.ProcessId, processId)
            .sinkOR()
            .whereIn(() => t.ToActType, new short?[]
            {
                (short)ActivityTypeEnum.TaskNode,
                (short)ActivityTypeEnum.SubProcessNode,
                (short)ActivityTypeEnum.MultiSignNode
            })
            .where(() => a.WorkItemType, (short)1)
            .rise()
            .orderByDesc(() => t.CreatedDateTime);
        return clip.select(t).queryList()?.ToList() ?? new List<TrackEntity>();
    }

    public virtual IList<TrackEntity> ListByBizAndToActType(
        SQLBuilder kit,
        string physicalTable,
        string bizPK,
        string processId,
        short toActivityType)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<TrackEntity>(physicalTable, out var t)
            .where(() => t.BizPK, bizPK)
            .where(() => t.ProcessId, processId)
            .where(() => t.ToActType, toActivityType)
            .orderByDesc(() => t.CreatedDateTime);
        return clip.select(t).queryList()?.ToList() ?? new List<TrackEntity>();
    }

    public virtual IList<TrackEntity> ListByProcessInstance(
        SQLBuilder kit,
        string physicalTable,
        string bizPK,
        string processId,
        string processInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<TrackEntity>(physicalTable, out var t)
            .where(() => t.BizPK, bizPK)
            .where(() => t.ProcessId, processId)
            .where(() => t.ProcessInstanceId, processInstanceId)
            .orderByDesc(() => t.CreatedDateTime);
        return clip.select(t).queryList()?.ToList() ?? new List<TrackEntity>();
    }

    public virtual IList<TrackEntity> ListByFromActivityInstance(
        SQLBuilder kit,
        string physicalTable,
        string fromActivityInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<TrackEntity>(physicalTable, out var t)
            .where(() => t.FromActInstanceId, fromActivityInstanceId);
        return clip.select(t).queryList()?.ToList() ?? new List<TrackEntity>();
    }
}
