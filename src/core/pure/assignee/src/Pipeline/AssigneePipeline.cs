using System.Diagnostics;
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

    /// <summary>业务回调（BusinessCallback Type）；宿主/biz 可往里 Register。</summary>
    public AssigneeBusinessCallbackRegistry Callbacks { get; } = new();

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

        if (Registry.IsResolverOverridden(config.Type))
        {
            Trace.TraceInformation(
                "[Assignee] Resolve using overridden Type '{0}' impl={1}",
                config.Type,
                Registry.GetResolverImplementationName(config.Type));
        }

        WireSail(resolver);

        var ctx = new AssigneeResolveContext
        {
            Request = request,
            Config = config
        };

        var resolved = resolver.Resolve(ctx) ?? new PerformerList();
        if (resolved.Count == 0)
        {
            // 空结果交 Mediator：已挂管道时按 R2/R4 抛 WfAssigneeException（不再双轨回落）
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

        if (Registry.IsLeaveJudgeOverridden(config.Type))
        {
            Trace.TraceInformation(
                "[Assignee] Leave using overridden Type '{0}' impl={1}",
                config.Type,
                Registry.GetLeaveJudgeImplementationName(config.Type));
        }

        result = judge.CanLeave(new LeaveJudgeContext
        {
            Request = request,
            Config = config
        });
        return result != null;
    }

    protected virtual void WireSail(IAssigneeResolver resolver)
    {
        if (resolver is Builtin.RoleGroupAssigneeResolver rg && Sail != null)
            rg.Sail = Sail;
        if (resolver is Builtin.ProcessRelativeAssigneeResolver pr && Sail != null)
            pr.Sail = Sail;
        if (resolver is Builtin.BusinessCallbackAssigneeResolver bc)
            bc.Callbacks = Callbacks;
        if (resolver is Builtin.FallbackAssigneeResolver fb)
            fb.Registry = Registry;
    }
}
