using sailflow.assignee;
using sailflow.assignee.Hosting;
using sailflow.utils;
using Xunit;

namespace sailflow.engine.tests;

public class AssigneeScopeLoadCandidatesTests
{
    static AssigneePipeline CreatePipeline()
    {
        var registry = new AssigneeCapabilityRegistry();
        SailAssigneeExtensions.RegisterBuiltins(registry);
        return new AssigneePipeline(registry);
    }

    static AssigneeInvokeRequest Req() => new() { ActivityId = "Act_Test" };

    [Fact]
    public void Empty_scope_is_unconstrained()
    {
        var pipe = CreatePipeline();
        var ok = pipe.TryLoadScopeCandidates(
            "",
            null,
            Req(),
            out var list,
            out var meta);

        Assert.True(ok);
        Assert.True(meta.Unconstrained);
        Assert.False(meta.UnregisteredType);
        Assert.Equal(0, list.Count);
    }

    [Fact]
    public void AnyMan_is_unconstrained()
    {
        var pipe = CreatePipeline();
        var ok = pipe.TryLoadScopeCandidates(
            AssigneeResolveTypeCodes.AnyMan,
            null,
            Req(),
            out var list,
            out var meta);

        Assert.True(ok);
        Assert.True(meta.Unconstrained);
        Assert.Equal(AssigneeResolveTypeCodes.AnyMan, meta.ScopeType);
        Assert.Equal(0, list.Count);
    }

    [Fact]
    public void FixedUsers_returns_candidates()
    {
        var pipe = CreatePipeline();
        var ok = pipe.TryLoadScopeCandidates(
            AssigneeResolveTypeCodes.FixedUsers,
            new Dictionary<string, object>
            {
                ["userIds"] = new[] { "u1", "u2" },
                ["userNames"] = new[] { "甲", "乙" }
            },
            Req(),
            out var list,
            out var meta);

        Assert.True(ok);
        Assert.False(meta.Unconstrained);
        Assert.False(meta.UnregisteredType);
        Assert.Equal(2, list.Count);
        Assert.Equal("u1", list[0].UserId);
        Assert.Equal("u2", list[1].UserId);
    }

    [Fact]
    public void Unregistered_type_fails_with_reason()
    {
        var pipe = CreatePipeline();
        var ok = pipe.TryLoadScopeCandidates(
            "NoSuchScopeType_XYZ",
            null,
            Req(),
            out _,
            out var meta);

        Assert.False(ok);
        Assert.True(meta.UnregisteredType);
        Assert.Equal(AssigneeReasonCodes.AssigneeTypeUnregistered, meta.ReasonCode);
    }
}
