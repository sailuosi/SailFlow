using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using sailflow.data;
using sailflow.storage;
using sailflow.utils;

namespace sailflow.runtime.storage;

/// <summary>默认 <see cref="IProcessVariableStorage"/>。</summary>
public class DefaultProcessVariableStorage : IProcessVariableStorage
{
    public virtual ProcessVariableEntity GetById(SQLBuilder kit, string physicalTable, string variableId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(variableId))
            return null!;
        return kit.findRowById<ProcessVariableEntity>(variableId.Trim(), physicalTable);
    }

    public virtual string Insert(SQLBuilder kit, ProcessVariableEntity entity, string physicalTable = null)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        var newId = kit.insertReturnId<ProcessVariableEntity, string>(entity, physicalTable);
        entity.ProcessVariableId = newId;
        return newId;
    }

    public virtual void Update(SQLBuilder kit, ProcessVariableEntity entity, string physicalTable = null)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        kit.update(entity, physicalTable);
    }

    public virtual void DeleteById(SQLBuilder kit, string physicalTable, string variableId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(variableId))
            return;
        kit.deleteById<ProcessVariableEntity>(variableId.Trim(), physicalTable);
    }

    public virtual void DeleteByProcessInstance(SQLBuilder kit, string physicalTable, string processInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrWhiteSpace(processInstanceId))
            return;
        try
        {
            var clip = kit.useClip();
            clip.setTable<ProcessVariableEntity>(out var v)
                .useSQL(b => b.setTable(physicalTable))
                .where(() => v.ProcessInstanceId, processInstanceId.Trim())
                .doDelete();
        }
        catch
        {
            // 分表未创建时忽略
        }
    }

    public virtual IList<ProcessVariableEntity> ListByProcessInstance(
        SQLBuilder kit,
        string physicalTable,
        string processInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ProcessVariableEntity>(physicalTable, out var v)
            .where(() => v.ProcessInstanceId, processInstanceId)
            .orderBy(() => v.ActivityId);
        return clip.select(v).queryList()?.ToList() ?? new List<ProcessVariableEntity>();
    }

    public virtual IList<ProcessVariableEntity> ListByActivityInstance(
        SQLBuilder kit,
        string physicalTable,
        string processInstanceId,
        string activityInstanceId)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ProcessVariableEntity>(physicalTable, out var v)
            .where(() => v.ProcessInstanceId, processInstanceId)
            .where(() => v.ActivityInstanceId, activityInstanceId)
            .orderBy(() => v.ActivityId);
        return clip.select(v).queryList()?.ToList() ?? new List<ProcessVariableEntity>();
    }

    public virtual IList<ProcessVariableEntity> ListByProcessScopeName(
        SQLBuilder kit,
        string physicalTable,
        string processInstanceId,
        string name)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ProcessVariableEntity>(physicalTable, out var v)
            .where(() => v.VariableScope, ProcessVariableScopeEnum.Process.ToString())
            .where(() => v.ProcessInstanceId, processInstanceId)
            .where(() => v.Name, name)
            .orderBy(() => v.ActivityId);
        return clip.select(v).queryList()?.ToList() ?? new List<ProcessVariableEntity>();
    }

    public virtual IList<ProcessVariableEntity> ListByActivityScopeName(
        SQLBuilder kit,
        string physicalTable,
        string processInstanceId,
        string activityInstanceId,
        string name)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ProcessVariableEntity>(physicalTable, out var v)
            .where(() => v.VariableScope, ProcessVariableScopeEnum.Activity.ToString())
            .where(() => v.ProcessInstanceId, processInstanceId)
            .where(() => v.ActivityInstanceId, activityInstanceId)
            .where(() => v.Name, name)
            .orderBy(() => v.Name);
        return clip.select(v).queryList()?.ToList() ?? new List<ProcessVariableEntity>();
    }

    public virtual IList<ProcessVariableEntity> ListByActivityScopeNames(
        SQLBuilder kit,
        string physicalTable,
        string processInstanceId,
        string activityInstanceId,
        IList<string> names)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        var clip = kit.useClip();
        clip.from<ProcessVariableEntity>(physicalTable, out var v)
            .where(() => v.VariableScope, ProcessVariableScopeEnum.Activity.ToString())
            .where(() => v.ProcessInstanceId, processInstanceId)
            .where(() => v.ActivityInstanceId, activityInstanceId);

        if (names != null && names.Count == 1)
            clip.where(() => v.Name, names[0]);
        else if (names != null && names.Count > 1)
            clip.whereIn(() => v.Name, names.ToList());

        clip.orderBy(() => v.Name);
        return clip.select(v).queryList()?.ToList() ?? new List<ProcessVariableEntity>();
    }
}
