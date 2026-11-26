using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using sailflow.data;
using sailflow.storage;
using sailflow.utils;

namespace sailflow.runtime.storage;

/// <summary>默认 <see cref="IActivityInstanceStorage"/>。</summary>
public class DefaultActivityInstanceStorage : IActivityInstanceStorage
{
    static readonly short[] DefaultOpenStates =
    {
        (short)ActivityStateEnum.Ready,
        (short)ActivityStateEnum.Running,
        (short)ActivityStateEnum.Suspended
    };

    public virtual ActivityInstanceEntity GetById(SQLBuilder kit, string activityInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(activityInstanceId))
            return null!;
        return kit.findRowById<ActivityInstanceEntity>(activityInstanceId.Trim());
    }

    public virtual int UpdateState(
        SQLBuilder kit,
        string activityInstanceId,
        short newState,
        string updatedUserId = null,
        string updatedUserName = null)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(activityInstanceId))
            return 0;

        var clip = kit.useClip();
        clip.setTable<ActivityInstanceEntity>(out var ai)
            .set(() => ai.ActivityState, newState)
            .set(() => ai.UpdatedDateTime, DateTime.UtcNow)
            .where(() => ai.ActivityInstanceId, activityInstanceId.Trim());

        if (!string.IsNullOrWhiteSpace(updatedUserId))
            clip.set(() => ai.UpdatedUserId, updatedUserId);
        if (!string.IsNullOrWhiteSpace(updatedUserName))
            clip.set(() => ai.UpdatedUserName, updatedUserName);

        return clip.doUpdate();
    }

    public virtual int EndState(
        SQLBuilder kit,
        string activityInstanceId,
        short newState,
        DateTime endedDateTime,
        string endedUserId = null,
        string endedUserName = null)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(activityInstanceId))
            return 0;

        var clip = kit.useClip();
        clip.setTable<ActivityInstanceEntity>(out var ai)
            .set(() => ai.ActivityState, newState)
            .set(() => ai.EndedDateTime, endedDateTime)
            .set(() => ai.UpdatedDateTime, DateTime.UtcNow)
            .where(() => ai.ActivityInstanceId, activityInstanceId.Trim());

        if (!string.IsNullOrWhiteSpace(endedUserId))
        {
            clip.set(() => ai.EndedUserId, endedUserId);
            clip.set(() => ai.UpdatedUserId, endedUserId);
        }

        if (!string.IsNullOrWhiteSpace(endedUserName))
        {
            clip.set(() => ai.EndedUserName, endedUserName);
            clip.set(() => ai.UpdatedUserName, endedUserName);
        }

        return clip.doUpdate();
    }

    public virtual int CancelOpen(
        SQLBuilder kit,
        string activityInstanceId,
        string updatedUserId = null,
        string updatedUserName = null)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(activityInstanceId))
            return 0;

        var clip = kit.useClip();
        clip.setTable<ActivityInstanceEntity>(out var ai)
            .set(() => ai.ActivityState, (short)ActivityStateEnum.Cancelled)
            .set(() => ai.UpdatedDateTime, DateTime.UtcNow)
            .where(() => ai.ActivityInstanceId, activityInstanceId.Trim())
            .whereIn(() => ai.ActivityState, DefaultOpenStates.Cast<short?>().ToArray());

        if (!string.IsNullOrWhiteSpace(updatedUserId))
            clip.set(() => ai.UpdatedUserId, updatedUserId);
        if (!string.IsNullOrWhiteSpace(updatedUserName))
            clip.set(() => ai.UpdatedUserName, updatedUserName);

        return clip.doUpdate();
    }

    public virtual int UpdateChildrenState(
        SQLBuilder kit,
        string mainActivityInstanceId,
        short newState,
        IReadOnlyList<short> fromStates,
        string updatedUserId = null,
        string updatedUserName = null)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(mainActivityInstanceId))
            return 0;

        var states = (fromStates == null || fromStates.Count == 0)
            ? DefaultOpenStates
            : fromStates.ToArray();

        var clip = kit.useClip();
        clip.setTable<ActivityInstanceEntity>(out var ai)
            .set(() => ai.ActivityState, newState)
            .set(() => ai.UpdatedDateTime, DateTime.UtcNow)
            .where(() => ai.MainActInstanceId, mainActivityInstanceId.Trim())
            .whereIn(() => ai.ActivityState, states.Select(s => (short?)s).ToArray());

        if (!string.IsNullOrWhiteSpace(updatedUserId))
            clip.set(() => ai.UpdatedUserId, updatedUserId);
        if (!string.IsNullOrWhiteSpace(updatedUserName))
            clip.set(() => ai.UpdatedUserName, updatedUserName);

        return clip.doUpdate();
    }
}
