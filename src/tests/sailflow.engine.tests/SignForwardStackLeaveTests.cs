using sailflow.assignee.Builtin;
using sailflow.engine.Assignee;
using sailflow.engine.Common;
using sailflow.engine.Entity;
using sailflow.engine.Xpdl;
using Xunit;

namespace sailflow.engine.tests;

/// <summary>后加签子堆栈：Collaborate + Sequence 全员阈时，首席办结应为 Waiting。</summary>
public class SignForwardStackLeaveTests
{
    [Fact]
    public void Sequence_Collaborate_First_Of_Two_Is_Waiting()
    {
        var main = new ActivityInstanceEntity
        {
            ActivityInstanceId = "main",
            LeaveJudgeType = "Collaborate",
            MergeType = (short)MergeTypeEnum.Sequence,
            CompareType = (short)CompareTypeEnum.Count,
            CompleteOrder = 2
        };

        var children = new List<ActivityInstanceEntity>
        {
            new()
            {
                ActivityInstanceId = "c1",
                MainActInstanceId = "main",
                SignIds = "self",
                CompleteOrder = 1,
                ActivityState = (short)ActivityStateEnum.Completed
            },
            new()
            {
                ActivityInstanceId = "c2",
                MainActInstanceId = "main",
                SignIds = "zhangsan",
                CompleteOrder = 2,
                ActivityState = (short)ActivityStateEnum.Suspended
            }
        };

        var seats = new List<LeaveSeatSnapshot>
        {
            new() { SignId = "self", Weight = 1m, Agreed = true },
            new() { SignId = "zhangsan", Weight = 1m, Agreed = false }
        };

        var result = MiPassRateHelper.Evaluate(main, children, seats);
        Assert.Equal(LeaveDecisions.Waiting, result.Decision);
    }

    [Fact]
    public void Sequence_Collaborate_Both_Agreed_Is_Passed()
    {
        var main = new ActivityInstanceEntity
        {
            ActivityInstanceId = "main",
            LeaveJudgeType = "Collaborate",
            MergeType = (short)MergeTypeEnum.Sequence,
            CompareType = (short)CompareTypeEnum.Count,
            CompleteOrder = 2
        };

        var children = new List<ActivityInstanceEntity>
        {
            new()
            {
                ActivityInstanceId = "c1",
                SignIds = "self",
                CompleteOrder = 1,
                ActivityState = (short)ActivityStateEnum.Completed
            },
            new()
            {
                ActivityInstanceId = "c2",
                SignIds = "zhangsan",
                CompleteOrder = 2,
                ActivityState = (short)ActivityStateEnum.Completed
            }
        };

        var seats = new List<LeaveSeatSnapshot>
        {
            new() { SignId = "self", Weight = 1m, Agreed = true },
            new() { SignId = "zhangsan", Weight = 1m, Agreed = true }
        };

        var result = MiPassRateHelper.Evaluate(main, children, seats);
        Assert.Equal(LeaveDecisions.Passed, result.Decision);
    }
}
