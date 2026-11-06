using System;
using saiflow.engine.Client;
using saiflow.engine.Manager;
using saiflow.engine.Runtime;

namespace saiflow.engine.Factories;

/// <summary>
/// A · 默认 Manager 工厂：按 Client 懒创建；不持 Runner/kit；并入 Parser/Checker。
/// </summary>
public sealed class DefaultManagerFactory : IManagerFactory
{
    private readonly SailClient _sail;
    private ActivityInstanceManager _activities;
    private TaskManager _tasks;
    private ProcessManager _processes;
    private ProcessInstanceManager _processInstances;
    private ProcessVariableManager _processVariables;
    private ProcessVariableResolver _processVariableResolver;
    private TransitionInstanceManager _transitions;
    private VariableManager _variables;
    private HookOutboxManager _hookOutbox;
    private RuleSetManager _ruleSets;
    private RuleSetExecutionManager _ruleSetExecutions;
    private JobInfoManager _jobInfos;
    private LogManager _logs;
    private PreviousStepChecker _previousSteps;
    private NextStepParser _nextSteps;
    private SignForwardStepMaker _signForwardSteps;

    public DefaultManagerFactory(SailClient sail)
    {
        _sail = sail ?? throw new ArgumentNullException(nameof(sail));
    }

    public ActivityInstanceManager Activities =>
        _activities ??= new ActivityInstanceManager(_sail);

    public TaskManager Tasks =>
        _tasks ??= new TaskManager(_sail);

    public ProcessManager Processes =>
        _processes ??= new ProcessManager(_sail);

    public TransitionInstanceManager Transitions =>
        _transitions ??= new TransitionInstanceManager(_sail);

    public VariableManager Variables =>
        _variables ??= new VariableManager(_sail);

    public HookOutboxManager HookOutbox =>
        _hookOutbox ??= new HookOutboxManager(_sail);

    public RuleSetManager RuleSets =>
        _ruleSets ??= new RuleSetManager(_sail);

    public RuleSetExecutionManager RuleSetExecutions =>
        _ruleSetExecutions ??= new RuleSetExecutionManager(_sail);

    public JobInfoManager JobInfos =>
        _jobInfos ??= new JobInfoManager(_sail);

    public LogManager Logs =>
        _logs ??= new LogManager(_sail);

    /// <summary>引擎内部用（类型暂 internal）。</summary>
    internal ProcessInstanceManager ProcessInstances =>
        _processInstances ??= new ProcessInstanceManager(_sail);

    /// <summary>引擎内部用（类型暂 internal）。</summary>
    internal ProcessVariableManager ProcessVariables =>
        _processVariables ??= new ProcessVariableManager(_sail);

    /// <summary>引擎内部用（类型暂 internal）。</summary>
    internal ProcessVariableResolver ProcessVariableResolver =>
        _processVariableResolver ??= new ProcessVariableResolver(_sail);

    /// <summary>引擎内部用：上一步解析。</summary>
    internal PreviousStepChecker PreviousSteps =>
        _previousSteps ??= new PreviousStepChecker(_sail);

    /// <summary>引擎内部用：下一步解析。</summary>
    internal NextStepParser NextSteps =>
        _nextSteps ??= new NextStepParser(_sail);

    /// <summary>引擎内部用：加签步骤。</summary>
    internal SignForwardStepMaker SignForwardSteps =>
        _signForwardSteps ??= new SignForwardStepMaker(_sail);
}
