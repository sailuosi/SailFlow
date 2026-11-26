using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using sailflow.data;
using sailflow.storage;
using sailflow.utils;

namespace sailflow.runtime.storage;

/// <summary>
/// 默认 <see cref="IProcessInstanceStorage"/>：kit 首参；状态热点列更新。
/// </summary>
public class DefaultProcessInstanceStorage : IProcessInstanceStorage
{
    public virtual ProcessInstanceEntity GetById(SQLBuilder kit, string processInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(processInstanceId))
            return null!;
        return kit.findRowById<ProcessInstanceEntity>(processInstanceId.Trim());
    }

    public virtual string Insert(SQLBuilder kit, ProcessInstanceEntity entity)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        var newId = kit.useRepo<ProcessInstanceEntity, string>().InsertReturnId(entity);
        entity.ProcessInstanceId = newId;
        return newId;
    }

    public virtual void Update(SQLBuilder kit, ProcessInstanceEntity entity)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        kit.useRepo<ProcessInstanceEntity>().Update(entity);
    }

    public virtual void DeleteById(SQLBuilder kit, string processInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(processInstanceId))
            return;
        kit.useRepo<ProcessInstanceEntity, string>().DeleteById(processInstanceId.Trim());
    }

    public virtual int UpdateState(
        SQLBuilder kit,
        string processInstanceId,
        short newState,
        string updatedUserId = null,
        string updatedUserName = null,
        DateTime? endedDateTime = null,
        string endedUserId = null,
        string endedUserName = null)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(processInstanceId))
            return 0;

        var clip = kit.useClip();
        clip.setTable<ProcessInstanceEntity>(out var pi)
            .set(() => pi.ProcessState, newState)
            .set(() => pi.UpdatedDateTime, DateTime.UtcNow)
            .where(() => pi.ProcessInstanceId, processInstanceId.Trim());

        if (!string.IsNullOrWhiteSpace(updatedUserId))
            clip.set(() => pi.UpdatedUserId, updatedUserId);
        if (!string.IsNullOrWhiteSpace(updatedUserName))
            clip.set(() => pi.UpdatedUserName, updatedUserName);

        if (endedDateTime != null)
        {
            clip.set(() => pi.EndedDateTime, endedDateTime.Value);
            if (!string.IsNullOrWhiteSpace(endedUserId))
                clip.set(() => pi.EndedUserId, endedUserId);
            if (!string.IsNullOrWhiteSpace(endedUserName))
                clip.set(() => pi.EndedUserName, endedUserName);
        }

        return clip.doUpdate();
    }

    public virtual int DiscardByVersion(
        SQLBuilder kit,
        string bizPK,
        string processId,
        string version,
        string updatedUserId,
        string updatedUserName)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));

        var clip = kit.useClip();
        clip.setTable<ProcessInstanceEntity>(out var pi)
            .set(() => pi.ProcessState, (short)ProcessStateEnum.Discarded)
            .set(() => pi.RecordStatusInvalid, (byte)1)
            .set(() => pi.UpdatedDateTime, DateTime.UtcNow)
            .set(() => pi.UpdatedUserId, updatedUserId)
            .set(() => pi.UpdatedUserName, updatedUserName)
            .where(() => pi.BizPK, bizPK)
            .where(() => pi.ProcessId, processId)
            .where(() => pi.Version, version);
        return clip.doUpdate();
    }

    public virtual IList<ProcessInstanceEntity> ListSuspendedByInvokedActivity(
        SQLBuilder kit,
        string invokedActivityInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(invokedActivityInstanceId))
            return new List<ProcessInstanceEntity>();

        var clip = kit.useClip();
        clip.from<ProcessInstanceEntity>(out var pi)
            .where(() => pi.InvokedActInstanceId, invokedActivityInstanceId.Trim())
            .where(() => pi.ProcessState, (short)ProcessStateEnum.Suspended)
            .orderByDesc(() => pi.CreatedDateTime);
        return clip.select(pi).queryList()?.ToList() ?? new List<ProcessInstanceEntity>();
    }
}
