using saiflow.data;
using saiflow.engine.Entity;
using saiflow.engine.Client;
using saiflow.engine.Common;

namespace saiflow.assignee.Builtin;

/// <summary>
/// 流程相对人。Params.relative = Starter | PrevSender（S1：PrevSender≈当前 runner）。
/// 禁止 sealed。
/// </summary>
public class ProcessRelativeAssigneeResolver : IAssigneeResolver, IAssigneeResolverWire
{
    public const string RelativeStarter = "Starter";
    public const string RelativePrevSender = "PrevSender";
    public const string RelativePrevAssignee = "PrevAssignee";

    public SailClient? Sail { get; set; }

    public virtual string Type => AssigneeResolveTypeCodes.ProcessRelative;

    public virtual void Wire(AssigneePipeline pipeline)
    {
        if (pipeline.Sail != null)
            Sail = pipeline.Sail;
    }

    public virtual PerformerList Resolve(AssigneeResolveContext ctx)
    {
        var relative = ConfigPairParams.GetString(ctx.Config, "relative", RelativeStarter)
                       ?? RelativeStarter;
        var list = new PerformerList();

        if (string.Equals(relative, RelativeStarter, StringComparison.OrdinalIgnoreCase))
        {
            var pi = LoadProcessInstance(ctx);
            if (pi != null && !string.IsNullOrEmpty(pi.CreatedUserId))
            {
                list.Add(new Performer(pi.CreatedUserId, pi.CreatedUserName ?? pi.CreatedUserId)
                {
                    GroupRole = GroupRoles.Member
                });
            }
            return list;
        }

        if (string.Equals(relative, RelativePrevSender, StringComparison.OrdinalIgnoreCase)
            || string.Equals(relative, RelativePrevAssignee, StringComparison.OrdinalIgnoreCase))
        {
            var runner = ctx.Runner;
            if (runner != null && !string.IsNullOrEmpty(runner.UserId))
            {
                list.Add(new Performer(runner.UserId, runner.UserName ?? runner.UserId)
                {
                    GroupRole = GroupRoles.Member
                });
            }
            return list;
        }

        return list;
    }

    protected virtual ProcessInstanceEntity? LoadProcessInstance(AssigneeResolveContext ctx)
    {
        var sail = Sail;
        var piId = ctx.Request.ProcessInstanceId;
        if (sail == null || piId == null || piId.Value <= 0)
            return null;
        return sail.UseRunSQL().useRepo<ProcessInstanceEntity, int>().GetById(piId.Value);
    }
}
