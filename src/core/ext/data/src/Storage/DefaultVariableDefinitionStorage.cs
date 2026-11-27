using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using sailflow.data;
using sailflow.storage;

namespace sailflow.data.Storage;

/// <summary>默认 <see cref="IVariableDefinitionStorage"/>（定义表 sf_variable）。</summary>
public class DefaultVariableDefinitionStorage : IVariableDefinitionStorage
{
    public virtual IList<VariableEntity> ListByActivity(
        SQLBuilder kit,
        string processId,
        string version,
        string activityId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrEmpty(processId) || string.IsNullOrEmpty(version) || string.IsNullOrEmpty(activityId))
            return new List<VariableEntity>();

        var clip = kit.useClip();
        clip.from<VariableEntity>(out var v)
            .where(() => v.ProcessId, processId)
            .where(() => v.Version, version)
            .where(() => v.ActivityId, activityId)
            .orderBy(() => v.SortOrder)
            .orderBy(() => v.VariableId);
        return clip.select(v).queryList()?.ToList() ?? new List<VariableEntity>();
    }

    public virtual VariableEntity GetById(SQLBuilder kit, string variableId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(variableId))
            return null!;
        return kit.findRowById<VariableEntity>(variableId.Trim());
    }

    public virtual string Insert(SQLBuilder kit, VariableEntity entity)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        var newId = kit.useRepo<VariableEntity, string>().InsertReturnId(entity);
        entity.VariableId = newId;
        return newId;
    }

    public virtual void Update(SQLBuilder kit, VariableEntity entity)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        kit.useRepo<VariableEntity>().Update(entity);
    }

    public virtual void DeleteById(SQLBuilder kit, string variableId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(variableId))
            return;
        kit.useRepo<VariableEntity, string>().DeleteById(variableId.Trim());
    }

    public virtual void DeleteByProcessActivity(
        SQLBuilder kit,
        string processId,
        string version,
        string activityId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.setTable<VariableEntity>(out var v)
            .where(() => v.ProcessId, processId)
            .where(() => v.Version, version)
            .where(() => v.ActivityId, activityId)
            .doDelete();
    }
}
