using System;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.engine.Entity;
using saiflow.engine.Result;
using saiflow.engine.Runtime;
using saiflow.engine.Xpdl;

namespace saiflow.engine.Factories;

/// <summary>
/// A · 默认 Runtime 工厂：D2 转发现行 static <see cref="WfRuntimeManagerFactory"/>。
/// </summary>
public class DefaultRuntimeFactory : IRuntimeFactory
{
    private readonly SailClient _sail;

    public DefaultRuntimeFactory(SailClient sail)
    {
        _sail = sail ?? throw new ArgumentNullException(nameof(sail));
    }

    internal virtual WfRuntimeManager CreateRuntimeInstanceStartup(
        WfAppRunner runner, SQLBuilder kit, ref WfExecutedResult result)
        => WfRuntimeManagerFactory.CreateRuntimeInstanceStartup(_sail, runner, kit, ref result);

    internal virtual WfRuntimeManager CreateRuntimeInstanceStartupSub(
        WfAppRunner runner,
        ProcessInstanceEntity processInstance,
        SubProcessNode subProcessNode,
        PerformerList performerList,
        SQLBuilder kit,
        ref WfExecutedResult result)
        => WfRuntimeManagerFactory.CreateRuntimeInstanceStartupSub(
            _sail, runner, processInstance, subProcessNode, performerList, kit, ref result);

    internal virtual WfRuntimeManager CreateRuntimeInstanceAppRunning(
        WfAppRunner runner, SQLBuilder kit, ref WfExecutedResult result)
        => WfRuntimeManagerFactory.CreateRuntimeInstanceAppRunning(_sail, runner, kit, ref result);

    internal virtual Task<(WfRuntimeManager Manager, WfExecutedResult Result)>
        CreateRuntimeInstanceAppRunningAsync(
            WfAppRunner runner, SQLBuilder kit, CancellationToken cancellationToken)
        => WfRuntimeManagerFactory.CreateRuntimeInstanceAppRunningAsync(
            _sail, runner, kit, cancellationToken);

    internal virtual WfRuntimeManager CreateRuntimeInstanceRunAuto(
        WfAppRunner runner, SQLBuilder kit, ref WfExecutedResult result)
        => WfRuntimeManagerFactory.CreateRuntimeInstanceRunAuto(_sail, runner, kit, ref result);

    internal virtual Task<(WfRuntimeManager Manager, WfExecutedResult Result)>
        CreateRuntimeInstanceRunAutoAsync(
            WfAppRunner runner, SQLBuilder kit, CancellationToken cancellationToken)
        => WfRuntimeManagerFactory.CreateRuntimeInstanceRunAutoAsync(
            _sail, runner, kit, cancellationToken);

    internal virtual WfRuntimeManager CreateRuntimeInstanceWithdraw(
        WfAppRunner runner, ref WfExecutedResult result)
        => WfRuntimeManagerFactory.CreateRuntimeInstanceWithdraw(_sail, runner, ref result);

    internal virtual WfRuntimeManager CreateRuntimeInstanceSendBack(
        WfAppRunner runner, ref WfExecutedResult result)
        => WfRuntimeManagerFactory.CreateRuntimeInstanceSendBack(_sail, runner, ref result);

    internal virtual WfRuntimeManager CreateRuntimeInstanceResend(
        WfAppRunner runner, ref WfExecutedResult result)
        => WfRuntimeManagerFactory.CreateRuntimeInstanceResend(_sail, runner, ref result);

    internal virtual WfRuntimeManager CreateRuntimeInstanceRevise(
        WfAppRunner runner, ref WfExecutedResult result)
        => WfRuntimeManagerFactory.CreateRuntimeInstanceRevise(_sail, runner, ref result);

    internal virtual WfRuntimeManager CreateRuntimeInstanceReverse(
        WfAppRunner runner, ref WfExecutedResult result)
        => WfRuntimeManagerFactory.CreateRuntimeInstanceReverse(_sail, runner, ref result);

    internal virtual WfRuntimeManager CreateRuntimeInstanceReject(
        WfAppRunner runner, ref WfExecutedResult result)
        => WfRuntimeManagerFactory.CreateRuntimeInstanceReject(_sail, runner, ref result);

    internal virtual WfRuntimeManager CreateRuntimeInstanceClose(
        WfAppRunner runner, ref WfExecutedResult result)
        => WfRuntimeManagerFactory.CreateRuntimeInstanceClose(_sail, runner, ref result);

    internal virtual WfRuntimeManager CreateRuntimeInstanceJump(
        WfAppRunner runner, ref WfExecutedResult result)
        => WfRuntimeManagerFactory.CreateRuntimeInstanceJump(_sail, runner, ref result);

    internal virtual WfRuntimeManager CreateRuntimeInstanceSignForward(
        WfAppRunner runner, ref WfExecutedResult result)
        => WfRuntimeManagerFactory.CreateRuntimeInstanceSignForward(_sail, runner, ref result);
}
