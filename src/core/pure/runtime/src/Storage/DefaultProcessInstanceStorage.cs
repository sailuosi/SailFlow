using System;
using mooSQL.data;
using sailflow.data;
using sailflow.storage;

namespace sailflow.runtime.storage;

/// <summary>
/// 默认 <see cref="IProcessInstanceStorage"/>：kit 首参；状态热点列更新。
/// </summary>
public class DefaultProcessInstanceStorage : IProcessInstanceStorage
{
    public virtual ProcessInstanceEntity GetById(SQLBuilder kit, string processInstanceId)
    {
        ArgumentNullException.ThrowIfNull(kit);
        if (string.IsNullOrWhiteSpace(processInstanceId))
            return null!;
        return kit.findRowById<ProcessInstanceEntity>(processInstanceId.Trim());
    }

    public virtual int UpdateState(
        SQLBuilder kit,
        string processInstanceId,
        short newState,
        string updatedUserId = null,
        string updatedUserName = null)
    {
        ArgumentNullException.ThrowIfNull(kit);
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

        return clip.doUpdate();
    }
}
