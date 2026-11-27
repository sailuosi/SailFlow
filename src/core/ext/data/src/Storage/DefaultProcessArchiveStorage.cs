using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using sailflow.data;
using sailflow.storage;
using sailflow.utils;

namespace sailflow.data.Storage;

/// <summary>默认 <see cref="IProcessArchiveStorage"/>（冷表读写 + 归档候选）。</summary>
public class DefaultProcessArchiveStorage : IProcessArchiveStorage
{
    public virtual IList<ProcessInstanceEntity> ListArchivableCandidates(
        SQLBuilder kit,
        DateTime cutoffUtc,
        int limit)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ProcessInstanceEntity>(out var pi)
            .whereIn(() => pi.ProcessState, new short?[]
            {
                (short)ProcessStateEnum.Completed,
                (short)ProcessStateEnum.Canceled,
                (short)ProcessStateEnum.Discarded,
                (short)ProcessStateEnum.Terminated
            })
            .whereIsNotNull(() => pi.EndedDateTime)
            .where(() => pi.EndedDateTime, cutoffUtc, "<")
            .orderBy(() => pi.EndedDateTime);
        return clip.select(pi).queryList()?.Take(Math.Max(1, limit)).ToList()
               ?? new List<ProcessInstanceEntity>();
    }

    public virtual ProcessInstanceEntity GetProcessInstance(
        SQLBuilder kit,
        string physicalTable,
        string processInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        try
        {
            var clip = kit.useClip();
            clip.from<ProcessInstanceEntity>(physicalTable, out var row)
                .where(() => row.ProcessInstanceId, processInstanceId);
            return clip.select(row).queryList()?.FirstOrDefault();
        }
        catch
        {
            return null!;
        }
    }

    public virtual IList<TaskEntity> ListTasks(SQLBuilder kit, string physicalTable, string processInstanceId)
        => ListByPi<TaskEntity>(kit, physicalTable, processInstanceId, (clip, tb, pi) =>
        {
            clip.from<TaskEntity>(tb, out var row).where(() => row.ProcessInstanceId, pi);
            return clip.select(row).queryList()?.ToList() ?? new List<TaskEntity>();
        });

    public virtual IList<ActivityInstanceEntity> ListActivities(
        SQLBuilder kit,
        string physicalTable,
        string processInstanceId)
        => ListByPi<ActivityInstanceEntity>(kit, physicalTable, processInstanceId, (clip, tb, pi) =>
        {
            clip.from<ActivityInstanceEntity>(tb, out var row).where(() => row.ProcessInstanceId, pi);
            return clip.select(row).queryList()?.ToList() ?? new List<ActivityInstanceEntity>();
        });

    public virtual IList<TrackEntity> ListTransitions(
        SQLBuilder kit,
        string physicalTable,
        string processInstanceId)
        => ListByPi<TrackEntity>(kit, physicalTable, processInstanceId, (clip, tb, pi) =>
        {
            clip.from<TrackEntity>(tb, out var row).where(() => row.ProcessInstanceId, pi);
            return clip.select(row).queryList()?.ToList() ?? new List<TrackEntity>();
        });

    public virtual IList<ProcessVariableEntity> ListVariables(
        SQLBuilder kit,
        string physicalTable,
        string processInstanceId)
        => ListByPi<ProcessVariableEntity>(kit, physicalTable, processInstanceId, (clip, tb, pi) =>
        {
            clip.from<ProcessVariableEntity>(tb, out var row).where(() => row.ProcessInstanceId, pi);
            return clip.select(row).queryList()?.ToList() ?? new List<ProcessVariableEntity>();
        });

    public virtual bool ExistsProcessInstance(SQLBuilder kit, string physicalTable, string id)
        => Exists(kit, physicalTable, id, (clip, tb, key) =>
        {
            clip.from<ProcessInstanceEntity>(tb, out var row).where(() => row.ProcessInstanceId, key);
            return clip.select(row).queryList().Any();
        });

    public virtual bool ExistsTask(SQLBuilder kit, string physicalTable, string id)
        => Exists(kit, physicalTable, id, (clip, tb, key) =>
        {
            clip.from<TaskEntity>(tb, out var row).where(() => row.TaskId, key);
            return clip.select(row).queryList().Any();
        });

    public virtual bool ExistsActivityInstance(SQLBuilder kit, string physicalTable, string id)
        => Exists(kit, physicalTable, id, (clip, tb, key) =>
        {
            clip.from<ActivityInstanceEntity>(tb, out var row).where(() => row.ActivityInstanceId, key);
            return clip.select(row).queryList().Any();
        });

    public virtual bool ExistsTransition(SQLBuilder kit, string physicalTable, string id)
        => Exists(kit, physicalTable, id, (clip, tb, key) =>
        {
            clip.from<TrackEntity>(tb, out var row).where(() => row.TrackId, key);
            return clip.select(row).queryList().Any();
        });

    public virtual bool ExistsProcessVariable(SQLBuilder kit, string physicalTable, string id)
        => Exists(kit, physicalTable, id, (clip, tb, key) =>
        {
            clip.from<ProcessVariableEntity>(tb, out var row).where(() => row.ProcessVariableId, key);
            return clip.select(row).queryList().Any();
        });

    public virtual void InsertPreservingKeyProcessInstance(
        SQLBuilder kit,
        string physicalTable,
        ProcessInstanceEntity entity)
        => InsertPreservingKey(kit, physicalTable, entity);

    public virtual void InsertPreservingKeyTask(SQLBuilder kit, string physicalTable, TaskEntity entity)
        => InsertPreservingKey(kit, physicalTable, entity);

    public virtual void InsertPreservingKeyActivityInstance(
        SQLBuilder kit,
        string physicalTable,
        ActivityInstanceEntity entity)
        => InsertPreservingKey(kit, physicalTable, entity);

    public virtual void InsertPreservingKeyTransition(
        SQLBuilder kit,
        string physicalTable,
        TrackEntity entity)
        => InsertPreservingKey(kit, physicalTable, entity);

    public virtual void InsertPreservingKeyProcessVariable(
        SQLBuilder kit,
        string physicalTable,
        ProcessVariableEntity entity)
        => InsertPreservingKey(kit, physicalTable, entity);

    static void InsertPreservingKey<T>(SQLBuilder kit, string physicalTable, T entity) where T : class
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var maps = SailEntityMapper.GetColumnMaps<T>();
        kit.setTable(physicalTable).newRow();
        foreach (var map in maps)
        {
            var val = map.GetValue(entity);
            if (val == null)
                continue;
            if (map.IsKey && IsDefaultKey(val))
                continue;
            kit.set(map.ColumnName, val);
        }
        kit.doInsert();
    }

    static bool Exists(
        SQLBuilder kit,
        string physicalTable,
        string id,
        Func<SQLClip, string, string, bool> query)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;
        try
        {
            return query(kit.useClip(), physicalTable, id);
        }
        catch
        {
            return false;
        }
    }

    static IList<T> ListByPi<T>(
        SQLBuilder kit,
        string physicalTable,
        string processInstanceId,
        Func<SQLClip, string, string, IList<T>> query) where T : class, new()
    {
        try
        {
            return query(kit.useClip(), physicalTable, processInstanceId);
        }
        catch
        {
            return new List<T>();
        }
    }

    static bool IsDefaultKey(object val)
    {
        if (val == null)
            return true;
        if (val is int i)
            return i == 0;
        if (val is long l)
            return l == 0L;
        if (val is short s)
            return s == 0;
        if (val is string str)
            return string.IsNullOrWhiteSpace(str);
        return false;
    }
}
