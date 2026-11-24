using System;
using sailflow.engine.Client;
using sailflow.factory;
using sailflow.data;
using sailflow.run;
using sailflow.runtime.Parser;

namespace sailflow.factory;
/// <summary>
/// A · 默认 Manager 工厂：按 Client 懒创建；不持 Runner/kit；并入 Parser/Checker。
/// </summary>
public sealed class DefaultManagerFactory : IManagerFactory
{
    private readonly SailClient _sail;
    private IActivityInstanceManager _activities;
    private ITaskManager _tasks;
    private IProcessManager _processes;
    private IProcessInstanceManager _processInstances;
    private IProcessVariableManager _processVariables;
    private IProcessVariableResolver _processVariableResolver;
    private ITransitionInstanceManager _transitions;
    private IVariableManager _variables;
    private IHookOutboxManager _hookOutbox;
    private IRuleSetManager _ruleSets;
    private IRuleSetExecutionManager _ruleSetExecutions;
    private IJobInfoManager _jobInfos;
    private ILogManager _logs;
    private IPreviousStepChecker _previousSteps;
    private INextStepParser _nextSteps;
    private ISignForwardStepMaker _signForwardSteps;

    public DefaultManagerFactory(SailClient sail)
    {
        _sail = sail ?? throw new ArgumentNullException(nameof(sail));
    }

    public IActivityInstanceManager Activities =>
        _activities ??= new ActivityInstanceManager(_sail);

    public ITaskManager Tasks =>
        _tasks ??= new TaskManager(_sail);

    public IProcessManager Processes =>
        _processes ??= new ProcessManager(_sail);

    public IProcessInstanceManager ProcessInstances =>
        _processInstances ??= new ProcessInstanceManager(_sail);

    public IProcessVariableManager ProcessVariables =>
        _processVariables ??= new ProcessVariableManager(_sail);

    public IProcessVariableResolver ProcessVariableResolver =>
        _processVariableResolver ??= new ProcessVariableResolver(_sail);

    public ITransitionInstanceManager Transitions =>
        _transitions ??= new TransitionInstanceManager(_sail);

    public IVariableManager Variables =>
        _variables ??= new VariableManager(_sail);

    public IHookOutboxManager HookOutbox =>
        _hookOutbox ??= new HookOutboxManager(_sail);

    public IRuleSetManager RuleSets =>
        _ruleSets ??= new RuleSetManager(_sail);

    public IRuleSetExecutionManager RuleSetExecutions =>
        _ruleSetExecutions ??= new RuleSetExecutionManager(_sail);

    public IJobInfoManager JobInfos =>
        _jobInfos ??= new JobInfoManager(_sail);

    public ILogManager Logs =>
        _logs ??= new LogManager(_sail);

    public IPreviousStepChecker PreviousSteps =>
        _previousSteps ??= new PreviousStepChecker(_sail);

    public INextStepParser NextSteps =>
        _nextSteps ??= new NextStepParser(_sail);

    public ISignForwardStepMaker SignForwardSteps =>
        _signForwardSteps ??= new SignForwardStepMaker(_sail);
}
