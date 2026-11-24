using sailflow.assignee.Builtin;
using sailflow.assignee;
using sailflow.engine.Common;
using sailflow.data;
using sailflow.xml;
using Xunit;

namespace sailflow.engine.tests;

/// <summary>席位级加签：同级 seats、折合、Race 淹没语义（单元）。</summary>
public class SignForwardStackLeaveTests
{
    [Fact]
    public void Sequence_Collaborate_First_Of_Two_Is_Waiting()
    {
        var main = new ActivityInstanceEntity
        {
            ActivityInstanceId = "container",
            LeaveJudgeType = "Collaborate",
            MergeType = (short)MergeTypeEnum.Sequence,
            CompareType = (short)CompareTypeEnum.Count,
            CompleteOrder = 2
        };

        var seats = new List<LeaveSeatSnapshot>
        {
            new() { SignId = "self", Weight = 1m, Agreed = true },
            new() { SignId = "lisi", Weight = 1m, Agreed = false }
        };

        var result = MiPassRateHelper.Evaluate(main, new List<ActivityInstanceEntity>(), seats);
        Assert.Equal(LeaveDecisions.Waiting, result.Decision);
    }

    [Fact]
    public void Sequence_Collaborate_Both_Agreed_Is_Passed()
    {
        var main = new ActivityInstanceEntity
        {
            ActivityInstanceId = "container",
            LeaveJudgeType = "Collaborate",
            MergeType = (short)MergeTypeEnum.Sequence,
            CompareType = (short)CompareTypeEnum.Count,
            CompleteOrder = 2
        };

        var seats = new List<LeaveSeatSnapshot>
        {
            new() { SignId = "self", Weight = 1m, Agreed = true },
            new() { SignId = "lisi", Weight = 1m, Agreed = true }
        };

        var result = MiPassRateHelper.Evaluate(main, new List<ActivityInstanceEntity>(), seats);
        Assert.Equal(LeaveDecisions.Passed, result.Decision);
    }

    [Fact]
    public void BuildSeatsForActivity_DirectTask_And_ChildFold_SameLevel()
    {
        var subject = new ActivityInstanceEntity
        {
            ActivityInstanceId = "zhangsan-ai",
            SignIds = "zhangsan",
            ComplexType = (short)ComplexTypeEnum.SignForward,
            LeaveJudgeType = "Collaborate"
        };

        var directTasks = new List<TaskEntity>
        {
            new()
            {
                TaskId = "t1",
                SignId = "zhangsan",
                ActivityInstanceId = "zhangsan-ai",
                TaskState = (short)TaskStateEnum.Completed,
                Weight = 1m
            }
        };

        var children = new List<ActivityInstanceEntity>
        {
            new()
            {
                ActivityInstanceId = "lisi-ai",
                SignIds = "lisi",
                ActivityState = (short)ActivityStateEnum.Ready,
                ComplexType = null
            }
        };

        var childTasks = new List<TaskEntity>
        {
            new()
            {
                TaskId = "t2",
                SignId = "lisi",
                ActivityInstanceId = "lisi-ai",
                TaskState = (short)TaskStateEnum.Waiting,
                Weight = 1m
            }
        };

        var seats = LeaveSeatProjector.BuildSeatsForActivity(subject, directTasks, children, childTasks);
        Assert.Equal(2, seats.Count);
        Assert.Contains(seats, s => s.SignId == "zhangsan" && s.Agreed);
        Assert.Contains(seats, s => s.SignId == "lisi" && !s.Agreed);
    }

    [Fact]
    public void BuildSeatsForActivity_SignForwardChild_Incomplete_FoldsAsNotAgreed()
    {
        var node = new ActivityInstanceEntity
        {
            ActivityInstanceId = "node",
            LeaveJudgeType = "Race"
        };

        var directTasks = new List<TaskEntity>
        {
            new()
            {
                TaskId = "tw",
                SignId = "zhangwu",
                ActivityInstanceId = "node",
                TaskState = (short)TaskStateEnum.Completed,
                Weight = 1m
            }
        };

        var children = new List<ActivityInstanceEntity>
        {
            new()
            {
                ActivityInstanceId = "zhangsan-container",
                SignIds = "zhangsan",
                ComplexType = (short)ComplexTypeEnum.SignForward,
                LeaveJudgeType = "Collaborate",
                ActivityState = (short)ActivityStateEnum.Ready
            }
        };

        var seats = LeaveSeatProjector.BuildSeatsForActivity(node, directTasks, children, null);
        Assert.Equal(2, seats.Count);
        Assert.Contains(seats, s => s.SignId == "zhangwu" && s.Agreed);
        Assert.Contains(seats, s => s.SignId == "zhangsan" && !s.Agreed);

        var race = MiPassRateHelper.EvaluateRace(seats);
        Assert.Equal(LeaveDecisions.Passed, race.Decision);
    }

    [Fact]
    public void BuildSeatsForActivity_CompletedContainer_FoldsAsAgreed()
    {
        var node = new ActivityInstanceEntity
        {
            ActivityInstanceId = "node",
            LeaveJudgeType = "Collaborate",
            MergeType = (short)MergeTypeEnum.Parallel,
            CompareType = (short)CompareTypeEnum.Count,
            CompleteOrder = 3
        };

        var children = new List<ActivityInstanceEntity>
        {
            new()
            {
                ActivityInstanceId = "zhangsan-container",
                SignIds = "zhangsan",
                ComplexType = (short)ComplexTypeEnum.SignForward,
                LeaveJudgeType = "Collaborate",
                ActivityState = (short)ActivityStateEnum.Completed,
                ApprovalStatus = (short)ApprovalStatusEnum.Agreed
            }
        };

        var seats = LeaveSeatProjector.BuildSeatsForActivity(
            node,
            new List<TaskEntity>
            {
                new()
                {
                    TaskId = "t4",
                    SignId = "zhangsi",
                    ActivityInstanceId = "node",
                    TaskState = (short)TaskStateEnum.Waiting
                },
                new()
                {
                    TaskId = "t5",
                    SignId = "zhangwu",
                    ActivityInstanceId = "node",
                    TaskState = (short)TaskStateEnum.Waiting
                }
            },
            children,
            null);

        Assert.Equal(3, seats.Count);
        Assert.Contains(seats, s => s.SignId == "zhangsan" && s.Agreed);
        var leave = MiPassRateHelper.Evaluate(node, children, seats);
        Assert.Equal(LeaveDecisions.Waiting, leave.Decision);
    }

    [Fact]
    public void IsSignForwardContainer_RequiresLeaveOnComplexType()
    {
        Assert.False(IActivityInstanceManager.IsSignForwardContainer(new ActivityInstanceEntity
        {
            ComplexType = (short)ComplexTypeEnum.SignForward,
            LeaveJudgeType = null
        }));
        Assert.True(IActivityInstanceManager.IsSignForwardContainer(new ActivityInstanceEntity
        {
            ComplexType = (short)ComplexTypeEnum.SignForward,
            LeaveJudgeType = "Collaborate"
        }));
    }
}
