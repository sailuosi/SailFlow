using sailflow.assignee.Builtin;
using sailflow.assignee.Job;
using sailflow.assignee.Store;
using sailflow.engine.Client;
using sailflow.engine.Common;

namespace sailflow.assignee.Hosting;

/// <summary>
/// R10：UseAssign 子 Builder。AddFilter 注册序=执行序；AddResolver 同 Type 后写覆盖；
/// AddEmptyAction 按 Key 选型。
/// </summary>
public sealed class AssigneeAssignBuilder
{
    readonly List<Action<AssigneePipeline>> _actions = new();
    int _filterRegisterSeq;
    AutoApproveAnchorOptions? _autoApproveOptions;
    bool _useAutoApproveJob;

    /// <summary>优先泛型：无参构造并注册 Resolver。</summary>
    public AssigneeAssignBuilder AddResolver<TResolver>()
        where TResolver : class, IAssigneeResolver, new()
    {
        _actions.Add(p => p.Registry.Register(new TResolver()));
        return this;
    }

    public AssigneeAssignBuilder AddResolver(IAssigneeResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _actions.Add(p => p.Registry.Register(resolver));
        return this;
    }

    public AssigneeAssignBuilder AddResolver(
        string type,
        Func<AssigneeResolveContext, PerformerList> resolve)
    {
        ArgumentNullException.ThrowIfNull(resolve);
        return AddResolver(new DelegateAssigneeResolver(type, resolve));
    }

    public AssigneeAssignBuilder AddLeaveJudge<TJudge>()
        where TJudge : class, IActivityLeaveJudge, new()
    {
        _actions.Add(p => p.Registry.Register(new TJudge()));
        return this;
    }

    public AssigneeAssignBuilder AddLeaveJudge(IActivityLeaveJudge judge)
    {
        ArgumentNullException.ThrowIfNull(judge);
        _actions.Add(p => p.Registry.Register(judge));
        return this;
    }

    public AssigneeAssignBuilder AddFilter<TFilter>(
        Action<AssigneeFilterRegistrationOptions>? configure = null)
        where TFilter : class, IAssigneeResolveFilter, new()
    {
        var opts = new AssigneeFilterRegistrationOptions();
        configure?.Invoke(opts);
        var order = opts.Order ?? _filterRegisterSeq;
        _filterRegisterSeq++;
        _actions.Add(p => p.AddFilter(new TFilter(), order));
        return this;
    }

    public AssigneeAssignBuilder AddFilter(
        IAssigneeResolveFilter filter,
        Action<AssigneeFilterRegistrationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var opts = new AssigneeFilterRegistrationOptions();
        configure?.Invoke(opts);
        var order = opts.Order ?? _filterRegisterSeq++;
        _actions.Add(p => p.AddFilter(filter, order));
        return this;
    }

    public AssigneeAssignBuilder AddEmptyAction<TAction>()
        where TAction : class, IAssigneeEmptyAction, new()
    {
        _actions.Add(p => p.RegisterEmptyAction(new TAction()));
        return this;
    }

    public AssigneeAssignBuilder AddEmptyAction(IAssigneeEmptyAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _actions.Add(p => p.RegisterEmptyAction(action));
        return this;
    }

    /// <summary>
    /// 空集选型委托：返回 EmptyAction.Key 或 Resolver.Type（如 FixedUsers / 业务 Type）；
    /// 无 EmptyAction 时管道按同名调 Resolver，调不到或仍空 → 默认 Reject。
    /// </summary>
    public AssigneeAssignBuilder UseEmptyActionSelector(Func<AssigneeFilterContext, string?> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        _actions.Add(p => p.EmptyActionSelector = selector);
        return this;
    }

    /// <summary>空人/Filter 策略袋（如 NodePolicy 的 userIds），供 Empty → Resolver 的 Params。</summary>
    public AssigneeAssignBuilder UsePolicyBag(Func<AssigneeFilterContext, IDictionary<string, object?>?> reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        _actions.Add(p => p.PolicyBagReader = reader);
        return this;
    }

    /// <summary>启用自动审批锚点 Job（HookOutbox Kind=AssigneeAutoApprove）。</summary>
    public AssigneeAssignBuilder UseAutoApproveJob(Action<AutoApproveAnchorOptions>? configure = null)
    {
        _useAutoApproveJob = true;
        _autoApproveOptions = new AutoApproveAnchorOptions();
        configure?.Invoke(_autoApproveOptions);
        return this;
    }

    public AssigneeAssignBuilder UseAnchorStore(IAutoApproveAnchorStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _actions.Add(p => p.AnchorStore = store);
        return this;
    }

    internal void Apply(AssigneePipeline pipeline, SailClient client)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        foreach (var action in _actions)
            action(pipeline);

        if (_useAutoApproveJob)
        {
            pipeline.AnchorStore ??= new HookOutboxAutoApproveAnchorStore();
            if (_autoApproveOptions?.AutoStart == true)
            {
                System.Diagnostics.Trace.TraceWarning(
                    "[Assignee] UseAutoApproveJob(AutoStart=true): ensure Host JobWorker is NOT also draining "
                    + "AssigneeAutoApprove (CAS 防双领，但仍浪费 reclaim/空转). Prefer 单消费者.");
                client.AssigneeAutoApproveDrainCts?.Cancel();
                client.AssigneeAutoApproveDrainCts?.Dispose();
                client.AssigneeAutoApproveDrainCts =
                    AutoApproveAnchorDrainLoop.StartBackground(client, _autoApproveOptions);
            }
        }
    }

    sealed class DelegateAssigneeResolver : IAssigneeResolver
    {
        readonly Func<AssigneeResolveContext, PerformerList> _resolve;

        public DelegateAssigneeResolver(string type, Func<AssigneeResolveContext, PerformerList> resolve)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            _resolve = resolve;
        }

        public string Type { get; }
        public PerformerList Resolve(AssigneeResolveContext ctx) => _resolve(ctx);
    }
}
