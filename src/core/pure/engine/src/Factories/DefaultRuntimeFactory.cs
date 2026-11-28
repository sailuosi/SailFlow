using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using sailflow.hosting;
using sailflow.utils;
using sailflow.data;
using sailflow.run;
using sailflow.result;
using sailflow.xml;

namespace sailflow.factory;
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
        FlowCommandContext context,
        IDictionary<string, PerformerList> nextActivityPerformers,
        IDictionary<string, string> conditionValuePair = null)
        => new ActivityResource(context, nextActivityPerformers, conditionValuePair);

    public virtual ActivityResource CreateActivityResource(
        IProcessModel processModel,
        NextActivityComponent root,
        FlowCommandContext context,
        IDictionary<string, string> conditionValuePair)
    {
        var nextActivityPerformers = GetActivityPerformerList(processModel, root, context, conditionValuePair);
        return new ActivityResource(context, nextActivityPerformers, conditionValuePair);
    }

    private static IDictionary<string, PerformerList> GetActivityPerformerList(
        IProcessModel processModel,
        NextActivityComponent root,
        FlowCommandContext context,
        IDictionary<string, string> conditionValuePair)
    {
        IDictionary<string, PerformerList> nextActivityPerformers = new Dictionary<string, PerformerList>();
        foreach (NextActivityComponent comp in root)
        {
            IDictionary<string, PerformerList> itemActivityPerformers;
            if (comp.HasChildren)
            {
                itemActivityPerformers = GetActivityPerformerList(processModel, comp, context, conditionValuePair);
            }
            else
            {
                itemActivityPerformers = processModel.GetActivityPerformers(comp.Activity.ActivityId);
                foreach (var item in itemActivityPerformers)
                {
                    if (item.Value.Count == 0)
                        item.Value.Add(new Performer(context.UserId, context.UserName));
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
        FlowCommandContext context, SQLBuilder kit, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceStartup(context, kit, ref result);

    public virtual IWfRuntime CreateRuntimeInstanceStartupSub(
        FlowCommandContext context,
        ProcessInstanceEntity processInstance,
        SubProcessNode subProcessNode,
        PerformerList performerList,
        SQLBuilder kit,
        ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceStartupSub(
            context, processInstance, subProcessNode, performerList, kit, ref result);

    public virtual IWfRuntime CreateRuntimeInstanceAppRunning(
        FlowCommandContext context, SQLBuilder kit, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceAppRunning(context, kit, ref result);

    public virtual async Task<(IWfRuntime Manager, WfExecutedResult Result)>
        CreateRuntimeInstanceAppRunningAsync(
            FlowCommandContext context, SQLBuilder kit, CancellationToken cancellationToken)
    {
        var (manager, result) = await _instances
            .CreateRuntimeInstanceAppRunningAsync(context, kit, cancellationToken)
            .ConfigureAwait(false);
        return (manager, result);
    }

    public virtual IWfRuntime CreateRuntimeInstanceRunAuto(
        FlowCommandContext context, SQLBuilder kit, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceRunAuto(context, kit, ref result);

    public virtual async Task<(IWfRuntime Manager, WfExecutedResult Result)>
        CreateRuntimeInstanceRunAutoAsync(
            FlowCommandContext context, SQLBuilder kit, CancellationToken cancellationToken)
    {
        var (manager, result) = await _instances
            .CreateRuntimeInstanceRunAutoAsync(context, kit, cancellationToken)
            .ConfigureAwait(false);
        return (manager, result);
    }

    public virtual IWfRuntime CreateRuntimeInstanceWithdraw(
        FlowCommandContext context, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceWithdraw(context, ref result);

    public virtual IWfRuntime CreateRuntimeInstanceSendBack(
        FlowCommandContext context, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceSendBack(context, ref result);

    public virtual IWfRuntime CreateRuntimeInstanceResend(
        FlowCommandContext context, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceResend(context, ref result);

    public virtual IWfRuntime CreateRuntimeInstanceRevise(
        FlowCommandContext context, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceRevise(context, ref result);

    public virtual IWfRuntime CreateRuntimeInstanceReverse(
        FlowCommandContext context, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceReverse(context, ref result);

    public virtual IWfRuntime CreateRuntimeInstanceReject(
        FlowCommandContext context, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceReject(context, ref result);

    public virtual IWfRuntime CreateRuntimeInstanceClose(
        FlowCommandContext context, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceClose(context, ref result);

    public virtual IWfRuntime CreateRuntimeInstanceJump(
        FlowCommandContext context, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceJump(context, ref result);

    public virtual IWfRuntime CreateRuntimeInstanceSignForward(
        FlowCommandContext context, ref WfExecutedResult result)
        => _instances.CreateRuntimeInstanceSignForward(context, ref result);
}
