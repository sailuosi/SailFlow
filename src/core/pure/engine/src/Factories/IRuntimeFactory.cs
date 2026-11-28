using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using sailflow.utils;
using sailflow.data;
using sailflow.run;
using sailflow.result;
using sailflow.xml;

namespace sailflow.factory;
/// <summary>
/// A · Runtime 工厂槽（可覆写）。Create* 返回 <see cref="IWfRuntime"/> / <see cref="ActivityResource"/>。
/// </summary>
public interface IRuntimeFactory
{
    ActivityResource CreateActivityResource(
        FlowCommandContext context,
        IDictionary<string, PerformerList> nextActivityPerformers,
        IDictionary<string, string> conditionValuePair = null);

    ActivityResource CreateActivityResource(
        IProcessModel processModel,
        NextActivityComponent root,
        FlowCommandContext context,
        IDictionary<string, string> conditionValuePair);

    IWfRuntime CreateRuntimeInstanceStartup(
        FlowCommandContext context, SQLBuilder kit, ref WfExecutedResult result);

    IWfRuntime CreateRuntimeInstanceStartupSub(
        FlowCommandContext context,
        ProcessInstanceEntity processInstance,
        SubProcessNode subProcessNode,
        PerformerList performerList,
        SQLBuilder kit,
        ref WfExecutedResult result);

    IWfRuntime CreateRuntimeInstanceAppRunning(
        FlowCommandContext context, SQLBuilder kit, ref WfExecutedResult result);

    Task<(IWfRuntime Manager, WfExecutedResult Result)> CreateRuntimeInstanceAppRunningAsync(
        FlowCommandContext context, SQLBuilder kit, CancellationToken cancellationToken);

    IWfRuntime CreateRuntimeInstanceRunAuto(
        FlowCommandContext context, SQLBuilder kit, ref WfExecutedResult result);

    Task<(IWfRuntime Manager, WfExecutedResult Result)> CreateRuntimeInstanceRunAutoAsync(
        FlowCommandContext context, SQLBuilder kit, CancellationToken cancellationToken);

    IWfRuntime CreateRuntimeInstanceWithdraw(
        FlowCommandContext context, ref WfExecutedResult result);

    IWfRuntime CreateRuntimeInstanceSendBack(
        FlowCommandContext context, ref WfExecutedResult result);

    IWfRuntime CreateRuntimeInstanceResend(
        FlowCommandContext context, ref WfExecutedResult result);

    IWfRuntime CreateRuntimeInstanceRevise(
        FlowCommandContext context, ref WfExecutedResult result);

    IWfRuntime CreateRuntimeInstanceReverse(
        FlowCommandContext context, ref WfExecutedResult result);

    IWfRuntime CreateRuntimeInstanceReject(
        FlowCommandContext context, ref WfExecutedResult result);

    IWfRuntime CreateRuntimeInstanceClose(
        FlowCommandContext context, ref WfExecutedResult result);

    IWfRuntime CreateRuntimeInstanceJump(
        FlowCommandContext context, ref WfExecutedResult result);

    IWfRuntime CreateRuntimeInstanceSignForward(
        FlowCommandContext context, ref WfExecutedResult result);
}
