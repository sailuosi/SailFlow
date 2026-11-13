using System;
using System.Collections.Generic;
using sailflow.engine.Client;
using sailflow.engine.NextStep;

namespace sailflow.engine.Hosting;

/// <summary>
/// <c>UseNextStepRoute</c> 子 Builder：IfCondition* / AddConditionEvaluator / AddFilter。
/// </summary>
public sealed class NextStepRouteBuilder
{
    readonly List<Action<NextStepRoutePipeline>> _actions = new();
    int _registerSeq;
    bool _skipDefaultLinq;

    /// <summary>
    /// 保留 API。默认 LINQ 由 <c>UseSailRuntimeDefaults</c> 挂载；本方法仅跳过已废弃的 engine 内 Ensure。
    /// </summary>
    public NextStepRouteBuilder WithoutDefaultLinqEvaluator()
    {
        _skipDefaultLinq = true;
        return this;
    }

    #region IfCondition (Prepare)

    public NextStepRouteBuilder IfCondition<TPrepare>(
        Action<NextStepRouteOptions> configure = null)
        where TPrepare : class, INextStepPrepare, new()
    {
        var opts = NewOptions(configure);
        _actions.Add(p => p.AddPrepare(new TPrepare(), opts));
        return this;
    }

    public NextStepRouteBuilder IfCondition(
        INextStepPrepare prepare,
        Action<NextStepRouteOptions> configure = null)
    {
        if (prepare == null)
            throw new ArgumentNullException(nameof(prepare));
        var opts = NewOptions(configure);
        _actions.Add(p => p.AddPrepare(prepare, opts));
        return this;
    }

    /// <summary>字面量键值 → LiteralPrepare。</summary>
    public NextStepRouteBuilder IfCondition(
        string name,
        string value,
        Action<NextStepRouteOptions> configure = null)
    {
        var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [name ?? string.Empty] = value ?? string.Empty
        };
        return IfCondition(new LiteralNextStepPrepare(bag), configure);
    }

    /// <summary>委托 Prepare（Contribute）。</summary>
    public NextStepRouteBuilder IfCondition(
        Action<NextStepRouteContext> contribute,
        Action<NextStepRouteOptions> configure = null)
    {
        if (contribute == null)
            throw new ArgumentNullException(nameof(contribute));
        return IfCondition(new DelegateNextStepPrepare(contribute), configure);
    }

    /// <summary>链式糖：ForProcess / ForActivity / Matching / IfCondition。</summary>
    public NextStepRouteBuilder IfCondition(Action<NextStepConditionChain> configure)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));
        var chain = new NextStepConditionChain(this);
        configure(chain);
        chain.Commit();
        return this;
    }

    #endregion

    #region Evaluator

    public NextStepRouteBuilder AddConditionEvaluator<TEvaluator>(
        Action<NextStepRouteOptions> configure = null)
        where TEvaluator : class, IConditionEvaluator, new()
    {
        var opts = NewOptions(configure);
        _actions.Add(p => p.AddConditionEvaluator(new TEvaluator(), opts));
        return this;
    }

    public NextStepRouteBuilder AddConditionEvaluator(
        IConditionEvaluator evaluator,
        Action<NextStepRouteOptions> configure = null)
    {
        if (evaluator == null)
            throw new ArgumentNullException(nameof(evaluator));
        var opts = NewOptions(configure);
        _actions.Add(p => p.AddConditionEvaluator(evaluator, opts));
        return this;
    }

    /// <summary>与 <see cref="AddConditionEvaluator{TEvaluator}"/> 等价糖。</summary>
    public NextStepRouteBuilder UseConditionEvaluator<TEvaluator>(
        Action<NextStepRouteOptions> configure = null)
        where TEvaluator : class, IConditionEvaluator, new()
        => AddConditionEvaluator<TEvaluator>(configure);

    public NextStepRouteBuilder UseConditionEvaluator(
        IConditionEvaluator evaluator,
        Action<NextStepRouteOptions> configure = null)
        => AddConditionEvaluator(evaluator, configure);

    #endregion

    #region Filter

    public NextStepRouteBuilder AddFilter<TFilter>(
        Action<NextStepRouteOptions> configure = null)
        where TFilter : class, INextStepFilter, new()
    {
        var opts = NewOptions(configure);
        _actions.Add(p => p.AddFilter(new TFilter(), opts));
        return this;
    }

    public NextStepRouteBuilder AddFilter(
        INextStepFilter filter,
        Action<NextStepRouteOptions> configure = null)
    {
        if (filter == null)
            throw new ArgumentNullException(nameof(filter));
        var opts = NewOptions(configure);
        _actions.Add(p => p.AddFilter(filter, opts));
        return this;
    }

    #endregion

    internal void Apply(NextStepRoutePipeline pipeline, SailClient client)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        foreach (var action in _actions)
            action(pipeline);
        if (!_skipDefaultLinq)
            pipeline.EnsureDefaultLinqEvaluator();
    }

    NextStepRouteOptions NewOptions(Action<NextStepRouteOptions> configure)
    {
        var opts = new NextStepRouteOptions();
        configure?.Invoke(opts);
        if (opts.Order == null)
            opts.Order = _registerSeq;
        _registerSeq++;
        // 未配任何流程维且未显式 Match → 默认 All，便于字面量糖全局生效
        if (opts.Match == NextStepMatch.Whitelist
            && (opts.ProcessCodes == null || opts.ProcessCodes.Length == 0)
            && (opts.ProcessIds == null || opts.ProcessIds.Length == 0)
            && (opts.ProcessCodePatterns == null || opts.ProcessCodePatterns.Length == 0)
            && configure == null)
        {
            opts.Match = NextStepMatch.All;
        }
        return opts;
    }

    internal NextStepRouteOptions NewOptionsForChain(Action<NextStepRouteOptions> configure)
        => NewOptions(configure);

    internal void Enqueue(Action<NextStepRoutePipeline> action)
        => _actions.Add(action);
}

/// <summary>链式 IfCondition 糖。</summary>
public sealed class NextStepConditionChain
{
    readonly NextStepRouteBuilder _owner;
    readonly NextStepRouteOptions _opts = new() { Match = NextStepMatch.Whitelist };
    readonly Dictionary<string, string> _literals = new(StringComparer.OrdinalIgnoreCase);
    readonly List<string> _codes = new();
    readonly List<string> _patterns = new();
    readonly List<string> _activities = new();
    bool _committed;

    internal NextStepConditionChain(NextStepRouteBuilder owner) => _owner = owner;

    public NextStepConditionChain ForProcess(params string[] processCodes)
    {
        if (processCodes != null)
            _codes.AddRange(processCodes);
        return this;
    }

    public NextStepConditionChain ForActivity(params string[] activityCodes)
    {
        if (activityCodes != null)
            _activities.AddRange(activityCodes);
        return this;
    }

    public NextStepConditionChain Matching(params string[] processCodePatterns)
    {
        if (processCodePatterns != null)
            _patterns.AddRange(processCodePatterns);
        return this;
    }

    public NextStepConditionChain WithOrder(int order)
    {
        _opts.Order = order;
        return this;
    }

    public NextStepConditionChain MatchAll()
    {
        _opts.Match = NextStepMatch.All;
        return this;
    }

    public NextStepConditionChain IfCondition(string name, string value)
    {
        if (!string.IsNullOrEmpty(name))
            _literals[name] = value ?? string.Empty;
        return this;
    }

    internal void Commit()
    {
        if (_committed)
            return;
        _committed = true;
        if (_codes.Count > 0)
            _opts.ProcessCodes = _codes.ToArray();
        if (_patterns.Count > 0)
            _opts.ProcessCodePatterns = _patterns.ToArray();
        if (_activities.Count > 0)
            _opts.ActivityCodes = _activities.ToArray();
        if (_opts.Match == NextStepMatch.Whitelist
            && (_opts.ProcessCodes == null || _opts.ProcessCodes.Length == 0)
            && (_opts.ProcessIds == null || _opts.ProcessIds.Length == 0)
            && (_opts.ProcessCodePatterns == null || _opts.ProcessCodePatterns.Length == 0))
        {
            _opts.Match = NextStepMatch.All;
        }

        var bag = new Dictionary<string, string>(_literals, StringComparer.OrdinalIgnoreCase);
        var prepare = new LiteralNextStepPrepare(bag);
        var opts = _owner.NewOptionsForChain(o =>
        {
            o.Match = _opts.Match;
            o.ProcessCodes = _opts.ProcessCodes;
            o.ProcessIds = _opts.ProcessIds;
            o.ProcessCodePatterns = _opts.ProcessCodePatterns;
            o.ActivityCodes = _opts.ActivityCodes;
            if (_opts.Order != null)
                o.Order = _opts.Order;
        });
        _owner.Enqueue(p => p.AddPrepare(prepare, opts));
    }
}

sealed class LiteralNextStepPrepare : NextStepPrepare
{
    public LiteralNextStepPrepare(IDictionary<string, string> bag)
    {
        if (bag == null)
            return;
        foreach (var kv in bag)
            IfCondition(kv.Key, kv.Value);
    }
}

sealed class DelegateNextStepPrepare : NextStepPrepare
{
    readonly Action<NextStepRouteContext> _contribute;

    public DelegateNextStepPrepare(Action<NextStepRouteContext> contribute)
        => _contribute = contribute;

    protected override void Contribute(NextStepRouteContext ctx) => _contribute(ctx);
}
