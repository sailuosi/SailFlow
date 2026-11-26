using System;
using mooSQL.data;
using sailflow.data;
using sailflow.storage;

namespace sailflow.runtime.storage;

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
}
