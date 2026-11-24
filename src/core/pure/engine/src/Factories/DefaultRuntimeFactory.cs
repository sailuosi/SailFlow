using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.data;
using sailflow.engine.Pattern;
using sailflow.engine.Result;
using sailflow.engine.Runtime;
using sailflow.engine.Xpdl;

namespace sailflow.engine.Factories;

/// <summary>
/// A · 默认 Runtime 工厂：Create Runtime 实例；并入原 <c>ActivityResourceFactory</c>。
/// </summary>
public class DefaultRuntimeFactory : IRuntimeFactory
{
    private readonly SailClient _sail;
    private readonly RuntimeInstanceCreator _instances;

    public DefaultRuntimeFactory(SailClient sail)
    {
        _sail = sail ?? throw new ArgumentNullException(nameof(sail));
        _instances = new RuntimeInstanceCreator(_sail);
    }

    public virtual ActivityResource CreateActivityResource(
        FlowCommandContext runner,
        IDictionary<string, PerformerList> nextActivityPerformers,
        IDictionary<string, string> conditionValuePair = null)
        => new ActivityResource(runner, nextActivityPerformers, conditionValuePair);

    public virtual ActivityResource CreateActivityResource(
        IProcessModel processModel,
        NextActivityComponent root,
        FlowCommandContext runner,
        IDictionary<string, string> conditionValuePair)
    {
        var nextActivityPerformers = GetActivityPerformerList(processModel, root, runner, conditionValuePair);
        return new ActivityResource(runner, nextActivityPerformers, conditionValuePair);
    }

    private static IDictionary<string, PerformerList> GetActivityPerformerList(
        IProcessModel processModel,
        NextActivityComponent root,
        FlowCommandContext runner,
        IDictionary<string, string> conditionValuePair)
    {
        IDictionary<string, PerformerList> nextActivityPerformers = new Dictionary<string, PerformerList>();
        foreach (NextActivityComponent comp in root)
        {
            IDictionary<string, PerformerList> itemActivityPerformers;
            if (comp.HasChildren)
            {
                itemActivityPerformers = GetActivityPerformerList(processModel, comp, runner, conditionValuePair);
            }
            else
            {
                itemActivityPerformers = processModel.GetActivityPerformers(comp.Activity.ActivityId);
                foreach (var item in itemActivityPerformers)
                {
                    if (item.Value.Count == 0)
                        item.Value.Add(new Performer(runner.UserId, runner.UserName));
                }
            }
            Append(nextActivityPerformers, itemActivityPerformers);
        }
        return nextActivityPerformers;
    }

    private static void Append(
        IDictionary<string, PerformerList> orginalPerformerList,
        IDictionary<string, PerformerList> newPerformerList)
    {
        foreach (var item in newPerformerList)
        {
            if (!orginalPerformerList.ContainsKey(item.Key))
                orginalPerformerList[item.Key] = item.Value;
        }
    }

    public virtual IWfRuntime CreateRuntimeInstanceStartup(
        FlowCommandContext runner, SQLBuilder kit, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceStartup(runner, kit, ref result);

    public virtual IWfRuntime CreateRuntimeInstanceStartupSub(
        FlowCommandContext runner,
        ProcessInstanceEntity processInstance,
        SubProcessNode subProcessNode,
        PerformerList performerList,
        SQLBuilder kit,
        ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceStartupSub(
            runner, processInstance, subProcessNode, performerList, kit, ref result);

    public virtual IWfRuntime CreateRuntimeInstanceAppRunning(
        FlowCommandContext runner, SQLBuilder kit, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceAppRunning(runner, kit, ref result);

    public virtual async Task<(IWfRuntime Manager, WfExecutedResult Result)>
        CreateRuntimeInstanceAppRunningAsync(
            FlowCommandContext runner, SQLBuilder kit, CancellationToken cancellationToken)
    {
        var (manager, result) = await _instances
            .CreateRuntimeInstanceAppRunningAsync(runner, kit, cancellationToken)
            .ConfigureAwait(false);
        return (manager, result);
    }

    public virtual IWfRuntime CreateRuntimeInstanceRunAuto(
        FlowCommandContext runner, SQLBuilder kit, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceRunAuto(runner, kit, ref result);

    public virtual async Task<(IWfRuntime Manager, WfExecutedResult Result)>
        CreateRuntimeInstanceRunAutoAsync(
            FlowCommandContext runner, SQLBuilder kit, CancellationToken cancellationToken)
    {
        var (manager, result) = await _instances
            .CreateRuntimeInstanceRunAutoAsync(runner, kit, cancellationToken)
            .ConfigureAwait(false);
        return (manager, result);
    }

    public virtual IWfRuntime CreateRuntimeInstanceWithdraw(
        FlowCommandContext runner, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceWithdraw(runner, ref result);

    public virtual IWfRuntime CreateRuntimeInstanceSendBack(
        FlowCommandContext runner, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceSendBack(runner, ref result);

    public virtual IWfRuntime CreateRuntimeInstanceResend(
        FlowCommandContext runner, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceResend(runner, ref result);

    public virtual IWfRuntime CreateRuntimeInstanceRevise(
        FlowCommandContext runner, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceRevise(runner, ref result);

    public virtual IWfRuntime CreateRuntimeInstanceReverse(
        FlowCommandContext runner, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceReverse(runner, ref result);

    public virtual IWfRuntime CreateRuntimeInstanceReject(
        FlowCommandContext runner, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceReject(runner, ref result);

    public virtual IWfRuntime CreateRuntimeInstanceClose(
        FlowCommandContext runner, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceClose(runner, ref result);

    public virtual IWfRuntime CreateRuntimeInstanceJump(
        FlowCommandContext runner, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceJump(runner, ref result);

    public virtual IWfRuntime CreateRuntimeInstanceSignForward(
        FlowCommandContext runner, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceSignForward(runner, ref result);
}
