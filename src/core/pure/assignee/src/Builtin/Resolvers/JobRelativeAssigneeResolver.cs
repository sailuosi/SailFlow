using saiflow.engine.Common;

namespace saiflow.assignee.Builtin;

/// <summary>
/// 岗位相对。Params：jobCode / jobIds；可选 unitField。
/// 默认若带 userIds 则作演示数据；真实 HR 请继承 <see cref="LookupByJob"/>。禁止 sealed。
/// </summary>
public class JobRelativeAssigneeResolver : IAssigneeResolver
{
    public virtual string Type => AssigneeResolveTypeCodes.JobRelative;

    public virtual PerformerList Resolve(AssigneeResolveContext ctx)
    {
        var jobCode = ConfigPairParams.GetString(ctx.Config, "jobCode")
                      ?? ConfigPairParams.GetStringArray(ctx.Config, "jobIds").FirstOrDefault();
        var unitField = ConfigPairParams.GetString(ctx.Config, "unitField");
        var unit = ConditionBag.Get(ctx, unitField);
        var groupRole = ConfigPairParams.GetString(ctx.Config, "groupRole", GroupRoles.Member);

        if (!string.IsNullOrEmpty(jobCode))
        {
            var looked = LookupByJob(ctx, jobCode!, unit).ToList();
            if (looked.Count > 0)
                return ConditionBag.ToPerformers(looked, groupId: unit, groupRole: groupRole);
        }

        // 演示/配置内联：无 HR 时用 Params.userIds
        var inline = ConfigPairParams.GetStringArray(ctx.Config, "userIds");
        if (inline.Length > 0)
            return ConditionBag.ToPerformers(inline, groupId: unit, groupRole: groupRole);

        return new PerformerList();
    }

    /// <summary>默认空；产品/宿主继承后查岗位编制。</summary>
    protected virtual IEnumerable<string> LookupByJob(
        AssigneeResolveContext ctx,
        string jobCode,
        string? unitId)
    {
        yield break;
    }
}
