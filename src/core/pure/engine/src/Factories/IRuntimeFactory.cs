using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using sailflow.engine.Common;
using sailflow.data;
using sailflow.run;
using sailflow.engine.Result;
using sailflow.engine.Xpdl;

namespace sailflow.engine.Factories;

/// <summary>
/// A · Runtime 工厂槽（可覆写）。Create* 返回 <see cref="IWfRuntime"/> / <see cref="ActivityResource"/>。
/// </summary>
public interface IRuntimeFactory
{
    ActivityResource CreateActivityResource(
        FlowCommandContext runner,
        IDictionary<string, PerformerList> nextActivityPerformers,
        IDictionary<string, string> conditionValuePair = null);

    ActivityResource CreateActivityResource(
        IProcessModel processModel,
        NextActivityComponent root,
        FlowCommandContext runner,
        IDictionary<string, string> conditionValuePair);

    IWfRuntime CreateRuntimeInstanceStartup(
        FlowCommandContext runner, SQLBuilder kit, ref WfExecutedResult result);

    IWfRuntime CreateRuntimeInstanceStartupSub(
        FlowCommandContext runner,
        ProcessInstanceEntity processInstance,
        SubProcessNode subProcessNode,
        PerformerList performerList,
        SQLBuilder kit,
        ref WfExecutedResult result);

    IWfRuntime CreateRuntimeInstanceAppRunning(
        FlowCommandContext runner, SQLBuilder kit, ref WfExecutedResult result);

    Task<(IWfRuntime Manager, WfExecutedResult Result)> CreateRuntimeInstanceAppRunningAsync(
        FlowCommandContext runner, SQLBuilder kit, CancellationToken cancellationToken);

    IWfRuntime CreateRuntimeInstanceRunAuto(
        FlowCommandContext runner, SQLBuilder kit, ref WfExecutedResult result);

    Task<(IWfRuntime Manager, WfExecutedResult Result)> CreateRuntimeInstanceRunAutoAsync(
        FlowCommandContext runner, SQLBuilder kit, CancellationToken cancellationToken);

    IWfRuntime CreateRuntimeInstanceWithdraw(
        FlowCommandContext runner, ref WfExecutedResult result);

    IWfRuntime CreateRuntimeInstanceSendBack(
        FlowCommandContext runner, ref WfExecutedResult result);

    IWfRuntime CreateRuntimeInstanceResend(
        FlowCommandContext runner, ref WfExecutedResult result);

    IWfRuntime CreateRuntimeInstanceRevise(
        FlowCommandContext runner, ref WfExecutedResult result);

    IWfRuntime CreateRuntimeInstanceReverse(
        FlowCommandContext runner, ref WfExecutedResult result);

    IWfRuntime CreateRuntimeInstanceReject(
        FlowCommandContext runner, ref WfExecutedResult result);

    IWfRuntime CreateRuntimeInstanceClose(
        FlowCommandContext runner, ref WfExecutedResult result);

    IWfRuntime CreateRuntimeInstanceJump(
        FlowCommandContext runner, ref WfExecutedResult result);

    IWfRuntime CreateRuntimeInstanceSignForward(
        FlowCommandContext runner, ref WfExecutedResult result);
}
