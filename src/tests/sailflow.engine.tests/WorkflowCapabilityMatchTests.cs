using saiflow.engine.Hooks;
using Xunit;

namespace saiflow.engine.tests;

public sealed class WorkflowCapabilityMatchTests
{
    [Fact]
    public void MatchesProcess_All_ignores_filters()
    {
        Assert.True(WorkflowCapabilityMatch.MatchesProcess(
            HookMatch.All, null, null, processCode: null, processId: null));
        Assert.True(WorkflowCapabilityMatch.MatchesProcess(
            HookMatch.All, new[] { "X" }, null, processCode: "Y", processId: null));
    }

    [Fact]
    public void MatchesProcess_Whitelist_requires_code_or_id()
    {
        Assert.False(WorkflowCapabilityMatch.MatchesProcess(
            HookMatch.Whitelist, null, null, "EXPENSE", "p1"));

        Assert.True(WorkflowCapabilityMatch.MatchesProcess(
            HookMatch.Whitelist, new[] { "expense" }, null, "EXPENSE", null));

        Assert.True(WorkflowCapabilityMatch.MatchesProcess(
            HookMatch.Whitelist, null, new[] { "P-99" }, null, "p-99"));

        Assert.False(WorkflowCapabilityMatch.MatchesProcess(
            HookMatch.Whitelist, new[] { "A" }, null, "B", null));
    }

    [Fact]
    public void MatchesActivity_respects_whitelist()
    {
        Assert.True(WorkflowCapabilityMatch.MatchesActivity(null, "any"));
        Assert.True(WorkflowCapabilityMatch.MatchesActivity(Array.Empty<string>(), "any"));

        Assert.True(WorkflowCapabilityMatch.MatchesActivity(new[] { "approve" }, "Approve"));
        Assert.False(WorkflowCapabilityMatch.MatchesActivity(new[] { "approve" }, null));
        Assert.False(WorkflowCapabilityMatch.MatchesActivity(new[] { "approve" }, "submit"));
    }
}
