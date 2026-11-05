using saiflow.engine.Common;

namespace saiflow.assignee.Builtin;

/// <summary>
/// 业务动态取人。Params.callback → <see cref="AssigneeBusinessCallbackRegistry"/>。
/// 禁止 sealed。
/// </summary>
public class BusinessCallbackAssigneeResolver : IAssigneeResolver, IAssigneeResolverWire
{
    public AssigneeBusinessCallbackRegistry? Callbacks { get; set; }

    public virtual string Type => AssigneeResolveTypeCodes.BusinessCallback;

    public virtual void Wire(AssigneePipeline pipeline)
    {
        Callbacks = pipeline.Callbacks;
    }

    public virtual PerformerList Resolve(AssigneeResolveContext ctx)
    {
        var name = ConfigPairParams.GetString(ctx.Config, "callback")
                   ?? ConfigPairParams.GetString(ctx.Config, "name");
        if (string.IsNullOrWhiteSpace(name) || Callbacks == null)
            return new PerformerList();

        if (!Callbacks.TryGet(name, out var cb) || cb == null)
            return new PerformerList();

        return cb.Resolve(ctx) ?? new PerformerList();
    }
}
