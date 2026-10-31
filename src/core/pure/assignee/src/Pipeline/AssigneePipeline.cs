using saiflow.assignee.Adapt;
using saiflow.engine.Assignee;
using saiflow.engine.Client;
using saiflow.engine.Common;

namespace saiflow.assignee;

/// <summary>
/// 办理人管道：按 ConfigPair.Type（string）分发；无显式配置时走 Legacy 推断。
/// </summary>
public class AssigneePipeline : IAssigneePipeline
{
    public AssigneeCapabilityRegistry Registry { get; }

    public SailClient? Sail { get; set; }

    /// <summary>可选：显式 BPMN ConfigPair 读取（优先于 Legacy 推断）。</summary>
    public Func<AssigneeInvokeRequest, ConfigPair?>? ResolveConfigReader { get; set; }

    public Func<LeaveInvokeRequest, ConfigPair?>? LeaveConfigReader { get; set; }

    public AssigneePipeline(AssigneeCapabilityRegistry registry)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public virtual bool TryResolveForTaskCreate(AssigneeInvokeRequest request, out PerformerList performers)
    {
        performers = new PerformerList();
        if (request == null || string.IsNullOrEmpty(request.ActivityId))
            return false;

        var config = ResolveConfigReader?.Invoke(request)
                     ?? LegacyAssigneeConfigInfer.InferResolve(request);

        if (config == null || string.IsNullOrWhiteSpace(config.Type))
            return false;

        if (!Registry.TryGetResolver(config.Type, out var resolver) || resolver == null)
            return false;

        WireSail(resolver);

        var ctx = new AssigneeResolveContext
        {
            Request = request,
            Config = config
        };

        var resolved = resolver.Resolve(ctx) ?? new PerformerList();
        if (resolved.Count == 0)
        {
            // Specific / StarterPick 无数据：交回既有路径；RoleGroup 空也交回以免静默
            return false;
        }

        performers = resolved;
        return true;
    }

    public virtual bool TryCanLeave(LeaveInvokeRequest request, out LeaveJudgeResult result)
    {
        result = null!;
        if (request == null)
            return false;

        var config = LeaveConfigReader?.Invoke(request)
                     ?? LegacyAssigneeConfigInfer.InferLeave(request);

        if (config == null || string.IsNullOrWhiteSpace(config.Type))
            return false;

        if (!Registry.TryGetLeaveJudge(config.Type, out var judge) || judge == null)
            return false;

        result = judge.CanLeave(new LeaveJudgeContext
        {
            Request = request,
            Config = config
        });
        return result != null;
    }

    protected virtual void WireSail(IAssigneeResolver resolver)
    {
        if (Sail == null)
            return;
        if (resolver is Builtin.RoleGroupAssigneeResolver rg)
            rg.Sail = Sail;
        if (resolver is Builtin.ProcessRelativeAssigneeResolver pr)
            pr.Sail = Sail;
    }
}
