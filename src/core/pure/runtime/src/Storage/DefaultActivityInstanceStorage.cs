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

    public virtual string Insert(SQLBuilder kit, ActivityInstanceEntity entity)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        var newId = kit.useRepo<ActivityInstanceEntity, string>().InsertReturnId(entity);
        entity.ActivityInstanceId = newId;
        return newId;
    }

    public virtual void Update(SQLBuilder kit, ActivityInstanceEntity entity)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        kit.useRepo<ActivityInstanceEntity>().Update(entity);
    }

    public virtual void DeleteById(SQLBuilder kit, string activityInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(activityInstanceId))
            return;
        kit.useRepo<ActivityInstanceEntity, string>().DeleteById(activityInstanceId.Trim());
    }

    public virtual void DeleteByProcessInstance(SQLBuilder kit, string processInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(processInstanceId))
            return;
        var clip = kit.useClip();
        clip.setTable<ActivityInstanceEntity>(out var ai)
            .where(() => ai.ProcessInstanceId, processInstanceId.Trim())
            .doDelete();
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

    public virtual int CasIncreaseTokensHad(
        SQLBuilder kit,
        string activityInstanceId,
        int? previousTokensHad,
        int nextTokensHad)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(activityInstanceId))
            return 0;

        var clip = kit.useClip();
        clip.setTable<ActivityInstanceEntity>(out var ai)
            .set(() => ai.TokensHad, nextTokensHad)
            .set(() => ai.UpdatedDateTime, DateTime.UtcNow)
            .where(() => ai.ActivityInstanceId, activityInstanceId.Trim());
        if (previousTokensHad == null)
            clip.where(() => ai.TokensHad == null);
        else
            clip.where(() => ai.TokensHad, previousTokensHad);
        return clip.doUpdate();
    }

    public virtual int SetBlockedCanNotRenew(
        SQLBuilder kit,
        string processInstanceId,
        IReadOnlyList<string> activityIds)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(processInstanceId) || activityIds == null || activityIds.Count == 0)
            return 0;

        var clip = kit.useClip();
        clip.setTable<ActivityInstanceEntity>(out var ai)
            .set(() => ai.CanNotRenewInstance, (byte)1)
            .where(() => ai.ProcessInstanceId, processInstanceId.Trim())
            .whereIn(() => ai.ActivityState, DefaultOpenStates.Cast<short?>().ToArray())
            .whereIn(() => ai.ActivityId, activityIds.ToList());
        return clip.doUpdate();
    }

    public virtual int TerminateOpenByProcessInstance(
        SQLBuilder kit,
        string processInstanceId,
        DateTime endedDateTime,
        string endedUserId = null,
        string endedUserName = null)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(processInstanceId))
            return 0;

        var clip = kit.useClip();
        clip.setTable<ActivityInstanceEntity>(out var ai)
            .set(() => ai.ActivityState, (short)ActivityStateEnum.Terminated)
            .set(() => ai.EndedDateTime, endedDateTime)
            .where(() => ai.ProcessInstanceId, processInstanceId.Trim())
            .whereIn(() => ai.ActivityState, DefaultOpenStates.Cast<short?>().ToArray());

        if (!string.IsNullOrWhiteSpace(endedUserId))
            clip.set(() => ai.EndedUserId, endedUserId);
        if (!string.IsNullOrWhiteSpace(endedUserName))
            clip.set(() => ai.EndedUserName, endedUserName);

        return clip.doUpdate();
    }

    public virtual string GetProcessId(SQLBuilder kit, string activityInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(activityInstanceId))
            return null;
        try
        {
            return kit.useClip().from<ActivityInstanceEntity>(out var a)
                .where(() => a.ActivityInstanceId, activityInstanceId.Trim())
                .select(() => a.ProcessId)
                .queryUnique();
        }
        catch
        {
            return null;
        }
    }
}
