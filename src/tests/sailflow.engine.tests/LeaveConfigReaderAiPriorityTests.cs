using sailflow.assignee;
using sailflow.assignee.Adapt;
using sailflow.engine.Assignee;
using sailflow.data;
using Xunit;

namespace sailflow.engine.tests;

/// <summary>AI Leave 快照优先于 BPMN / Legacy（无 SailClient 时仅测 AI 分支）。</summary>
public class LeaveConfigReaderAiPriorityTests
{
    [Fact]
    public void ReadLeave_Uses_Ai_Snapshot_When_Present()
    {
        var main = new ActivityInstanceEntity
        {
            ActivityInstanceId = "ai-main",
            ActivityId = "act-1",
            ProcessId = "p1",
            LeaveJudgeType = LeaveJudgeTypeCodes.Collaborate,
            LeaveJudgeParams = "{\"completeCount\":2}"
        };

        var pair = BpmnAssigneeConfigReader.ReadLeave(
            sail: null,
            new LeaveInvokeRequest { MainActivityInstance = main });

        Assert.NotNull(pair);
        Assert.Equal(LeaveJudgeTypeCodes.Collaborate, pair!.Type);
        Assert.True(pair.Params.ContainsKey("completeCount"));
    }

    [Fact]
    public void ReadLeave_Ai_Empty_Without_Sail_Returns_Null()
    {
        var main = new ActivityInstanceEntity
        {
            ActivityInstanceId = "ai-main",
            ActivityId = "act-1",
            ProcessId = "p1",
            LeaveJudgeType = null,
            LeaveJudgeParams = null
        };

        // 无 AI 快照且无 SailClient 无法加载 BPMN → null（由 Pipeline 再 Legacy）
        var pair = BpmnAssigneeConfigReader.ReadLeave(
            sail: null,
            new LeaveInvokeRequest { MainActivityInstance = main });

        Assert.Null(pair);
    }

    [Fact]
    public void SignForward_Stack_Leave_Constant_Is_Collaborate()
    {
        // 与 NodeMediatorSignForward.StackLeaveCollaborate 对齐
        Assert.Equal("Collaborate", LeaveJudgeTypeCodes.Collaborate);
    }
}
