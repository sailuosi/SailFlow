using saiflow.engine.Hooks;
using Xunit;

namespace saiflow.engine.tests;

public sealed class FlowCapabilityMatchTests
{
    [Fact]
    public void MatchesProcess_All_ignores_filters()
    {
        Assert.True(FlowCapabilityMatch.MatchesProcess(
            HookMatch.All, null, null, processCode: null, processId: null));
        Assert.True(FlowCapabilityMatch.MatchesProcess(
            HookMatch.All, new[] { "X" }, null, processCode: "Y", processId: null));
    }

    [Fact]
    public void MatchesProcess_Whitelist_requires_code_or_id()
    {
        Assert.False(FlowCapabilityMatch.MatchesProcess(
            HookMatch.Whitelist, null, null, "EXPENSE", "p1"));

        Assert.True(FlowCapabilityMatch.MatchesProcess(
            HookMatch.Whitelist, new[] { "expense" }, null, "EXPENSE", null));

        Assert.True(FlowCapabilityMatch.MatchesProcess(
            HookMatch.Whitelist, null, new[] { "P-99" }, null, "p-99"));

        Assert.False(FlowCapabilityMatch.MatchesProcess(
            HookMatch.Whitelist, new[] { "A" }, null, "B", null));
    }

    [Fact]
    public void MatchesActivity_respects_whitelist()
    {
        Assert.True(FlowCapabilityMatch.MatchesActivity(null, "any"));
        Assert.True(FlowCapabilityMatch.MatchesActivity(Array.Empty<string>(), "any"));

        Assert.True(FlowCapabilityMatch.MatchesActivity(new[] { "approve" }, "Approve"));
        Assert.False(FlowCapabilityMatch.MatchesActivity(new[] { "approve" }, null));
        Assert.False(FlowCapabilityMatch.MatchesActivity(new[] { "approve" }, "submit"));
    }
}
