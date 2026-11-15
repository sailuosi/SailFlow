using sailflow.engine.Common;

namespace sailflow.assignee.Builtin;

/// <summary>
/// 实例预选定范围：Type=<see cref="AssigneeResolveTypeCodes.Preselect"/>。
/// 从 <see cref="IPreselectAssigneeStore"/> 按 PI + ActivityId 取人；空集交 EmptyAction/R4。
/// 禁止 sealed；宿主可继承覆写。
/// </summary>
public class PreselectAssigneeResolver : IAssigneeResolver
{
    public PreselectAssigneeResolver()
    {
    }

    public PreselectAssigneeResolver(IPreselectAssigneeStore store)
    {
        Store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public virtual string Type => AssigneeResolveTypeCodes.Preselect;

    /// <summary>宿主注入的预选仓；未挂时 Resolve 返回空列表。</summary>
    public IPreselectAssigneeStore? Store { get; set; }

    public virtual PerformerList Resolve(AssigneeResolveContext ctx)
    {
        var list = new PerformerList();
        var store = Store;
        if (store == null || ctx?.Request == null)
            return list;

        var pi = ctx.Request.ProcessInstanceId?.Trim();
        var activityId = ctx.ActivityId?.Trim();
        if (string.IsNullOrEmpty(pi) || string.IsNullOrEmpty(activityId))
            return list;

        var loaded = store.Load(pi, activityId);
        if (loaded == null || loaded.Count == 0)
            return list;

        foreach (var p in loaded)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.UserId))
                continue;
            list.Add(p);
        }

        return list;
    }
}
