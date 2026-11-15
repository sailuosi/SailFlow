using sailflow.engine.Actions.Transition;
using sailflow.engine.Common;
using Xunit;

namespace sailflow.engine.tests;

public sealed class RejectModeCodesTests
{
    [Theory]
    [InlineData(null, RejectModeCodes.Previous)]
    [InlineData("", RejectModeCodes.Previous)]
    [InlineData("  ", RejectModeCodes.Previous)]
    [InlineData("previous", RejectModeCodes.Previous)]
    [InlineData("Previous", RejectModeCodes.Previous)]
    [InlineData("Terminate", RejectModeCodes.Terminate)]
    [InlineData("starter", RejectModeCodes.Starter)]
    [InlineData("Specified", RejectModeCodes.Specified)]
    public void Normalize_known_or_blank(string? raw, string expected)
        => Assert.Equal(expected, RejectModeCodes.Normalize(raw));

    [Theory]
    [InlineData("Foo")]
    [InlineData("History")]
    [InlineData("SendBack")]
    public void Normalize_unknown_returns_empty(string raw)
        => Assert.Equal(string.Empty, RejectModeCodes.Normalize(raw));

    [Fact]
    public void ReadMode_defaults_Previous_when_sheet_missing()
    {
        var runner = new FlowCommandContext();
        Assert.Equal(RejectModeCodes.Previous, RejectTargetResolver.ReadMode(runner));
    }

    [Fact]
    public void ReadMode_reads_ControlParameterSheet()
    {
        var runner = new FlowCommandContext
        {
            ControlParameterSheet = new ControlParameterSheet { RejectMode = "Starter" }
        };
        Assert.Equal(RejectModeCodes.Starter, RejectTargetResolver.ReadMode(runner));
    }

    [Fact]
    public void Resolve_Specified_without_return_id_fails()
    {
        var runner = new FlowCommandContext
        {
            ControlParameterSheet = new ControlParameterSheet
            {
                RejectMode = RejectModeCodes.Specified,
                RejectReturnActivityId = ""
            }
        };
        // sail unused until after return-id check
        var r = RejectTargetResolver.ResolveBackwardTarget(null!, runner);
        Assert.False(r.Ok);
        Assert.Equal(RejectReasonCodes.RejectReturnActivityRequired, r.ReasonCode);
    }

    [Fact]
    public void Resolve_invalid_mode_fails()
    {
        var runner = new FlowCommandContext
        {
            ControlParameterSheet = new ControlParameterSheet { RejectMode = "Nope" }
        };
        var r = RejectTargetResolver.ResolveBackwardTarget(null!, runner);
        Assert.False(r.Ok);
        Assert.Equal(RejectReasonCodes.RejectModeInvalid, r.ReasonCode);
    }

    [Fact]
    public void Resolve_Terminate_via_backward_resolver_fails()
    {
        var runner = new FlowCommandContext
        {
            ControlParameterSheet = new ControlParameterSheet
            {
                RejectMode = RejectModeCodes.Terminate
            }
        };
        var r = RejectTargetResolver.ResolveBackwardTarget(null!, runner);
        Assert.False(r.Ok);
        Assert.Equal(RejectReasonCodes.RejectModeInvalid, r.ReasonCode);
    }
}
