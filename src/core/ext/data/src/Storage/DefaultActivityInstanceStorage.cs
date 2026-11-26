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

/// <summary>默认 <see cref="IActivityInstanceStorage"/>。</summary>
public class DefaultActivityInstanceStorage : IActivityInstanceStorage
{
    static readonly short[] DefaultOpenStates =
    {
        (short)ActivityStateEnum.Ready,
        (short)ActivityStateEnum.Running,
        (short)ActivityStateEnum.Suspended
    };

    static readonly short?[] ReadyOrRunningStates =
    {
        (short)ActivityStateEnum.Ready,
        (short)ActivityStateEnum.Running
    };

    static readonly short?[] ValidSplitedOrMiChildStates =
    {
        (short)ActivityStateEnum.Ready,
        (short)ActivityStateEnum.Running,
        (short)ActivityStateEnum.Completed,
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

    public virtual IList<ActivityInstanceEntity> ListByIds(SQLBuilder kit, IEnumerable<string> activityInstanceIds)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (activityInstanceIds == null)
            return new List<ActivityInstanceEntity>();
        return kit.findListByIds<ActivityInstanceEntity>(activityInstanceIds)
               ?? new List<ActivityInstanceEntity>();
    }

    public virtual async Task<ActivityInstanceEntity> GetByIdAsync(
        SQLBuilder kit,
        string activityInstanceId,
        CancellationToken cancellationToken = default)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(activityInstanceId))
            return null!;

        var clip = kit.useClip();
        clip.from<ActivityInstanceEntity>(out var ai)
            .where(() => ai.ActivityInstanceId, activityInstanceId.Trim());
        var list = await clip.select(ai).take(1).queryListAsync(cancellationToken).ConfigureAwait(false);
        return list?.FirstOrDefault();
    }

    public virtual IList<ActivityInstanceEntity> ListByProcessInstance(SQLBuilder kit, string processInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ActivityInstanceEntity>(out var ai)
            .where(() => ai.ProcessInstanceId, processInstanceId)
            .orderBy(() => ai.CreatedDateTime);
        return clip.select(ai).queryList().ToList();
    }

    public virtual ActivityInstanceEntity GetLatestByProcessInstanceActivity(
        SQLBuilder kit,
        string processInstanceId,
        string activityId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ActivityInstanceEntity>(out var ai)
            .where(() => ai.ProcessInstanceId, processInstanceId)
            .where(() => ai.ActivityId, activityId)
            .orderByDesc(() => ai.CreatedDateTime);
        return clip.select(ai).take(1).queryList()?.FirstOrDefault();
    }

    public virtual IList<ActivityInstanceEntity> ListByRunningProcessBizProcessActivity(
        SQLBuilder kit,
        string bizPK,
        string processId,
        string activityId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ActivityInstanceEntity>(out var ai)
            .InnerJoin<ProcessInstanceEntity>(out var pi).on(() => ai.ProcessInstanceId == pi.ProcessInstanceId)
            .where(() => pi.ProcessState, (short)ProcessStateEnum.Running)
            .where(() => ai.BizPK, bizPK)
            .where(() => ai.ProcessId, processId)
            .where(() => ai.ActivityId, activityId);
        return clip.select(ai).queryList().ToList();
    }

    public virtual IList<ActivityInstanceEntity> ListReadyOrRunningByRunningProcessVersionActivity(
        SQLBuilder kit,
        string processId,
        string version,
        string activityId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ActivityInstanceEntity>(out var ai)
            .InnerJoin<ProcessInstanceEntity>(out var pi).on(() => ai.ProcessInstanceId == pi.ProcessInstanceId)
            .where(() => pi.ProcessState, (short)ProcessStateEnum.Running)
            .where(() => pi.ProcessId, processId)
            .where(() => pi.Version, version)
            .where(() => ai.ActivityId, activityId)
            .whereIn(() => ai.ActivityState, ReadyOrRunningStates);
        return clip.select(ai).queryList().ToList();
    }

    public virtual IList<ActivityInstanceEntity> ListByProcessInstanceActivityState(
        SQLBuilder kit,
        string processInstanceId,
        string activityId,
        short activityState)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ActivityInstanceEntity>(out var ai)
            .where(() => ai.ProcessInstanceId, processInstanceId)
            .where(() => ai.ActivityId, activityId)
            .where(() => ai.ActivityState, activityState)
            .orderByDesc(() => ai.CreatedDateTime);
        return clip.select(ai).queryList().ToList();
    }

    public virtual IList<ActivityInstanceEntity> ListCompletedByRunningProcessBizProcess(
        SQLBuilder kit,
        string bizPK,
        string processId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ActivityInstanceEntity>(out var ai)
            .InnerJoin<ProcessInstanceEntity>(out var pi).on(() => ai.ProcessInstanceId == pi.ProcessInstanceId)
            .where(() => pi.ProcessState, (short)ProcessStateEnum.Running)
            .where(() => ai.BizPK, bizPK)
            .where(() => ai.ProcessId, processId)
            .where(() => ai.ActivityState, (short)ActivityStateEnum.Completed);
        return clip.select(ai).queryList().ToList();
    }

    public virtual IList<ActivityInstanceEntity> ListCompletedByProcessInstanceActivity(
        SQLBuilder kit,
        string processInstanceId,
        string activityId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ActivityInstanceEntity>(out var ai)
            .where(() => ai.ProcessInstanceId, processInstanceId)
            .where(() => ai.ActivityId, activityId)
            .where(() => ai.ActivityState, (short)ActivityStateEnum.Completed)
            .orderByDesc(() => ai.EndedDateTime);
        return clip.select(ai).queryList().ToList();
    }

    public virtual IList<ActivityInstanceEntity> ListByTaskJoin(SQLBuilder kit, string taskId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ActivityInstanceEntity>(out var ai)
            .InnerJoin<TaskEntity>(out var t).on(() => ai.ActivityInstanceId == t.ActivityInstanceId)
            .where(() => t.TaskId, taskId);
        return clip.select(ai).queryList().ToList();
    }

    public virtual IList<ActivityInstanceEntity> ListRunningReadyByBizProcessVersion(
        SQLBuilder kit,
        string bizPK,
        string processId,
        string version)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ActivityInstanceEntity>(out var ai)
            .InnerJoin<ProcessInstanceEntity>(out var pi).on(() => ai.ProcessInstanceId == pi.ProcessInstanceId)
            .whereIn(() => ai.ActivityState, ReadyOrRunningStates)
            .where(() => pi.ProcessState, (short)ProcessStateEnum.Running)
            .where(() => pi.Version, version)
            .where(() => ai.BizPK, bizPK)
            .where(() => ai.ProcessId, processId);
        return clip.select(ai).queryList().ToList();
    }

    public virtual async Task<IList<ActivityInstanceEntity>> ListRunningReadyByBizProcessVersionAsync(
        SQLBuilder kit,
        string bizPK,
        string processId,
        string version,
        CancellationToken cancellationToken = default)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        cancellationToken.ThrowIfCancellationRequested();
        var clip = kit.useClip();
        clip.from<ActivityInstanceEntity>(out var ai)
            .InnerJoin<ProcessInstanceEntity>(out var pi).on(() => ai.ProcessInstanceId == pi.ProcessInstanceId)
            .whereIn(() => ai.ActivityState, ReadyOrRunningStates)
            .where(() => pi.ProcessState, (short)ProcessStateEnum.Running)
            .where(() => pi.Version, version)
            .where(() => ai.BizPK, bizPK)
            .where(() => ai.ProcessId, processId);
        var list = await clip.select(ai).queryListAsync(cancellationToken).ConfigureAwait(false);
        return list?.ToList() ?? new List<ActivityInstanceEntity>();
    }

    public virtual IList<ActivityInstanceEntity> ListCompletedMultipleInstanceByMain(
        SQLBuilder kit,
        string mainActivityInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ActivityInstanceEntity>(out var ai)
            .InnerJoin<ProcessInstanceEntity>(out var pi).on(() => ai.ProcessInstanceId == pi.ProcessInstanceId)
            .where(() => pi.ProcessState, (short)ProcessStateEnum.Running)
            .where(() => ai.MainActInstanceId, mainActivityInstanceId)
            .where(() => ai.ActivityState, (short)ActivityStateEnum.Completed);
        return clip.select(ai).queryList().ToList();
    }

    public virtual IList<ActivityInstanceEntity> ListCompletedByProcessInstanceActivityOrderByEndedDesc(
        SQLBuilder kit,
        string processInstanceId,
        string activityId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ActivityInstanceEntity>(out var ai)
            .where(() => ai.ProcessInstanceId, processInstanceId)
            .where(() => ai.ActivityId, activityId)
            .where(() => ai.ActivityState, (short)ActivityStateEnum.Completed)
            .orderByDesc(() => ai.EndedDateTime);
        return clip.select(ai).queryList().ToList();
    }

    public virtual IList<ActivityInstanceEntity> ListMultipleInstanceChildren(
        SQLBuilder kit,
        string mainActivityInstanceId,
        string processInstanceId,
        short? activityState)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ActivityInstanceEntity>(out var ai)
            .where(() => ai.MainActInstanceId, mainActivityInstanceId)
            .where(() => ai.ProcessInstanceId, processInstanceId);
        if (activityState.HasValue)
            clip.where(() => ai.ActivityState, activityState.Value);
        clip.orderBy(() => ai.CompleteOrder);
        return clip.select(ai).queryList().ToList();
    }

    public virtual IList<ActivityInstanceEntity> ListValidMultipleInstanceChildren(
        SQLBuilder kit,
        string mainActivityInstanceId,
        string processInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ActivityInstanceEntity>(out var ai)
            .whereIn(() => ai.ActivityState, ValidSplitedOrMiChildStates)
            .where(() => ai.MainActInstanceId, mainActivityInstanceId)
            .where(() => ai.ProcessInstanceId, processInstanceId);
        return clip.select(ai).queryList().ToList();
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
