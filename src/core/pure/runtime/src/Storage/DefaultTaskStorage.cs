using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using sailflow.data;
using sailflow.storage;
using sailflow.utils;

namespace sailflow.runtime.storage;

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
}
