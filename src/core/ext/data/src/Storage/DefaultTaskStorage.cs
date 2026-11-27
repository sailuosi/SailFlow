using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using sailflow.data;
using sailflow.storage;
using sailflow.utils;

namespace sailflow.data.Storage;

/// <summary>默认 <see cref="ITaskStorage"/>。</summary>
public class DefaultTaskStorage : ITaskStorage
{
    static readonly short?[] OpenStates =
    {
        (short)TaskStateEnum.Waiting,
        (short)TaskStateEnum.Reading
    };

    public virtual TaskEntity GetById(SQLBuilder kit, string taskId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(taskId))
            return null!;
        return kit.findRowById<TaskEntity>(taskId.Trim());
    }

    public virtual string Insert(SQLBuilder kit, TaskEntity entity)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        var newTaskId = kit.useRepo<TaskEntity, string>().InsertReturnId(entity);
        entity.TaskId = newTaskId;
        return newTaskId;
    }

    public virtual void Update(SQLBuilder kit, TaskEntity entity)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        kit.update(entity);
    }

    public virtual void DeleteById(SQLBuilder kit, string taskId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(taskId))
            return;
        kit.useRepo<TaskEntity, string>().DeleteById(taskId.Trim());
    }

    public virtual void DeleteByProcessInstance(SQLBuilder kit, string processInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(processInstanceId))
            return;
        var clip = kit.useClip();
        clip.setTable<TaskEntity>(out var t)
            .where(() => t.ProcessInstanceId, processInstanceId.Trim())
            .doDelete();
    }

    public virtual IList<TaskEntity> ListByProcessInstance(SQLBuilder kit, string processInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(processInstanceId))
            return new List<TaskEntity>();
        var clip = kit.useClip();
        clip.from<TaskEntity>(out var t)
            .where(() => t.ProcessInstanceId, processInstanceId.Trim());
        return clip.select(t).queryList()?.ToList() ?? new List<TaskEntity>();
    }

    public virtual int UpdateState(
        SQLBuilder kit,
        string taskId,
        short newState,
        string updatedUserId = null,
        string updatedUserName = null)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(taskId))
            return 0;

        var clip = kit.useClip();
        clip.setTable<TaskEntity>(out var t)
            .set(() => t.TaskState, newState)
            .set(() => t.UpdatedDateTime, DateTime.UtcNow)
            .where(() => t.TaskId, taskId.Trim());

        if (!string.IsNullOrWhiteSpace(updatedUserId))
            clip.set(() => t.UpdatedUserId, updatedUserId);
        if (!string.IsNullOrWhiteSpace(updatedUserName))
            clip.set(() => t.UpdatedUserName, updatedUserName);

        return clip.doUpdate();
    }

    public virtual int EndState(
        SQLBuilder kit,
        string taskId,
        short newState,
        DateTime completedAt,
        string endedUserId = null,
        string endedUserName = null,
        string endType = null,
        string msg = null)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(taskId))
            return 0;

        var clip = kit.useClip();
        clip.setTable<TaskEntity>(out var t)
            .set(() => t.TaskState, newState)
            .set(() => t.CompletedAt, completedAt)
            .set(() => t.UpdatedDateTime, DateTime.UtcNow)
            .where(() => t.TaskId, taskId.Trim());

        if (!string.IsNullOrWhiteSpace(endedUserId))
            clip.set(() => t.EndedUserId, endedUserId);
        if (!string.IsNullOrWhiteSpace(endedUserName))
            clip.set(() => t.EndedUserName, endedUserName);
        if (!string.IsNullOrWhiteSpace(endType))
            clip.set(() => t.EndType, endType);
        if (!string.IsNullOrWhiteSpace(msg))
            clip.set(() => t.Msg, msg);
        if (!string.IsNullOrWhiteSpace(endedUserId))
            clip.set(() => t.UpdatedUserId, endedUserId);
        if (!string.IsNullOrWhiteSpace(endedUserName))
            clip.set(() => t.UpdatedUserName, endedUserName);

        return clip.doUpdate();
    }

    public virtual int CancelByActivityInstance(SQLBuilder kit, string activityInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(activityInstanceId))
            return 0;

        var clip = kit.useClip();
        clip.setTable<TaskEntity>(out var t)
            .set(() => t.TaskState, (short)48)
            .set(() => t.RecordStatusInvalid, (byte)1)
            .where(() => t.ActivityInstanceId, activityInstanceId.Trim());
        return clip.doUpdate();
    }

    public virtual IList<TaskEntity> ListOpenByProcessInstance(SQLBuilder kit, string processInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(processInstanceId))
            return new List<TaskEntity>();

        var clip = kit.useClip();
        clip.from<TaskEntity>(out var ot)
            .where(() => ot.ProcessInstanceId, processInstanceId.Trim())
            .whereIn(() => ot.TaskState, OpenStates);
        return clip.select(ot).queryList()?.ToList() ?? new List<TaskEntity>();
    }

    public virtual int CancelOpenByProcessInstance(
        SQLBuilder kit,
        string processInstanceId,
        DateTime completedAt,
        string endedUserId = null,
        string endedUserName = null)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(processInstanceId))
            return 0;

        var clip = kit.useClip();
        clip.setTable<TaskEntity>(out var t)
            .set(() => t.TaskState, (short)TaskStateEnum.Canceled)
            .set(() => t.RecordStatusInvalid, (byte)1)
            .set(() => t.CompletedAt, completedAt)
            .where(() => t.ProcessInstanceId, processInstanceId.Trim())
            .whereIn(() => t.TaskState, OpenStates);

        if (!string.IsNullOrWhiteSpace(endedUserId))
            clip.set(() => t.EndedUserId, endedUserId);
        if (!string.IsNullOrWhiteSpace(endedUserName))
            clip.set(() => t.EndedUserName, endedUserName);

        return clip.doUpdate();
    }

    public virtual TaskViewEntity GetViewById(SQLBuilder kit, string taskId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(taskId))
            return null!;
        return kit.findRowById<TaskViewEntity>(taskId.Trim());
    }

    public virtual async Task<TaskViewEntity> GetViewByIdAsync(
        SQLBuilder kit,
        string taskId,
        CancellationToken cancellationToken = default)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(taskId))
            return null!;

        var clip = kit.useClip();
        clip.from<TaskViewEntity>(out var t)
            .where(() => t.TaskId, taskId.Trim());
        var list = await clip.select(t).take(1).queryListAsync(cancellationToken).ConfigureAwait(false);
        return list?.FirstOrDefault();
    }

    public virtual IList<TaskEntity> ListByActivity(
        SQLBuilder kit,
        string processInstanceId,
        string activityInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<TaskEntity>(out var t)
            .where(() => t.ActivityInstanceId, activityInstanceId)
            .where(() => t.ProcessInstanceId, processInstanceId);
        return clip.select(t).queryList()?.ToList() ?? new List<TaskEntity>();
    }

    public virtual IList<TaskEntity> ListByActivityIds(
        SQLBuilder kit,
        string processInstanceId,
        IReadOnlyList<string> activityInstanceIds)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (activityInstanceIds == null || activityInstanceIds.Count == 0
            || string.IsNullOrWhiteSpace(processInstanceId))
            return new List<TaskEntity>();

        var ids = activityInstanceIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (ids.Count == 0)
            return new List<TaskEntity>();

        var clip = kit.useClip();
        clip.from<TaskEntity>(out var t)
            .where(() => t.ProcessInstanceId, processInstanceId)
            .whereIn(() => t.ActivityInstanceId, ids);
        return clip.select(t).queryList()?.ToList() ?? new List<TaskEntity>();
    }

    public virtual IList<TaskViewEntity> ListViewsByActivity(
        SQLBuilder kit,
        string processInstanceId,
        string activityInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<TaskViewEntity>(out var t)
            .where(() => t.ActivityInstanceId, activityInstanceId)
            .where(() => t.ProcessInstanceId, processInstanceId);
        return clip.select(t).queryList()?.ToList() ?? new List<TaskViewEntity>();
    }

    public virtual async Task<IList<TaskViewEntity>> ListViewsByActivityAsync(
        SQLBuilder kit,
        string processInstanceId,
        string activityInstanceId,
        CancellationToken cancellationToken = default)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        cancellationToken.ThrowIfCancellationRequested();
        var clip = kit.useClip();
        clip.from<TaskViewEntity>(out var t)
            .where(() => t.ActivityInstanceId, activityInstanceId)
            .where(() => t.ProcessInstanceId, processInstanceId);
        var list = await clip.select(t).queryListAsync(cancellationToken).ConfigureAwait(false);
        return list?.ToList() ?? new List<TaskViewEntity>();
    }

    public virtual IList<TaskViewEntity> ListMineViewsByActivityInstance(
        SQLBuilder kit,
        string activityInstanceId,
        string userId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<TaskViewEntity>(out var t)
            .where(() => t.ActivityInstanceId, activityInstanceId)
            .where(() => t.SignId, userId)
            .where(() => t.ProcessState, (short)2)
            .whereIn(() => t.ActivityType, new short?[] { (short)4, (short)5, (short)6 })
            .whereIn(() => t.ActivityState, new short?[] { (short)1, (short)2 })
            .orderByDesc(() => t.ActiveAt)
            .top(1);
        return clip.select(t).queryList()?.ToList() ?? new List<TaskViewEntity>();
    }

    public virtual async Task<IList<TaskViewEntity>> ListMineViewsByActivityInstanceAsync(
        SQLBuilder kit,
        string activityInstanceId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        cancellationToken.ThrowIfCancellationRequested();
        var clip = kit.useClip();
        clip.from<TaskViewEntity>(out var t)
            .where(() => t.ActivityInstanceId, activityInstanceId)
            .where(() => t.SignId, userId)
            .where(() => t.ProcessState, (short)2)
            .whereIn(() => t.ActivityType, new short?[] { (short)4, (short)5, (short)6 })
            .whereIn(() => t.ActivityState, new short?[] { (short)1, (short)2 })
            .orderByDesc(() => t.ActiveAt)
            .top(1);
        var list = await clip.select(t).queryListAsync(cancellationToken).ConfigureAwait(false);
        return list?.ToList() ?? new List<TaskViewEntity>();
    }

    public virtual IList<TaskViewEntity> ListMineViewsByBizProcess(
        SQLBuilder kit,
        string bizPK,
        string processId,
        string userId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<TaskViewEntity>(out var t)
            .where(() => t.BizPK, bizPK)
            .where(() => t.ProcessId, processId)
            .where(() => t.SignId, userId)
            .where(() => t.ProcessState, (short)2)
            .sinkOR()
            .whereIn(() => t.ActivityType, new short?[] { (short)4, (short)5, (short)6 })
            .where(() => t.WorkItemType, (short)1)
            .rise()
            .whereIn(() => t.ActivityState, new short?[] { (short)1, (short)2 })
            .orderByDesc(() => t.ActiveAt)
            .top(1);
        return clip.select(t).queryList()?.ToList() ?? new List<TaskViewEntity>();
    }

    public virtual IList<TaskEntity> ListOpenByConsultSrc(SQLBuilder kit, string consultSrcTaskId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(consultSrcTaskId))
            return new List<TaskEntity>();

        var clip = kit.useClip();
        clip.from<TaskEntity>(out var t)
            .where(() => t.ConsultSrcTaskId, consultSrcTaskId)
            .whereIn(() => t.TaskState, OpenStates);
        return clip.select(t).queryList()?.ToList() ?? new List<TaskEntity>();
    }

    public virtual TaskEntity GetByEntrustedTaskId(SQLBuilder kit, string entrustedTaskId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(entrustedTaskId))
            return null!;

        var clip = kit.useClip();
        clip.from<TaskEntity>(out var t)
            .where(() => t.EntrustedTaskId, entrustedTaskId);
        return clip.select(t).queryList()?.FirstOrDefault();
    }
}
