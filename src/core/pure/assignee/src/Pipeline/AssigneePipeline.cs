using saiflow.engine.Assignee;
using saiflow.engine.Common;

namespace saiflow.assignee;

/// <summary>
/// 办理人管道实现：按 ConfigPair.Type 分发。S0 尚无 BPMN ConfigPair 解析时，
/// Specific 透传有数据则处理；Leave 一律交回既有逻辑（Try 返回 false）。
/// </summary>
public class AssigneePipeline : IAssigneePipeline
{
    public AssigneeCapabilityRegistry Registry { get; }

    /// <summary>可选：从活动定义解析 ConfigPair。S0 可为空。</summary>
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
                     ?? InferSpecificConfig(request);

        if (config == null || string.IsNullOrWhiteSpace(config.Type))
            return false;

        if (!Registry.TryGetResolver(config.Type, out var resolver) || resolver == null)
            return false;

        var ctx = new AssigneeResolveContext
        {
            Request = request,
            Config = config
        };

        var resolved = resolver.Resolve(ctx) ?? new PerformerList();
        if (resolved.Count == 0
            && string.Equals(config.Type, AssigneeResolveTypeCodes.Specific, StringComparison.OrdinalIgnoreCase))
        {
            // Specific 无数据：不吞掉，交给既有路径报错/Definition
            return false;
        }

        performers = resolved;
        return performers.Count > 0;
    }

    public virtual bool TryCanLeave(LeaveInvokeRequest request, out LeaveJudgeResult result)
    {
        result = null!;
        if (request == null)
            return false;

        var config = LeaveConfigReader?.Invoke(request);
        if (config == null || string.IsNullOrWhiteSpace(config.Type))
            return false; // S0 双轨：无配置则走既有会签

        if (!Registry.TryGetLeaveJudge(config.Type, out var judge) || judge == null)
            return false;

        result = judge.CanLeave(new LeaveJudgeContext
        {
            Request = request,
            Config = config
        });
        return result != null;
    }

    /// <summary>
    /// S0：无 BPMN 配置时，若 runner 标明 Specific 且带有下一办理人，则推断为 Specific。
    /// </summary>
    protected virtual ConfigPair? InferSpecificConfig(AssigneeInvokeRequest request)
    {
        var runner = request.Runner;
        if (runner == null)
            return null;

        if (runner.NextPerformerType != NextPerformerIntTypeEnum.Specific)
            return null;

        var map = request.NextActivityPerformers;
        if (map == null || !map.TryGetValue(request.ActivityId, out var list) || list == null || list.Count == 0)
            return null;

        return ConfigPair.Create(AssigneeResolveTypeCodes.Specific);
    }
}
