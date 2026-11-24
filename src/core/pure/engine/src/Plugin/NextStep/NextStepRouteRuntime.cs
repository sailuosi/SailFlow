using System;
using System.Collections.Generic;
using System.Linq;
using sailflow.engine.Client;
using sailflow.utils;
using sailflow.data;
using sailflow.xml;

namespace sailflow.nextStep;
/// <summary>下一跳管道运行时入口（Prepare / Filter）。</summary>
public static class NextStepRouteRuntime
{
    public const string ReasonAndSplitGuard = "AndSplitGuard";

    /// <summary>
    /// 在结构演算前对条件袋执行 Client 级 Prepare（同袋原地写）。
    /// 未挂管道时原样返回（必要时新建可写袋）。
    /// </summary>
    public static IDictionary<string, string> ApplyPrepare(
        SailClient sail,
        IDictionary<string, string> conditions,
        Activity currentActivity,
        ProcessEntity processEntity,
        NextStepRoutePhase phase,
        FlowCommandContext context = null,
        string activityInstanceId = null)
    {
        var bag = ResolveWritableBag(conditions, context);
        var pipeline = sail?.NextStepRoute;
        if (pipeline == null)
            return bag;

        pipeline.Prepare(BuildContext(
            sail, bag, currentActivity, processEntity, phase, context, activityInstanceId));
        return bag;
    }

    /// <summary>
    /// 结构演算后跑 Filter；AndSplit 必达支不可静默删/禁；返回候选列表供预览标注或发送裁剪。
    /// 未挂管道 → null。
    /// </summary>
    public static NextStepCandidateList ApplyFilter(
        SailClient sail,
        NextActivityMatchedResult matched,
        Activity currentActivity,
        ProcessEntity processEntity,
        NextStepRoutePhase phase,
        IDictionary<string, string> conditions = null,
        FlowCommandContext context = null,
        string activityInstanceId = null)
    {
        var pipeline = sail?.NextStepRoute;
        if (pipeline == null || matched?.Root == null)
            return null;

        var candidates = CollectCandidates(matched.Root);
        if (candidates.Items.Count == 0)
            return candidates;

        var requiredSnapshot = candidates.Items
            .Where(c => c.Required)
            .Select(c => CloneCandidate(c))
            .ToList();

        var ctx = BuildContext(
            sail, ResolveWritableBag(conditions, context), currentActivity, processEntity,
            phase, context, activityInstanceId);
        pipeline.Filter(ctx, candidates);

        EnforceAndSplitGuard(candidates, requiredSnapshot);

        if (candidates.Items.Count == 0
            || candidates.Items.All(c => c.Disabled))
            NextStepRouteDiagnostics.ZeroCandidates();

        return candidates;
    }

    /// <summary>把 Filter 结果写回 NodeView（预览）。</summary>
    public static void AnnotateNodeViews(IList<NodeView> views, NextStepCandidateList candidates)
    {
        if (views == null || candidates?.Items == null)
            return;
        var map = candidates.Items
            .Where(c => !string.IsNullOrEmpty(c.ActivityId))
            .GroupBy(c => c.ActivityId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last(), StringComparer.OrdinalIgnoreCase);

        for (var i = views.Count - 1; i >= 0; i--)
        {
            var v = views[i];
            if (v?.ActivityId == null || !map.TryGetValue(v.ActivityId, out var c))
                continue;
            v.Disabled = c.Disabled;
            v.Required = c.Required;
            v.ReasonCode = c.ReasonCode;
        }
    }

    /// <summary>发送路径：从树中移除 Disabled 且非 Required 的叶子。</summary>
    public static void PruneDisabledFromTree(NextActivityComponent root, NextStepCandidateList candidates)
    {
        if (root == null || candidates?.Items == null)
            return;
        var disabled = new HashSet<string>(
            candidates.Items.Where(c => c.Disabled && !c.Required)
                .Select(c => c.ActivityId),
            StringComparer.OrdinalIgnoreCase);
        if (disabled.Count == 0)
            return;
        PruneRecursive(root, disabled);
    }

    internal static NextStepCandidateList CollectCandidates(NextActivityComponent root)
    {
        var list = new NextStepCandidateList();
        CollectRecursive(root, andSplitParent: false, list);
        return list;
    }

    static void CollectRecursive(NextActivityComponent node, bool andSplitParent, NextStepCandidateList list)
    {
        if (node == null)
            return;

        var andHere = andSplitParent || IsAndSplitGateway(node.Activity);
        if (!node.HasChildren)
        {
            if (node.Activity != null)
            {
                list.Items.Add(new NextStepCandidate
                {
                    ActivityId = node.Activity.ActivityId,
                    Activity = node.Activity,
                    Required = andHere,
                    Disabled = false
                });
            }
            return;
        }

        foreach (var child in EnumerateChildren(node))
            CollectRecursive(child, andHere, list);
    }

    static void EnforceAndSplitGuard(
        NextStepCandidateList candidates,
        List<NextStepCandidate> requiredSnapshot)
    {
        if (requiredSnapshot == null || requiredSnapshot.Count == 0)
            return;

        foreach (var req in requiredSnapshot)
        {
            var current = candidates.Items
                .FirstOrDefault(c => string.Equals(c.ActivityId, req.ActivityId, StringComparison.OrdinalIgnoreCase));
            if (current == null)
            {
                req.Disabled = false;
                req.Required = true;
                req.ReasonCode = string.IsNullOrEmpty(req.ReasonCode)
                    ? ReasonAndSplitGuard
                    : req.ReasonCode;
                candidates.Items.Add(req);
                NextStepRouteDiagnostics.AndSplitGuard(req.ActivityId, "removed");
                continue;
            }

            if (current.Disabled)
            {
                current.Disabled = false;
                current.Required = true;
                if (string.IsNullOrEmpty(current.ReasonCode))
                    current.ReasonCode = ReasonAndSplitGuard;
                NextStepRouteDiagnostics.AndSplitGuard(current.ActivityId, "disabled");
            }
        }
    }

    static bool PruneRecursive(NextActivityComponent node, HashSet<string> disabled)
    {
        if (node == null)
            return false;
        if (!node.HasChildren)
        {
            return node.Activity != null
                   && disabled.Contains(node.Activity.ActivityId);
        }

        var toRemove = new List<NextActivityComponent>();
        foreach (var child in EnumerateChildren(node).ToList())
        {
            if (PruneRecursive(child, disabled))
                toRemove.Add(child);
        }
        foreach (var c in toRemove)
            node.Remove(c);
        return !node.HasChildren && node.Activity != null && disabled.Contains(node.Activity.ActivityId);
    }

    static bool IsAndSplitGateway(Activity activity)
    {
        var g = activity?.GatewayDetail;
        if (g == null || g.SplitJoinType != GatewaySplitJoinTypeEnum.Split)
            return false;
        return g.DirectionType == GatewayDirectionEnum.AndSplit
               || g.DirectionType == GatewayDirectionEnum.AndSplitMI;
    }

    static IEnumerable<NextActivityComponent> EnumerateChildren(NextActivityComponent node)
    {
        var e = node.GetEnumerator();
        if (e == null)
            yield break;
        while (e.MoveNext())
            yield return e.Current;
    }

    static NextStepRouteContext BuildContext(
        SailClient sail,
        IDictionary<string, string> bag,
        Activity currentActivity,
        ProcessEntity processEntity,
        NextStepRoutePhase phase,
        FlowCommandContext context,
        string activityInstanceId)
    {
        var ctx = new NextStepRouteContext
        {
            Sail = sail,
            Runner = context,
            ProcessId = processEntity?.ProcessId,
            ProcessCode = processEntity?.ProcessCode,
            Version = processEntity?.Version,
            CurrentActivityId = currentActivity?.ActivityId,
            CurrentActivityCode = currentActivity?.ActivityCode,
            ActivityInstanceId = activityInstanceId,
            Conditions = bag,
            Phase = phase
        };
        if (context != null)
        {
            ctx.BizPK = context.BizPK;
            ctx.UserId = context.UserId;
            if (string.IsNullOrEmpty(ctx.ProcessId))
                ctx.ProcessId = context.ProcessId;
            if (string.IsNullOrEmpty(ctx.Version))
                ctx.Version = context.Version;
        }

        FillIdentityFromActivityInstance(sail, ctx, context, activityInstanceId);
        return ctx;
    }

    static void FillIdentityFromActivityInstance(
        SailClient sail,
        NextStepRouteContext routeCtx,
        FlowCommandContext context,
        string activityInstanceId)
    {
        if (!string.IsNullOrWhiteSpace(routeCtx.BizPK))
            return;
        if (string.IsNullOrWhiteSpace(activityInstanceId) || sail == null)
            return;
        try
        {
            var ai = sail.ManagerFactory.Activities.GetById(activityInstanceId);
            if (ai == null)
                return;
            if (string.IsNullOrWhiteSpace(routeCtx.BizPK) && !string.IsNullOrWhiteSpace(ai.BizPK))
            {
                routeCtx.BizPK = ai.BizPK;
                if (context != null && string.IsNullOrWhiteSpace(context.BizPK))
                    context.BizPK = ai.BizPK;
            }
            if (context != null
                && string.IsNullOrWhiteSpace(context.BizChannel)
                && !string.IsNullOrWhiteSpace(ai.BizChannel))
                context.BizChannel = ai.BizChannel;
            if (string.IsNullOrWhiteSpace(routeCtx.ProcessId) && !string.IsNullOrWhiteSpace(ai.ProcessId))
                routeCtx.ProcessId = ai.ProcessId;
            if (string.IsNullOrWhiteSpace(routeCtx.ProcessInstanceId)
                && !string.IsNullOrWhiteSpace(ai.ProcessInstanceId))
                routeCtx.ProcessInstanceId = ai.ProcessInstanceId;
        }
        catch
        {
            /* 补身份失败不阻断路由 */
        }
    }

    static NextStepCandidate CloneCandidate(NextStepCandidate c) => new()
    {
        ActivityId = c.ActivityId,
        Activity = c.Activity,
        Disabled = c.Disabled,
        Required = c.Required,
        ReasonCode = c.ReasonCode
    };

    /// <summary>优先与 context.Conditions 同源；否则新建可写袋。</summary>
    static IDictionary<string, string> ResolveWritableBag(
        IDictionary<string, string> conditions,
        FlowCommandContext context)
    {
        if (conditions == null)
        {
            if (context == null)
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (context.Conditions == null)
                context.Conditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            else if (context.Conditions is not Dictionary<string, string>)
                context.Conditions = new Dictionary<string, string>(
                    context.Conditions, StringComparer.OrdinalIgnoreCase);
            return context.Conditions;
        }

        var bag = EnsureWritableBag(conditions);
        if (context != null
            && ReferenceEquals(conditions, context.Conditions)
            && !ReferenceEquals(bag, context.Conditions))
            context.Conditions = bag;
        return bag;
    }

    static IDictionary<string, string> EnsureWritableBag(IDictionary<string, string> conditions)
    {
        if (conditions == null)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (conditions is Dictionary<string, string>)
            return conditions;
        return new Dictionary<string, string>(conditions, StringComparer.OrdinalIgnoreCase);
    }
}
