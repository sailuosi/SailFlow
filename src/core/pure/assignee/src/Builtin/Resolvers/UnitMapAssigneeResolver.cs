using saiflow.engine.Common;

namespace saiflow.assignee.Builtin;

/// <summary>
/// 单位映射。Params：unitField（Conditions 键）、maps{unit→userIds}、fallbackUserIds、groupRole。
/// 可继承 <see cref="LookupUsers"/> 接真实组织主数据。禁止 sealed。
/// </summary>
public class UnitMapAssigneeResolver : IAssigneeResolver
{
    public virtual string Type => AssigneeResolveTypeCodes.UnitMap;

    public virtual PerformerList Resolve(AssigneeResolveContext ctx)
    {
        var unitField = ConfigPairParams.GetString(ctx.Config, "unitField", "unitId") ?? "unitId";
        var unit = ConditionBag.Get(ctx, unitField)
                   ?? ConfigPairParams.GetString(ctx.Config, "unitId");
        var groupRole = ConfigPairParams.GetString(ctx.Config, "groupRole", GroupRoles.Member);

        if (!string.IsNullOrEmpty(unit))
        {
            var fromMap = LookupUsers(ctx, unit).ToList();
            if (fromMap.Count > 0)
                return ConditionBag.ToPerformers(fromMap, groupId: unit, groupRole: groupRole);
        }

        var fallback = ConfigPairParams.GetStringArray(ctx.Config, "fallbackUserIds");
        if (fallback.Length > 0)
            return ConditionBag.ToPerformers(fallback, groupId: unit, groupRole: groupRole);

        return new PerformerList();
    }

    /// <summary>默认读 Params.maps；子类可改查 DB/HR。</summary>
    protected virtual IEnumerable<string> LookupUsers(AssigneeResolveContext ctx, string unitKey)
    {
        foreach (var id in ConditionBag.MapLookup(ctx.Config, unitKey))
            yield return id;
    }
}
