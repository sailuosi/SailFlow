using sailflow.engine.NextStep;
using Xunit;

namespace sailflow.engine.tests;

public sealed class NextStepRouteMatchTests
{
    [Fact]
    public void Match_All_ignores_process_filters()
    {
        var opts = new NextStepRouteOptions { Match = NextStepMatch.All };
        Assert.True(NextStepRouteMatch.Matches(opts, null, null, null));
        Assert.True(NextStepRouteMatch.Matches(opts, "X", "id", "act"));
    }

    [Fact]
    public void Match_Whitelist_empty_process_dims_is_miss()
    {
        var opts = new NextStepRouteOptions { Match = NextStepMatch.Whitelist };
        Assert.False(NextStepRouteMatch.Matches(opts, "EXPENSE", "p1", null));
    }

    [Fact]
    public void Match_Whitelist_process_code_case_insensitive()
    {
        var opts = new NextStepRouteOptions
        {
            Match = NextStepMatch.Whitelist,
            ProcessCodes = new[] { "expense" }
        };
        Assert.True(NextStepRouteMatch.Matches(opts, "EXPENSE", null, null));
        Assert.False(NextStepRouteMatch.Matches(opts, "OTHER", null, null));
    }

    [Fact]
    public void Match_ProcessCodePatterns_regex()
    {
        var opts = new NextStepRouteOptions
        {
            Match = NextStepMatch.Whitelist,
            ProcessCodePatterns = new[] { "^FIN_.*" }
        };
        Assert.True(NextStepRouteMatch.Matches(opts, "FIN_PAY", null, null));
        Assert.False(NextStepRouteMatch.Matches(opts, "HR_LEAVE", null, null));
    }

    [Fact]
    public void Match_ActivityCodes_when_set()
    {
        var opts = new NextStepRouteOptions
        {
            Match = NextStepMatch.All,
            ActivityCodes = new[] { "dept_approve" }
        };
        Assert.True(NextStepRouteMatch.Matches(opts, "X", null, "dept_approve"));
        Assert.False(NextStepRouteMatch.Matches(opts, "X", null, "other"));
    }
}
