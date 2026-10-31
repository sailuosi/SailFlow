namespace saiflow.assignee;

/// <summary>内置 Resolve Type 码（静态 string，禁止改成 enum）。</summary>
public static class AssigneeResolveTypeCodes
{
    public const string Specific = "Specific";
    public const string FixedUsers = "FixedUsers";
    public const string ProcessRelative = "ProcessRelative";
    public const string RoleGroup = "RoleGroup";
    public const string StarterPick = "StarterPick";
    public const string ApproverPick = "ApproverPick";
    public const string Fallback = "Fallback";
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
