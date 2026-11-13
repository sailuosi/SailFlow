using System;
using System.Collections.Generic;
using System.Linq;

namespace sailflow.engine.NextStep;

/// <summary>下一跳节点计算管道实现。</summary>
public sealed class NextStepRoutePipeline : INextStepRoutePipeline
{
    readonly List<(INextStepPrepare Prep, NextStepRouteOptions Opts, int Seq)> _prepares = new();
    readonly List<(INextStepFilter Filter, NextStepRouteOptions Opts, int Seq)> _filters = new();
    readonly List<(IConditionEvaluator Evaluator, NextStepRouteOptions Opts, int Seq)> _evaluators = new();
    int _seq;

    public IReadOnlyList<(IConditionEvaluator Evaluator, NextStepRouteOptions Options)> Evaluators =>
        OrderedEvaluators().Select(x => (x.Evaluator, x.Opts)).ToList();

    public void AddPrepare(INextStepPrepare prepare, NextStepRouteOptions options)
    {
        if (prepare == null)
            throw new ArgumentNullException(nameof(prepare));
        _prepares.Add((prepare, CloneOptions(options), _seq++));
    }

    public void AddFilter(INextStepFilter filter, NextStepRouteOptions options)
    {
        if (filter == null)
            throw new ArgumentNullException(nameof(filter));
        _filters.Add((filter, CloneOptions(options), _seq++));
    }

    public void AddConditionEvaluator(IConditionEvaluator evaluator, NextStepRouteOptions options)
    {
        if (evaluator == null)
            throw new ArgumentNullException(nameof(evaluator));
        _evaluators.Add((evaluator, CloneOptions(options), _seq++));
    }

    public void Prepare(NextStepRouteContext ctx)
    {
        if (ctx == null)
            return;
        foreach (var item in OrderedPrepares())
        {
            if (!NextStepRouteMatch.Matches(
                    item.Opts, ctx.ProcessCode, ctx.ProcessId, ctx.CurrentActivityCode))
                continue;
            if (!item.Prep.IsMatch(ctx))
                continue;
            item.Prep.Prepare(ctx);
        }
    }

    public bool Evaluate(ConditionEvaluateContext ctx)
    {
        if (ctx == null)
            return false;
        if (string.IsNullOrWhiteSpace(ctx.Expression))
            return true;

        var chain = OrderedEvaluators().ToList();
        if (chain.Count == 0)
            return LinqDynamicConditionEvaluator.EvaluateLegacy(ctx.Expression, ctx.Variables);

        foreach (var item in chain)
        {
            if (!NextStepRouteMatch.Matches(
                    item.Opts, ctx.ProcessCode, ctx.ProcessId, ctx.CurrentActivityCode))
                continue;
            if (!item.Evaluator.IsMatch(ctx))
                continue;
            var result = item.Evaluator.TryEvaluate(ctx);
            if (result.HasValue)
                return result.Value;
        }

        // 全链 null / 全未命中 → false（N14 / V11）
        return false;
    }

    public void Filter(NextStepRouteContext ctx, NextStepCandidateList candidates)
    {
        if (ctx == null || candidates == null)
            return;
        foreach (var item in OrderedFilters())
        {
            if (!NextStepRouteMatch.Matches(
                    item.Opts, ctx.ProcessCode, ctx.ProcessId, ctx.CurrentActivityCode))
                continue;
            if (!item.Filter.IsMatch(ctx))
                continue;
            item.Filter.Filter(ctx, candidates);
        }
    }

    /// <summary>启用管道时挂默认 LINQ 兜底（Order=100, Match=All），若尚未注册任何 Evaluator。</summary>
    public void EnsureDefaultLinqEvaluator()
    {
        if (_evaluators.Count > 0)
            return;
        AddConditionEvaluator(
            new LinqDynamicConditionEvaluator(),
            new NextStepRouteOptions { Match = NextStepMatch.All, Order = 100 });
    }

    IEnumerable<(INextStepPrepare Prep, NextStepRouteOptions Opts, int Seq)> OrderedPrepares() =>
        _prepares.OrderBy(x => x.Opts.Order ?? x.Seq).ThenBy(x => x.Seq);

    IEnumerable<(INextStepFilter Filter, NextStepRouteOptions Opts, int Seq)> OrderedFilters() =>
        _filters.OrderBy(x => x.Opts.Order ?? x.Seq).ThenBy(x => x.Seq);

    IEnumerable<(IConditionEvaluator Evaluator, NextStepRouteOptions Opts, int Seq)> OrderedEvaluators() =>
        _evaluators.OrderBy(x => x.Opts.Order ?? x.Seq).ThenBy(x => x.Seq);

    static NextStepRouteOptions CloneOptions(NextStepRouteOptions source)
    {
        if (source == null)
            return new NextStepRouteOptions { Match = NextStepMatch.All };
        return new NextStepRouteOptions
        {
            Match = source.Match,
            ProcessCodes = source.ProcessCodes,
            ProcessIds = source.ProcessIds,
            ProcessCodePatterns = source.ProcessCodePatterns,
            ActivityCodes = source.ActivityCodes,
            Order = source.Order
        };
    }
}
