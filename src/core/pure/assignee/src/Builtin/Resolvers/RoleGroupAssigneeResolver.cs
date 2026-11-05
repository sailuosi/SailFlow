using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.engine.Xpdl;
using saiflow.hub.Resource;

namespace saiflow.assignee.Builtin;

/// <summary>
/// 角色/群组展开。Params.roleIds[]；或 useActivityRoles=true 时按节点定义角色展开。
/// 禁止 sealed。
/// </summary>
public class RoleGroupAssigneeResolver : IAssigneeResolver, IAssigneeResolverWire
{
    public SailClient? Sail { get; set; }

    public virtual string Type => AssigneeResolveTypeCodes.RoleGroup;

    public virtual void Wire(AssigneePipeline pipeline)
    {
        if (pipeline.Sail != null)
            Sail = pipeline.Sail;
    }

    public virtual PerformerList Resolve(AssigneeResolveContext ctx)
    {
        var roleIds = ConfigPairParams.GetStringArray(ctx.Config, "roleIds");
        if (roleIds.Length == 0 && ConfigPairParams.GetBool(ctx.Config, "useActivityRoles"))
            roleIds = LoadActivityRoleIds(ctx);

        if (roleIds.Length == 0)
            return new PerformerList();

        var resource = ResourceServiceLocator.Resolve();
        var users = resource.GetUserListByRoles(roleIds) ?? Array.Empty<User>();
        var list = new PerformerList();
        foreach (var u in users)
        {
            if (u == null || string.IsNullOrEmpty(u.UserId))
                continue;
            list.Add(new Performer(u.UserId, u.UserName ?? u.UserId) { GroupRole = GroupRoles.Member });
        }
        return list;
    }

    protected virtual string[] LoadActivityRoleIds(AssigneeResolveContext ctx)
    {
        var sail = Sail;
        var processId = ctx.Request.ProcessId ?? ctx.Runner?.ProcessId;
        var version = ctx.Request.Version ?? ctx.Runner?.Version;
        var activityId = ctx.ActivityId;
        if (sail == null || string.IsNullOrEmpty(processId) || string.IsNullOrEmpty(version)
            || string.IsNullOrEmpty(activityId))
            return Array.Empty<string>();

        var model = ProcessModelFactory.CreateByProcess(sail, processId, version);
        var roles = model.GetActivityRoles(activityId);
        if (roles == null || roles.Count == 0)
            return Array.Empty<string>();
        return roles.Select(r => r.Id).Where(id => !string.IsNullOrEmpty(id)).Cast<string>().ToArray();
    }
}
