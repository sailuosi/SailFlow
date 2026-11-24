using sailflow.biz.Approval;
using sailflow.engine.Common;
using sailflow.events;
using Xunit;

namespace sailflow.engine.tests;

public sealed class SailApprovalLifecycleMapperTests
{
    [Theory]
    [InlineData(EventFireTypeEnum.OnProcessCompleted, null, SailApprovalLifecycleKind.Finish, ApprovalOutcomeCodes.Approved, null)]
    [InlineData(EventFireTypeEnum.OnProcessRejected, null, SailApprovalLifecycleKind.Finish, ApprovalOutcomeCodes.Rejected, null)]
    [InlineData(EventFireTypeEnum.OnProcessClosed, null, SailApprovalLifecycleKind.Abort, null, ApprovalAbortKind.Close)]
    [InlineData(EventFireTypeEnum.OnProcessCanceled, null, SailApprovalLifecycleKind.Abort, null, ApprovalAbortKind.Cancel)]
    [InlineData(EventFireTypeEnum.OnProcessDiscarded, null, SailApprovalLifecycleKind.Abort, null, ApprovalAbortKind.Discard)]
    [InlineData(EventFireTypeEnum.OnProcessTerminated, null, SailApprovalLifecycleKind.Abort, null, ApprovalAbortKind.Terminate)]
    [InlineData(EventFireTypeEnum.OnProcessWithdrawn, null, SailApprovalLifecycleKind.Abort, null, ApprovalAbortKind.Withdraw)]
    public void Event_only_Q8_mapping(
        EventFireTypeEnum evt,
        ProcessStateEnum? state,
        SailApprovalLifecycleKind kind,
        string? outcome,
        ApprovalAbortKind? abort)
    {
        var d = SailApprovalLifecycleMapper.Map(evt, state);
        Assert.Equal(kind, d.Kind);
        Assert.Equal(outcome, d.Outcome);
        Assert.Equal(abort, d.AbortKind);
    }

    [Theory]
    [InlineData(EventFireTypeEnum.OnProcessCompleted, ProcessStateEnum.Completed, SailApprovalLifecycleKind.Finish, ApprovalOutcomeCodes.Approved)]
    [InlineData(EventFireTypeEnum.OnProcessRejected, ProcessStateEnum.Completed, SailApprovalLifecycleKind.Finish, ApprovalOutcomeCodes.Rejected)]
    public void Dual_read_Completed_is_Finish(
        EventFireTypeEnum evt,
        ProcessStateEnum state,
        SailApprovalLifecycleKind kind,
        string outcome)
    {
        var d = SailApprovalLifecycleMapper.Map(evt, state);
        Assert.Equal(kind, d.Kind);
        Assert.Equal(outcome, d.Outcome);
    }

    [Theory]
    [InlineData(EventFireTypeEnum.OnProcessTerminated, ProcessStateEnum.Terminated, ApprovalAbortKind.Terminate)]
    [InlineData(EventFireTypeEnum.OnProcessCanceled, ProcessStateEnum.Canceled, ApprovalAbortKind.Cancel)]
    [InlineData(EventFireTypeEnum.OnProcessDiscarded, ProcessStateEnum.Discarded, ApprovalAbortKind.Discard)]
    [InlineData(EventFireTypeEnum.OnProcessClosed, ProcessStateEnum.Running, ApprovalAbortKind.Close)]
    public void Dual_read_abort_states(EventFireTypeEnum evt, ProcessStateEnum state, ApprovalAbortKind abort)
    {
        var d = SailApprovalLifecycleMapper.Map(evt, state);
        Assert.Equal(SailApprovalLifecycleKind.Abort, d.Kind);
        Assert.Equal(abort, d.AbortKind);
    }

    [Theory]
    [InlineData(EventFireTypeEnum.OnProcessRejected, ProcessStateEnum.Running)]
    [InlineData(EventFireTypeEnum.OnProcessWithdrawn, ProcessStateEnum.Running)]
    [InlineData(EventFireTypeEnum.OnProcessSendBacked, ProcessStateEnum.Running)]
    public void Non_terminal_ignored(EventFireTypeEnum evt, ProcessStateEnum state)
    {
        var d = SailApprovalLifecycleMapper.Map(evt, state);
        Assert.Equal(SailApprovalLifecycleKind.None, d.Kind);
    }

    [Fact]
    public void SendBack_events_ignored_without_state()
    {
        var d = SailApprovalLifecycleMapper.Map(EventFireTypeEnum.OnProcessSendBacked, null);
        Assert.Equal(SailApprovalLifecycleKind.None, d.Kind);
    }
}
