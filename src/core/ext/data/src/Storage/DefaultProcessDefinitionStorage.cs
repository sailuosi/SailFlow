using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using sailflow.data;
using sailflow.storage;
using sailflow.utils;

namespace sailflow.data.Storage;

/// <summary>默认 <see cref="IProcessDefinitionStorage"/>（流程定义元数据）。</summary>
public class DefaultProcessDefinitionStorage : IProcessDefinitionStorage
{
    public virtual ProcessEntity GetById(SQLBuilder kit, string processDefId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(processDefId))
            return null!;
        return kit.findRowById<ProcessEntity>(processDefId.Trim());
    }

    public virtual IList<ProcessEntity> ListByProcessIdVersion(SQLBuilder kit, string processId, string version)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ProcessEntity>(out var p)
            .where(() => p.ProcessId, processId)
            .where(() => p.Version, version);
        return clip.select(p).queryList()?.ToList() ?? new List<ProcessEntity>();
    }

    public virtual IList<ProcessEntity> ListByProcessIdStatus(SQLBuilder kit, string processId, byte status)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ProcessEntity>(out var p)
            .where(() => p.ProcessId, processId)
            .where(() => p.Status, status);
        return clip.select(p).queryList()?.ToList() ?? new List<ProcessEntity>();
    }

    public virtual IList<ProcessEntity> ListByName(SQLBuilder kit, string processName, string version)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ProcessEntity>(out var p)
            .where(() => p.ProcessName, processName);
        if (!string.IsNullOrEmpty(version))
            clip.where(() => p.Version, version);
        return clip.select(p).queryList()?.ToList() ?? new List<ProcessEntity>();
    }

    public virtual IList<ProcessEntity> ListByCode(SQLBuilder kit, string processCode, string version)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ProcessEntity>(out var p)
            .where(() => p.ProcessCode, processCode);
        if (!string.IsNullOrEmpty(version))
            clip.where(() => p.Version, version);
        return clip.select(p).queryList()?.ToList() ?? new List<ProcessEntity>();
    }

    public virtual IList<ProcessEntity> ListByMessage(SQLBuilder kit, string topic)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ProcessEntity>(out var p)
            .where(() => p.StartType, (byte)ProcessStartTypeEnum.Message)
            .where(() => p.StartExpression, topic);
        return clip.select(p).queryList()?.ToList() ?? new List<ProcessEntity>();
    }

    public virtual IList<ProcessEntity> ListByAppType(SQLBuilder kit, string appType)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ProcessEntity>(out var p)
            .where(() => p.AppType, appType);
        return clip.select(p).queryList()?.ToList() ?? new List<ProcessEntity>();
    }

    public virtual IList<ProcessEntity> GetAll(SQLBuilder kit)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ProcessEntity>(out var p);
        return clip.select(p).queryList()?.ToList() ?? new List<ProcessEntity>();
    }

    public virtual IList<ProcessEntity> ListByPackageId(SQLBuilder kit, string packageId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ProcessEntity>(out var p)
            .where(() => p.PackageId, packageId);
        return clip.select(p).queryList()?.ToList() ?? new List<ProcessEntity>();
    }

    public virtual IList<ProcessEntity> ListPoolsByPackageId(SQLBuilder kit, string packageId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ProcessEntity>(out var p)
            .where(() => p.PackageType, (byte)PackageTypeEnum.Pool)
            .where(() => p.PackageId, packageId);
        return clip.select(p).queryList()?.ToList() ?? new List<ProcessEntity>();
    }

    public virtual string Insert(SQLBuilder kit, ProcessEntity entity)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        return kit.useRepo<ProcessEntity, string>().InsertReturnId(entity);
    }

    public virtual void Update(SQLBuilder kit, ProcessEntity entity)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        kit.update(entity);
    }

    public virtual void DeleteById(SQLBuilder kit, string processDefId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(processDefId))
            return;
        kit.useRepo<ProcessEntity, string>().DeleteById(processDefId.Trim());
    }

    public virtual void Delete(SQLBuilder kit, ProcessEntity entity)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (entity == null)
            return;
        kit.useRepo<ProcessEntity, string>().Delete(entity);
    }

    public virtual void DeleteByProcessId(SQLBuilder kit, string processId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.setTable<ProcessEntity>(out var p)
            .where(() => p.ProcessId, processId)
            .doDelete();
    }

    public virtual void DeleteByProcessIdVersion(SQLBuilder kit, string processId, string version)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.setTable<ProcessEntity>(out var p)
            .where(() => p.ProcessId, processId)
            .where(() => p.Version, version)
            .doDelete();
    }

    public virtual void DeleteByPackageId(SQLBuilder kit, string packageId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.setTable<ProcessEntity>(out var p)
            .where(() => p.PackageId, packageId)
            .doDelete();
    }

    public virtual void DeletePoolsByPackageId(SQLBuilder kit, string packageId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.setTable<ProcessEntity>(out var p)
            .where(() => p.PackageId, packageId)
            .where(() => p.PackageType, (byte)PackageTypeEnum.Pool)
            .doDelete();
    }

    public virtual void DeletePoolsByPackageIdAndDefIds(
        SQLBuilder kit,
        string packageId,
        IList<string> processDefIds)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (processDefIds == null || processDefIds.Count == 0)
            return;
        var clip = kit.useClip();
        clip.setTable<ProcessEntity>(out var p)
            .where(() => p.PackageId, packageId)
            .whereIn(() => p.ProcessDefId, processDefIds)
            .doDelete();
    }

    public virtual void UpdateStatusByPackageId(SQLBuilder kit, string packageProcessDefId, byte status)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.setTable<ProcessEntity>(out var p)
            .set(() => p.Status, status)
            .where(() => p.PackageId, packageProcessDefId)
            .doUpdate();
    }
}
