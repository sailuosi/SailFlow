namespace sailflow.assignee;

/// <summary>
/// 操作模式 Mode（BPMN / 按钮配置属性名 <c>selectModel</c>）。
/// 闭集三态；禁止扩展注册表。
/// </summary>
public static class AssigneeSelectModelCodes
{
    public const string Fixed = "Fixed";
    public const string Pick = "Pick";
    public const string Preselect = "Preselect";

    public static bool TryNormalize(string? raw, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        var t = raw.Trim();
        if (string.Equals(t, Fixed, StringComparison.OrdinalIgnoreCase))
        {
            normalized = Fixed;
            return true;
        }

        if (string.Equals(t, Pick, StringComparison.OrdinalIgnoreCase))
        {
            normalized = Pick;
            return true;
        }

        if (string.Equals(t, Preselect, StringComparison.OrdinalIgnoreCase))
        {
            normalized = Preselect;
            return true;
        }

        return false;
    }

    public static bool IsKnown(string? raw) => TryNormalize(raw, out _);
}

/// <summary>内置 Resolve Type 码（静态 string，禁止改成 enum）。Scope 注册键；模式型旧码仅兼容残留，新图勿用。</summary>
public static class AssigneeResolveTypeCodes
{
    /// <summary>任意选人（空约束）；配合 selectModel=Pick 时选人器不限范围。</summary>
    public const string AnyMan = "AnyMan";

    public const string Specific = "Specific";
    public const string FixedUsers = "FixedUsers";
    public const string ProcessRelative = "ProcessRelative";
    public const string RoleGroup = "RoleGroup";

    public const string Fallback = "Fallback";

    /// <summary>单位映射：Params.unitField + maps[unit]→userIds。</summary>
    public const string UnitMap = "UnitMap";

    /// <summary>岗位相对：Params.jobCode；可继承 LookupByJob 接 HR。</summary>
    public const string JobRelative = "JobRelative";

    /// <summary>业务回调：Params.callback → <see cref="AssigneeBusinessCallbackRegistry"/>。</summary>
    public const string BusinessCallback = "BusinessCallback";

    /// <summary>表达式：Params.expression / userIdsField，从 Conditions 取值。</summary>
    public const string Expression = "Expression";

    /// <summary>
    /// 实例预选：按 ProcessInstanceId + ActivityId 从 <c>IPreselectAssigneeStore</c> 取人。
    /// 默认不进 RegisterBuiltins；宿主 <c>UsePreselectAssignees(store)</c> 显式启用。
    /// </summary>
    public const string Preselect = "Preselect";
}

/// <summary>内置 LeaveJudge Type 码（静态 string，禁止改成 enum）。</summary>
public static class LeaveJudgeTypeCodes
{
    public const string Race = "Race";
    public const string Collaborate = "Collaborate";
    public const string GroupRace = "GroupRace";
    public const string GroupCollaborate = "GroupCollaborate";
    public const string Captain = "Captain";
}

/// <summary>组内身份码（静态 string）。</summary>
public static class GroupRoles
{
    public const string Organizer = "Organizer";
    public const string Leader = "Leader";
    public const string DeputyLeader = "DeputyLeader";
    public const string Member = "Member";
}
