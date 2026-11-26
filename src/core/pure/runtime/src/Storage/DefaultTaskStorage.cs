using System;
using mooSQL.data;
using sailflow.data;
using sailflow.storage;

namespace sailflow.runtime.storage;

/// <summary>默认 <see cref="ITaskStorage"/>。</summary>
public class DefaultTaskStorage : ITaskStorage
{
    public virtual TaskEntity GetById(SQLBuilder kit, string taskId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(taskId))
            return null!;
        return kit.findRowById<TaskEntity>(taskId.Trim());
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
}
