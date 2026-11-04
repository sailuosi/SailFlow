using System.Diagnostics;
using saiflow.assignee.Adapt;
using saiflow.assignee.Builtin;
using saiflow.engine.Assignee;
using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.engine.Runtime;

namespace saiflow.assignee;

/// <summary>
/// 办理人管道：ConfigPair.Type 分发 +（R10）Filter / EmptyAction / 自动审批锚点入队。
/// </summary>
public class AssigneePipeline : IAssigneePipeline
{
    readonly List<(int Order, int Seq, IAssigneeResolveFilter Filter)> _filters = new();
    readonly Dictionary<string, IAssigneeEmptyAction> _emptyActions =
        new(StringComparer.OrdinalIgnoreCase);
    int _filterSeq;

    /// <summary>空集选型：返回 EmptyAction.Key；默认 <see cref="EmptyActionKeys.Reject"/>。</summary>
    public Func<AssigneeFilterContext, string?>? EmptyActionSelector { get; set; }

    public IAutoApproveAnchorStore? AnchorStore { get; set; }

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
        RegisterEmptyAction(new RejectEmptyAction());
    }

    public void AddFilter(IAssigneeResolveFilter filter, int? order = null)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var o = order ?? _filterSeq;
        _filters.Add((o, _filterSeq++, filter));
    }

    public void RegisterEmptyAction(IAssigneeEmptyAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (string.IsNullOrWhiteSpace(action.Key))
            throw new ArgumentException("IAssigneeEmptyAction.Key is required.", nameof(action));
        _emptyActions[action.Key.Trim()] = action;
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

        var resolveCtx = new AssigneeResolveContext
        {
            Request = request,
            Config = config
        };

        var resolved = resolver.Resolve(resolveCtx) ?? new PerformerList();
        var source = string.Equals(config.Type, AssigneeResolveTypeCodes.Specific, StringComparison.OrdinalIgnoreCase)
            ? PerformerSources.Specific
            : PerformerSources.Resolver;

        var filterCtx = new AssigneeFilterContext
        {
            Request = request,
            Source = source,
            ResolveType = config.Type,
            Performers = resolved
        };

        if (filterCtx.Performers.Count == 0)
        {
            if (!TryApplyEmptyAction(filterCtx))
                return false;
        }
        else
        {
            ApplyFilters(filterCtx);
            if (filterCtx.Performers.Count == 0)
            {
                // Filter 后变空：再走一次 EmptyAction（§11.2）
                if (!TryApplyEmptyAction(filterCtx))
                    return false;
            }
        }

        if (filterCtx.RewroteSpecific)
        {
            Trace.TraceInformation(
                "[Assignee] RewroteSpecific=true Activity={0} ResolveType={1}",
                request.ActivityId, config.Type);
        }

        FlushAnchors(filterCtx);
        performers = filterCtx.Performers ?? new PerformerList();
        return performers.Count > 0;
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

    void ApplyFilters(AssigneeFilterContext ctx)
    {
        foreach (var (_, _, filter) in _filters.OrderBy(x => x.Order).ThenBy(x => x.Seq))
        {
            filter.Apply(ctx);
            ctx.Performers ??= new PerformerList();
        }
    }

    bool TryApplyEmptyAction(AssigneeFilterContext ctx)
    {
        var key = EmptyActionSelector?.Invoke(ctx)?.Trim();
        if (string.IsNullOrEmpty(key))
            key = EmptyActionKeys.Reject;

        if (!_emptyActions.TryGetValue(key, out var action))
        {
            // 产品码别名：2→reject 等
            if (string.Equals(key, EmptyActionKeys.RejectProductCode, StringComparison.Ordinal))
                _emptyActions.TryGetValue(EmptyActionKeys.Reject, out action);
            else if (string.Equals(key, EmptyActionKeys.SkipProductCode, StringComparison.Ordinal))
                _emptyActions.TryGetValue(EmptyActionKeys.Skip, out action);
            else if (string.Equals(key, EmptyActionKeys.UsersProductCode, StringComparison.Ordinal))
                _emptyActions.TryGetValue(EmptyActionKeys.Users, out action);
            else if (string.Equals(key, EmptyActionKeys.AdminProductCode, StringComparison.Ordinal))
                _emptyActions.TryGetValue(EmptyActionKeys.Admin, out action);
        }

        action ??= _emptyActions.TryGetValue(EmptyActionKeys.Reject, out var reject) ? reject : null;
        if (action == null)
            return false;

        var result = action.Handle(ctx);
        if (result.IsReject)
        {
            throw new WfAssigneeException(
                result.ReasonCode ?? AssigneeReasonCodes.AssigneeEmpty,
                result.Message ?? "Assignee empty rejected by EmptyAction.");
        }

        ctx.Performers = result.Performers ?? new PerformerList();
        ctx.Source = PerformerSources.EmptyAction;
        // Mediator 仍要求 Performers.Count>0；仅锚点无办理人 → false
        return ctx.Performers.Count > 0;
    }

    void FlushAnchors(AssigneeFilterContext ctx)
    {
        if (ctx.Anchors.Count == 0 || AnchorStore == null || Sail == null)
            return;

        var enriched = ctx.Anchors.Select(a => new AutoApproveAnchor
        {
            StrategyCode = a.StrategyCode,
            Reason = a.Reason,
            ProcessId = a.ProcessId ?? ctx.Request.ProcessId,
            Version = a.Version ?? ctx.Request.Version,
            ProcessInstanceId = a.ProcessInstanceId ?? ctx.Request.ProcessInstanceId,
            ActivityId = a.ActivityId ?? ctx.Request.ActivityId,
            ActivityCode = a.ActivityCode ?? ctx.Request.ActivityCode,
            ActivityInstanceId = a.ActivityInstanceId,
            TaskId = a.TaskId,
            PreferUserId = a.PreferUserId,
            PreferUserName = a.PreferUserName,
            Extra = a.Extra
        }).ToList();

        try
        {
            AnchorStore.Enqueue(Sail, enriched);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("[Assignee] AnchorStore.Enqueue failed: {0}", ex.Message);
            throw;
        }
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
