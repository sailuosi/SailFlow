using System;
using mooSQL.data;
using sailflow.data;
using sailflow.storage;

namespace sailflow.runtime.storage;

/// <summary>默认 <see cref="IProcessVariableStorage"/>。</summary>
public class DefaultProcessVariableStorage : IProcessVariableStorage
{
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
}
