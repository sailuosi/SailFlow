using sailflow.assignee;
using sailflow.assignee.Builtin;
using sailflow.engine.Common;

namespace sailflow.biz.Assignee;

/// <summary>
/// 业务 Type 样例：班级学员签字。Type = <c>Business.ClassStudents</c>。
/// Params：classIdField（默认 classId）；demoUserIds（无真实学籍表时的演示名单）。
/// </summary>
public class ClassStudentsAssigneeResolver : IAssigneeResolver
{
    public const string TypeCode = "Business.ClassStudents";

    public virtual string Type => TypeCode;

    public virtual PerformerList Resolve(AssigneeResolveContext ctx)
    {
        var field = ConfigPairParams.GetString(ctx.Config, "classIdField", "classId") ?? "classId";
        var classId = ConditionBag.Get(ctx, field)
                      ?? ConfigPairParams.GetString(ctx.Config, "classId");
        var groupRole = ConfigPairParams.GetString(ctx.Config, "groupRole", GroupRoles.Member);

        // 产品接学籍：override LookupStudents(classId)
        var students = LookupStudents(ctx, classId).ToList();
        if (students.Count > 0)
            return ConditionBag.ToPerformers(students, groupId: classId, groupRole: groupRole);

        var demo = ConfigPairParams.GetStringArray(ctx.Config, "demoUserIds");
        if (demo.Length > 0)
            return ConditionBag.ToPerformers(demo, groupId: classId, groupRole: groupRole);

        return new PerformerList();
    }

    /// <summary>默认空；宿主可继承查子表。</summary>
    protected virtual IEnumerable<string> LookupStudents(AssigneeResolveContext ctx, string? classId)
    {
        yield break;
    }
}
