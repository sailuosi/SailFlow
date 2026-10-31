using saiflow.engine.Common;
using saiflow.engine.Xpdl;

namespace saiflow.assignee.Builtin;

/// <summary>
/// 表达式取人。Params：
/// - userIdsField：直接从 Conditions 取逗号分隔 userId；
/// - expression：经 <see cref="ExpressionParser.ReplaceParameterToValue"/> 替换后解析为 userId 列表。
/// 禁止 sealed。
/// </summary>
public class ExpressionAssigneeResolver : IAssigneeResolver
{
    public virtual string Type => AssigneeResolveTypeCodes.Expression;

    public virtual PerformerList Resolve(AssigneeResolveContext ctx)
    {
        var groupRole = ConfigPairParams.GetString(ctx.Config, "groupRole", GroupRoles.Member);
        var groupId = ConditionBag.Get(ctx, ConfigPairParams.GetString(ctx.Config, "groupIdField"));

        var field = ConfigPairParams.GetString(ctx.Config, "userIdsField");
        if (!string.IsNullOrEmpty(field))
        {
            var raw = ConditionBag.Get(ctx, field);
            return ConditionBag.ToPerformers(ConditionBag.SplitIds(raw), groupId, groupRole);
        }

        var expression = ConfigPairParams.GetString(ctx.Config, "expression");
        if (string.IsNullOrWhiteSpace(expression))
            return new PerformerList();

        var dict = ctx.Runner?.Conditions
                   ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string replaced;
        try
        {
            replaced = ExpressionParser.ReplaceParameterToValue(expression, dict);
        }
        catch
        {
            replaced = expression;
        }

        // 表达式结果约定：逗号分隔 userId；Replace 可能包上引号
        replaced = replaced.Trim().Trim('"').Trim('\'');
        return ConditionBag.ToPerformers(ConditionBag.SplitIds(replaced), groupId, groupRole);
    }
}
