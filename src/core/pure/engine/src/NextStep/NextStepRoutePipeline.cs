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
            var impl = item.Prep.GetType().Name;
            if (!NextStepRouteMatch.Matches(
                    item.Opts, ctx.ProcessCode, ctx.ProcessId, ctx.CurrentActivityCode))
            {
                NextStepRouteDiagnostics.OptionsSkip("Prepare", impl, ctx.ProcessCode, ctx.CurrentActivityCode);
                continue;
            }
            if (!item.Prep.IsMatch(ctx))
            {
                NextStepRouteDiagnostics.IsMatchSkip("Prepare", impl);
                continue;
            }

            var before = SnapshotKeys(ctx.Conditions);
            item.Prep.Prepare(ctx);
            LogWroteKeys(impl, before, ctx.Conditions);
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

        var anyTried = false;
        foreach (var item in chain)
        {
            var impl = item.Evaluator.GetType().Name;
            if (!NextStepRouteMatch.Matches(
                    item.Opts, ctx.ProcessCode, ctx.ProcessId, ctx.CurrentActivityCode))
            {
                NextStepRouteDiagnostics.OptionsSkip("Evaluator", impl, ctx.ProcessCode, ctx.CurrentActivityCode);
                continue;
            }
            if (!item.Evaluator.IsMatch(ctx))
            {
                NextStepRouteDiagnostics.IsMatchSkip("Evaluator", impl);
                continue;
            }

            anyTried = true;
            var result = item.Evaluator.TryEvaluate(ctx);
            if (result.HasValue)
                return result.Value;

            NextStepRouteDiagnostics.EvaluatorNull(impl);
        }

        if (anyTried || chain.Count > 0)
            NextStepRouteDiagnostics.EvaluatorAllNull();
        return false;
    }

    public void Filter(NextStepRouteContext ctx, NextStepCandidateList candidates)
    {
        if (ctx == null || candidates == null)
            return;
        foreach (var item in OrderedFilters())
        {
            var impl = item.Filter.GetType().Name;
            if (!NextStepRouteMatch.Matches(
                    item.Opts, ctx.ProcessCode, ctx.ProcessId, ctx.CurrentActivityCode))
            {
                NextStepRouteDiagnostics.OptionsSkip("Filter", impl, ctx.ProcessCode, ctx.CurrentActivityCode);
                continue;
            }
            if (!item.Filter.IsMatch(ctx))
            {
                NextStepRouteDiagnostics.IsMatchSkip("Filter", impl);
                continue;
            }
            item.Filter.Filter(ctx, candidates);
            NextStepRouteDiagnostics.FilterApplied(impl);
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

    static Dictionary<string, string> SnapshotKeys(IDictionary<string, string> bag)
    {
        if (bag == null || bag.Count == 0)
            return null;
        return new Dictionary<string, string>(bag, StringComparer.OrdinalIgnoreCase);
    }

    static void LogWroteKeys(string impl, Dictionary<string, string> before, IDictionary<string, string> after)
    {
        if (after == null)
            return;
        foreach (var kv in after)
        {
            if (before != null
                && before.TryGetValue(kv.Key, out var old)
                && string.Equals(old, kv.Value, StringComparison.Ordinal))
                continue;
            NextStepRouteDiagnostics.PrepareWrote(impl, kv.Key, kv.Value);
        }
    }

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
