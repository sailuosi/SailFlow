using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using sailflow.data;
using sailflow.hooks;
using sailflow.storage;

namespace sailflow.data.Storage;

/// <summary>默认 <see cref="IHookOutboxStorage"/>。</summary>
public class DefaultHookOutboxStorage : IHookOutboxStorage
{
    public virtual void Insert(SQLBuilder kit, HookOutboxEntity entity)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        kit.useRepo<HookOutboxEntity>().Insert(entity);
    }

    public virtual HookOutboxEntity GetById(SQLBuilder kit, string id)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(id))
            return null;
        return kit.useRepo<HookOutboxEntity>().GetById(id.Trim());
    }

    public virtual void Update(SQLBuilder kit, HookOutboxEntity entity)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        kit.useRepo<HookOutboxEntity>().Update(entity);
    }

    public virtual int CasClaimPending(
        SQLBuilder kit,
        string id,
        DateTime updatedAtUtc,
        DateTime leasedUntilUtc)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(id))
            return 0;

        var clip = kit.useClip();
        clip.setTable<HookOutboxEntity>(out var o)
            .set(() => o.Status, HookOutboxStatus.Processing)
            .set(() => o.UpdatedAt, updatedAtUtc)
            .set(() => o.LeasedUntil, leasedUntilUtc)
            .where(() => o.HookOutboxId, id.Trim())
            .where(() => o.Status, HookOutboxStatus.Pending);
        return clip.doUpdate();
    }

    public virtual int CasResetProcessingToPending(SQLBuilder kit, string id, DateTime updatedAtUtc)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(id))
            return 0;

        var clip = kit.useClip();
        clip.setTable<HookOutboxEntity>(out var u)
            .set(() => u.Status, HookOutboxStatus.Pending)
            .setToNull(() => u.LeasedUntil)
            .set(() => u.UpdatedAt, updatedAtUtc)
            .where(() => u.HookOutboxId, id.Trim())
            .where(() => u.Status, HookOutboxStatus.Processing);
        return clip.doUpdate();
    }

    public virtual void DeleteById(SQLBuilder kit, string id)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(id))
            return;
        kit.useRepo<HookOutboxEntity, string>().DeleteById(id.Trim());
    }

    public virtual IList<HookOutboxEntity> ListPendingDue(
        SQLBuilder kit,
        int limit,
        DateTime nowUtc,
        string kind = null)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var take = Math.Max(1, limit);
        var clip = kit.useClip();
        clip.from<HookOutboxEntity>(out var o)
            .where(() => o.Status, HookOutboxStatus.Pending)
            .where(() => o.NextRetryAt == null || o.NextRetryAt <= nowUtc);
        if (!string.IsNullOrEmpty(kind))
            clip.where(() => o.Kind, kind);
        else
            clip.where(() => o.Kind != HookOutboxKinds.AssigneeAutoApprove);
        clip.orderBy(() => o.CreatedAt);
        return clip.select(o).take(take).queryList()?.ToList()
               ?? new List<HookOutboxEntity>();
    }

    public virtual IList<HookOutboxEntity> ListByStatus(SQLBuilder kit, string status, int limit)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var take = Math.Max(1, limit);
        var clip = kit.useClip();
        clip.from<HookOutboxEntity>(out var o)
            .where(() => o.Status, status)
            .orderBy(() => o.CreatedAt);
        return clip.select(o).take(take).queryList()?.ToList()
               ?? new List<HookOutboxEntity>();
    }

    public virtual IList<HookOutboxEntity> ListDead(
        SQLBuilder kit,
        int limit,
        string processCode = null,
        string handlerName = null)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var take = Math.Max(1, limit);
        var clip = kit.useClip();
        clip.from<HookOutboxEntity>(out var o)
            .where(() => o.Status, HookOutboxStatus.Dead);
        if (!string.IsNullOrEmpty(processCode))
            clip.where(() => o.ProcessCode, processCode);
        if (!string.IsNullOrEmpty(handlerName))
            clip.where(() => o.HandlerName, handlerName);
        clip.orderBy(() => o.CreatedAt);
        return clip.select(o).take(take).queryList()?.ToList()
               ?? new List<HookOutboxEntity>();
    }

    public virtual int CountByStatus(SQLBuilder kit, string status)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<HookOutboxEntity>(out var o)
            .where(() => o.Status, status);
        return clip.select(o).count();
    }

    public virtual IList<HookOutboxEntity> ListStaleProcessing(
        SQLBuilder kit,
        DateTime nowUtc,
        DateTime staleBeforeUtc,
        int limit)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var take = Math.Max(1, limit);
        var clip = kit.useClip();
        clip.from<HookOutboxEntity>(out var o)
            .where(() => o.Status, HookOutboxStatus.Processing)
            .where(() =>
                (o.LeasedUntil != null && o.LeasedUntil < nowUtc)
                || (o.LeasedUntil == null && o.UpdatedAt != null && o.UpdatedAt < staleBeforeUtc))
            .orderBy(() => o.CreatedAt);
        return clip.select(o).take(take).queryList()?.ToList()
               ?? new List<HookOutboxEntity>();
    }

    public virtual IList<HookOutboxEntity> ListTerminalOlderThan(
        SQLBuilder kit,
        string status,
        DateTime olderThanUtc,
        int limit)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (limit <= 0)
            return new List<HookOutboxEntity>();

        var clip = kit.useClip();
        clip.from<HookOutboxEntity>(out var o)
            .where(() => o.Status, status)
            .where(() =>
                (o.UpdatedAt != null && o.UpdatedAt < olderThanUtc)
                || (o.UpdatedAt == null && o.CreatedAt != null && o.CreatedAt < olderThanUtc))
            .orderBy(() => o.CreatedAt);
        return clip.select(o).take(limit).queryList()?.ToList()
               ?? new List<HookOutboxEntity>();
    }

    public virtual IList<HookOutboxEntity> ListUnboundAssigneeAutoApprove(
        SQLBuilder kit,
        string processInstanceId,
        int limit)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(processInstanceId))
            return new List<HookOutboxEntity>();

        var take = Math.Max(1, limit);
        var clip = kit.useClip();
        clip.from<HookOutboxEntity>(out var o)
            .where(() => o.Kind, HookOutboxKinds.AssigneeAutoApprove)
            .where(() => o.ProcessInstanceId, processInstanceId.Trim())
            .where(() => o.Status, HookOutboxStatus.Pending)
            .where(() => o.ActivityInstanceId == null)
            .orderBy(() => o.CreatedAt);
        return clip.select(o).take(take).queryList()?.ToList()
               ?? new List<HookOutboxEntity>();
    }
}
