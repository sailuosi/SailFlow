using sailflow.biz.Approval;
using sailflow.events;
using sailflow.hooks;
using Xunit;

namespace sailflow.engine.tests;

public sealed class SailApprovalBusinessRegistryTests
{
    [Fact]
    public void Duplicate_FormCode_throws()
    {
        var a = new StubBusiness("Leave");
        var b = new StubBusiness("leave");
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new SailApprovalBusinessRegistry(new ISailApprovalBusiness[] { a, b }));
        Assert.Contains("Duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OnFinish_throw_is_not_swallowed_by_hook()
    {
        var handler = new ThrowingFinishBusiness();
        var hook = new SailApprovalBusinessLifecycleHook(new[] { handler });
        Assert.Throws<InvalidOperationException>(() =>
            hook.Handle(new FlowHookContext
            {
                BizChannel = handler.FormCode,
                BizPK = "pk1",
                EventType = EventFireTypeEnum.OnProcessCompleted
            }));
        Assert.True(handler.FinishCalled);
    }

    sealed class StubBusiness(string formCode) : SailApprovalBusinessBase
    {
        public override string FormCode => formCode;
    }

    sealed class ThrowingFinishBusiness : SailApprovalBusinessBase
    {
        public override string FormCode => "ThrowBiz";
        public bool FinishCalled { get; private set; }

        public override void OnFinish(SailApprovalBusinessContext ctx)
        {
            FinishCalled = true;
            throw new InvalidOperationException("biz finish failed");
        }
    }
}
