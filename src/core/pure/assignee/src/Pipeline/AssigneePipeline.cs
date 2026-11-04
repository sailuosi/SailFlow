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

    /// <summary>
    /// 空集选型键：优先匹配 <see cref="IAssigneeEmptyAction.Key"/>；
    /// 未注册 EmptyAction 时再按同名（或产品码映射）<see cref="IAssigneeResolver.Type"/> 调 Resolver；
    /// 仍无结果则默认 <see cref="EmptyActionKeys.Reject"/>。
    /// </summary>
    public Func<AssigneeFilterContext, string?>? EmptyActionSelector { get; set; }

    /// <summary>
    /// 可选：空人/Filter 策略袋（如 NodePolicy → userIds）。在 Empty / Filter 前写入 <see cref="AssigneeFilterContext.PolicyBag"/>。
    /// </summary>
    public Func<AssigneeFilterContext, IDictionary<string, object?>?>? PolicyBagReader { get; set; }

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
        filterCtx.PolicyBag = PolicyBagReader?.Invoke(filterCtx) ?? filterCtx.PolicyBag;

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

        // 1) 注册的 EmptyAction（含产品码别名）
        if (TryGetEmptyAction(key, out var action) && action != null)
            return ApplyEmptyResult(ctx, action.Handle(ctx));

        // 2) 无 EmptyAction → 调对应 Resolver（同名 Type，或 3/users→FixedUsers）；有人则采用
        var resolverType = MapEmptyKeyToResolverType(key);
        if (resolverType != null
            && TryResolveAsEmptyFallback(ctx, resolverType, out var fromResolver)
            && fromResolver.Count > 0)
        {
            ctx.Performers = fromResolver;
            ctx.Source = PerformerSources.EmptyAction;
            Trace.TraceInformation(
                "[Assignee] Empty key '{0}' → Resolver '{1}' yielded {2} performer(s)",
                key, resolverType, fromResolver.Count);
            return true;
        }

        if (!string.Equals(key, EmptyActionKeys.Reject, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(key, EmptyActionKeys.RejectProductCode, StringComparison.Ordinal))
        {
            Trace.TraceInformation(
                "[Assignee] Empty key '{0}' has no EmptyAction/Resolver (or resolver empty); default Reject",
                key);
        }

        // 3) 默认 RejectEmptyAction
        if (!TryGetEmptyAction(EmptyActionKeys.Reject, out var reject) || reject == null)
            reject = new RejectEmptyAction();
        return ApplyEmptyResult(ctx, reject.Handle(ctx));
    }

    /// <summary>
    /// 空人选型键 → 要调的 Resolver.Type；reject/skip 等纯 EmptyAction 键返回 null。
    /// </summary>
    static string? MapEmptyKeyToResolverType(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        // 纯处置键：不走 Resolver
        if (string.Equals(key, EmptyActionKeys.Reject, StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, EmptyActionKeys.RejectProductCode, StringComparison.Ordinal)
            || string.Equals(key, EmptyActionKeys.Skip, StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, EmptyActionKeys.SkipProductCode, StringComparison.Ordinal))
            return null;

        // 产品「指定人员」→ 内置 FixedUsers（Params 来自 PolicyBag）
        if (string.Equals(key, EmptyActionKeys.Users, StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, EmptyActionKeys.UsersProductCode, StringComparison.Ordinal))
            return AssigneeResolveTypeCodes.FixedUsers;

        // admin/4：无内置 ProcessAdmin Resolver 时不映射；靠注册 EmptyAction 或自定义 Type
        if (string.Equals(key, EmptyActionKeys.Admin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, EmptyActionKeys.AdminProductCode, StringComparison.Ordinal))
            return null;

        // 其它：按同名 Resolver Type（如 RoleGroup / JobRelative / 业务 Type）
        return key.Trim();
    }

    bool TryGetEmptyAction(string key, out IAssigneeEmptyAction? action)
    {
        action = null;
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (_emptyActions.TryGetValue(key, out action) && action != null)
            return true;

        // 产品码别名：1/2/3/4 → skip/reject/users/admin
        var alias = key switch
        {
            EmptyActionKeys.RejectProductCode => EmptyActionKeys.Reject,
            EmptyActionKeys.SkipProductCode => EmptyActionKeys.Skip,
            EmptyActionKeys.UsersProductCode => EmptyActionKeys.Users,
            EmptyActionKeys.AdminProductCode => EmptyActionKeys.Admin,
            _ => null
        };
        if (alias != null && _emptyActions.TryGetValue(alias, out action) && action != null)
            return true;

        action = null;
        return false;
    }

    bool TryResolveAsEmptyFallback(AssigneeFilterContext ctx, string resolverType, out PerformerList performers)
    {
        performers = new PerformerList();
        if (string.IsNullOrWhiteSpace(resolverType))
            return false;
        if (!Registry.TryGetResolver(resolverType, out var resolver) || resolver == null)
            return false;
        if (string.Equals(resolver.Type, AssigneeResolveTypeCodes.Fallback, StringComparison.OrdinalIgnoreCase))
            return false; // 空人路径不再套 Fallback，防递归

        WireSail(resolver);
        var config = BuildEmptyResolverConfig(ctx, resolverType);
        try
        {
            performers = resolver.Resolve(new AssigneeResolveContext
            {
                Request = ctx.Request,
                Config = config
            }) ?? new PerformerList();
            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning(
                "[Assignee] Empty → Resolver '{0}' threw: {1}; will default Reject",
                resolverType, ex.Message);
            performers = new PerformerList();
            return false;
        }
    }

    /// <summary>
    /// 空人调 Resolver 的 ConfigPair：优先 PolicyBag.emptyResolveParams / params，否则透传 PolicyBag。
    /// </summary>
    static ConfigPair BuildEmptyResolverConfig(AssigneeFilterContext ctx, string resolverType)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var bag = ctx.PolicyBag;
        if (bag != null)
        {
            if (TryCoerceParamsDict(bag, "emptyResolveParams", out var nested)
                || TryCoerceParamsDict(bag, "params", out nested))
            {
                foreach (var kv in nested!)
                    parameters[kv.Key] = kv.Value;
            }
            else
            {
                foreach (var kv in bag)
                    parameters[kv.Key] = kv.Value;
            }
        }

        return ConfigPair.Create(resolverType, parameters);
    }

    static bool TryCoerceParamsDict(
        IDictionary<string, object?> bag,
        string key,
        out IDictionary<string, object?>? dict)
    {
        dict = null;
        if (!bag.TryGetValue(key, out var raw) || raw == null)
            return false;
        if (raw is IDictionary<string, object?> d1)
        {
            dict = new Dictionary<string, object?>(d1, StringComparer.OrdinalIgnoreCase);
            return true;
        }

        if (raw is IDictionary<string, object> d2)
        {
            dict = d2.ToDictionary(kv => kv.Key, kv => (object?)kv.Value, StringComparer.OrdinalIgnoreCase);
            return true;
        }

        return false;
    }

    bool ApplyEmptyResult(AssigneeFilterContext ctx, EmptyActionResult result)
    {
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
            // 优先与发送主路径同事务（AssigneeInvokeRequest.Kit）；无 kit 时 Store 自开事务
            AnchorStore.Enqueue(Sail, enriched, ctx.Request?.Kit);
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
