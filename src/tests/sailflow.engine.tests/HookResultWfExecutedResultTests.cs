using saiflow.engine.Result;
using saiflow.engine.Hooks;
using Xunit;

namespace saiflow.engine.tests;

public sealed class HookResultWfExecutedResultTests
{
    [Fact]
    public void Reject_sets_validation_fields()
    {
        var reject = HookResult.Reject("blocked", "CODE_X", new[]
        {
            new HookValidationItem { Field = "amount", Message = "too high" },
        });

        Assert.False(reject.IsOk);
        Assert.Equal("blocked", reject.Message);
        Assert.Equal("CODE_X", reject.Code);
        Assert.Single(reject.Details!);

        var result = new WfExecutedResult();
        result.ApplyGateReject(reject);

        Assert.Equal(WfExecutedStatus.Failed, result.Status);
        Assert.Equal("blocked", result.Message);
        Assert.Equal("CODE_X", result.ValidationCode);
        Assert.True(result.IsValidationReject());
        Assert.True(result.IsBusinessValidationFailure());
    }

    [Fact]
    public void ApplyGateReject_ignores_ok()
    {
        var result = new WfExecutedResult { Status = WfExecutedStatus.Success, Message = "ok" };
        result.ApplyGateReject(HookResult.Ok());

        Assert.Equal(WfExecutedStatus.Success, result.Status);
        Assert.False(result.IsValidationReject());
    }
}
