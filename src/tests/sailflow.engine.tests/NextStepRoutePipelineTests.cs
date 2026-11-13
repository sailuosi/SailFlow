using sailflow.engine.Common;
using sailflow.engine.NextStep;
using sailflow.engine.Xpdl;
using Xunit;

namespace sailflow.engine.tests;

public sealed class NextStepRoutePipelineTests
{
    [Fact]
    public void Prepare_later_overwrite_same_key()
    {
        var pipeline = new NextStepRoutePipeline();
        pipeline.AddPrepare(
            new LiteralBagPrepare(("amount", "1")),
            new NextStepRouteOptions { Match = NextStepMatch.All, Order = 10 });
        pipeline.AddPrepare(
            new LiteralBagPrepare(("amount", "2")),
            new NextStepRouteOptions { Match = NextStepMatch.All, Order = 20 });

        var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        pipeline.Prepare(new NextStepRouteContext
        {
            ProcessCode = "EXPENSE",
            Conditions = bag,
            Phase = NextStepRoutePhase.Preview
        });

        Assert.Equal("2", bag["amount"]);
    }

    [Fact]
    public void Prepare_Whitelist_miss_skips_write()
    {
        var pipeline = new NextStepRoutePipeline();
        pipeline.AddPrepare(
            new LiteralBagPrepare(("flag", "1")),
            new NextStepRouteOptions
            {
                Match = NextStepMatch.Whitelist,
                ProcessCodes = new[] { "EXPENSE" }
            });

        var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        pipeline.Prepare(new NextStepRouteContext
        {
            ProcessCode = "HR",
            Conditions = bag
        });

        Assert.Empty(bag);
    }

    [Fact]
    public void Evaluate_null_then_true_uses_second()
    {
        var pipeline = new NextStepRoutePipeline();
        pipeline.AddConditionEvaluator(
            new FixedEvaluator(null),
            new NextStepRouteOptions { Match = NextStepMatch.All, Order = 1 });
        pipeline.AddConditionEvaluator(
            new FixedEvaluator(true),
            new NextStepRouteOptions { Match = NextStepMatch.All, Order = 2 });

        var ok = pipeline.Evaluate(new ConditionEvaluateContext
        {
            Expression = "1==1",
            Variables = new Dictionary<string, string>()
        });
        Assert.True(ok);
    }

    [Fact]
    public void Evaluate_false_stops_chain()
    {
        var pipeline = new NextStepRoutePipeline();
        pipeline.AddConditionEvaluator(
            new FixedEvaluator(false),
            new NextStepRouteOptions { Match = NextStepMatch.All, Order = 1 });
        pipeline.AddConditionEvaluator(
            new FixedEvaluator(true),
            new NextStepRouteOptions { Match = NextStepMatch.All, Order = 2 });

        Assert.False(pipeline.Evaluate(new ConditionEvaluateContext
        {
            Expression = "x",
            Variables = new Dictionary<string, string>()
        }));
    }

    [Fact]
    public void Evaluate_all_null_returns_false()
    {
        var pipeline = new NextStepRoutePipeline();
        pipeline.AddConditionEvaluator(
            new FixedEvaluator(null),
            new NextStepRouteOptions { Match = NextStepMatch.All });

        Assert.False(pipeline.Evaluate(new ConditionEvaluateContext
        {
            Expression = "x",
            Variables = new Dictionary<string, string>()
        }));
    }

    [Fact]
    public void AndSplitGuard_restores_disabled_required()
    {
        var pipeline = new NextStepRoutePipeline();
        pipeline.AddFilter(new DisableAllFilter(), new NextStepRouteOptions { Match = NextStepMatch.All });

        var gw = new Activity
        {
            ActivityId = "and1",
            ActivityType = ActivityTypeEnum.GatewayNode,
            GatewayDetail = new GatewayDetail
            {
                SplitJoinType = GatewaySplitJoinTypeEnum.Split,
                DirectionType = GatewayDirectionEnum.AndSplit
            }
        };
        var leafA = new Activity { ActivityId = "a1", ActivityName = "A" };
        var leafB = new Activity { ActivityId = "b1", ActivityName = "B" };

        var root = new NextActivityRouter("root", null, null);
        var router = new NextActivityRouter("and", null, gw);
        router.Add(new NextActivityItem("a", null, leafA));
        router.Add(new NextActivityItem("b", null, leafB));
        root.Add(router);

        var matched = NextActivityMatchedResult.CreateNextActivityMatchedResultObject(
            NextActivityMatchedType.Successed, root);

        // ApplyFilter needs Sail with pipeline — use direct collect + filter + guard path via runtime helper
        var sail = new sailflow.engine.Client.SailClient();
        sail.useNextStepRoute(pipeline);

        var list = NextStepRouteRuntime.ApplyFilter(
            sail, matched, currentActivity: null, processEntity: null,
            NextStepRoutePhase.Preview);

        Assert.NotNull(list);
        Assert.Equal(2, list.Items.Count);
        Assert.All(list.Items, c => Assert.False(c.Disabled));
        Assert.All(list.Items, c => Assert.True(c.Required));
        Assert.Contains(list.Items, c => c.ReasonCode == NextStepRouteRuntime.ReasonAndSplitGuard);
    }

    sealed class LiteralBagPrepare : NextStepPrepare
    {
        public LiteralBagPrepare(params (string k, string v)[] pairs)
        {
            foreach (var (k, v) in pairs)
                IfCondition(k, v);
        }
    }

    sealed class FixedEvaluator : IConditionEvaluator
    {
        readonly bool? _value;
        public FixedEvaluator(bool? value) => _value = value;
        public bool IsMatch(ConditionEvaluateContext ctx) => true;
        public bool? TryEvaluate(ConditionEvaluateContext ctx) => _value;
    }

    sealed class DisableAllFilter : INextStepFilter
    {
        public bool IsMatch(NextStepRouteContext ctx) => true;
        public void Filter(NextStepRouteContext ctx, NextStepCandidateList candidates)
        {
            foreach (var c in candidates.Items)
                c.Disabled = true;
        }
    }
}
