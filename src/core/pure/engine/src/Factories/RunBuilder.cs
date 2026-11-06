using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.engine.Manager;
using saiflow.engine.Result;
using saiflow.engine.Service;

namespace saiflow.engine.Factories;

/// <summary>
/// B · 办理链式建造会话。由 <see cref="SailClient.CreateRunBuilder"/> 每次 <c>new</c>；
/// <see cref="Build"/> / <see cref="BuildCommandContext"/> 产出<strong>入参袋</strong>
/// （<see cref="FlowCommandContext"/>）；执行编排袋由 Runtime/Mediator 工厂另建。
/// </summary>
public sealed class RunBuilder
{
    private readonly SailClient _sail;
    private readonly WfAppRunner _runner;

    internal RunBuilder(SailClient sail, WfAppRunner runner)
    {
        _sail = sail ?? throw new ArgumentNullException(nameof(sail));
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    WorkflowService Service() => new WorkflowService(_sail);

    public RunBuilder UseApp(string appInstanceId, string appName, string appCode = null)
    {
        _runner.AppInstanceId = appInstanceId;
        _runner.AppName = appName;
        _runner.AppInstanceCode = appCode;
        return this;
    }

    public RunBuilder UseProcess(string processId, string version = null)
    {
        if (string.IsNullOrEmpty(version)) version = "1";

        if (Guid.TryParse(processId, out _))
        {
            _runner.ProcessId = processId;
            _runner.Version = version;
        }
        else
        {
            var entity = _sail.ManagerFactory.Processes.GetByVersion(processId, version);
            _runner.ProcessId = entity.ProcessId;
            _runner.Version = entity.Version;
        }

        return this;
    }

    public RunBuilder OnTask(int taskId)
    {
        _runner.TaskId = taskId;
        return this;
    }

    public RunBuilder NextStep(IDictionary<string, PerformerList> nextActivityPerformers)
    {
        if (nextActivityPerformers == null || nextActivityPerformers.Count == 0)
            throw new ApplicationException(_sail.Messages.GetEngineMessage("workflowservice.nextstep.error"));
        _runner.NextActivityPerformers = nextActivityPerformers;
        return this;
    }

    public RunBuilder NextStep(string activityId, PerformerList performerList)
    {
        if (performerList == null || performerList.Count == 0)
            throw new ApplicationException(_sail.Messages.GetEngineMessage("workflowservice.nextstep.error"));
        _runner.NextActivityPerformers ??= new Dictionary<string, PerformerList>();
        _runner.NextActivityPerformers.Add(activityId, performerList);
        return this;
    }

    public RunBuilder PrevStepInt()
    {
        _runner.NextPerformerType = NextPerformerIntTypeEnum.Traced;
        return this;
    }

    public RunBuilder IfCondition(IDictionary<string, string> variables)
    {
        _runner.Conditions = variables;
        return this;
    }

    public RunBuilder IfCondition(string name, string value)
    {
        _runner.Conditions.Add(name, value);
        return this;
    }

    public RunBuilder SetVariable(string name, string value)
    {
        if (_runner.DynamicVariables == null)
            _runner.DynamicVariables = new Dictionary<string, string>();
        _runner.DynamicVariables[name] = value;
        return this;
    }

    public RunBuilder SetVariable(IDictionary<string, string> variables)
    {
        _runner.DynamicVariables = variables;
        return this;
    }

    public RunBuilder UseMessageTopic(string topic)
    {
        _runner.MessageTopic = topic;
        return this;
    }

    /// <summary>建成命令入参袋（基型 <see cref="WfAppRunner"/>；规范名见 <see cref="BuildCommandContext"/>）。</summary>
    public WfAppRunner Build() => _runner;

    /// <summary>
    /// 建成命令入参 Context（规范类型 <see cref="FlowCommandContext"/>）。
    /// <c>CreateRunBuilder(userId, userName)</c> 路径恒可成功；若 <c>CreateRunBuilder(WfAppRunner)</c> 传入的不是本类型则抛错。
    /// </summary>
    public FlowCommandContext BuildCommandContext()
    {
        if (_runner is FlowCommandContext ctx)
            return ctx;
        throw new InvalidOperationException(
            "Command bag is a legacy WfAppRunner. Use CreateRunBuilder(userId, userName) or pass a FlowCommandContext.");
    }

    public WfExecutedResult Start() => Service().StartProcess(Build());

    public Task<WfExecutedResult> StartAsync(CancellationToken cancellationToken = default)
        => Service().StartProcessAsync(Build(), cancellationToken);

    public WfExecutedResult StartByMessage() => Service().StartProcessByMessage(Build());

    public WfExecutedResult Run() => Service().RunProcess(Build());

    public Task<WfExecutedResult> RunAsync(CancellationToken cancellationToken = default)
        => Service().RunProcessAsync(Build(), cancellationToken);

    public WfExecutedResult RunAuto() => Service().RunProcessAuto(Build());

    public WfExecutedResult Withdraw() => Service().WithdrawProcess(Build());

    public WfExecutedResult SendBack() => Service().SendBackProcess(Build());

    public WfExecutedResult Resend() => Service().ResendProcess(Build());

    public WfExecutedResult Revise() => Service().ReviseProcess(Build());

    public WfExecutedResult Reverse() => Service().ReverseProcess(Build());

    public WfExecutedResult Reject() => Service().RejectProcess(Build());

    public WfExecutedResult Close() => Service().CloseProcess(Build());

    public WfExecutedResult Jump(JumpOptionEnum jumpOption = JumpOptionEnum.Default)
        => Service().JumpProcess(Build(), jumpOption);

    public WfExecutedResult SignForward() => Service().SignForwardProcess(Build());

    public WfExecutedResult Transfer() => Service().TransferProcess(Build());
}
