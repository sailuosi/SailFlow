using sailflow.data;
using sailflow.run;

namespace sailflow.engine.Factories;

/// <summary>
/// A · Manager 工厂槽：每 <see cref="Client.SailClient"/> 一套；懒持有 Manager；唯一 <c>new Manager</c> 入口在默认实现（sailflow.runtime）。
/// </summary>
public interface IManagerFactory
{
    IActivityInstanceManager Activities { get; }

    ITaskManager Tasks { get; }

    IProcessManager Processes { get; }

    IProcessInstanceManager ProcessInstances { get; }

    IProcessVariableManager ProcessVariables { get; }

    IProcessVariableResolver ProcessVariableResolver { get; }

    ITransitionInstanceManager Transitions { get; }

    IVariableManager Variables { get; }

    IHookOutboxManager HookOutbox { get; }

    IRuleSetManager RuleSets { get; }

    IRuleSetExecutionManager RuleSetExecutions { get; }

    IJobInfoManager JobInfos { get; }

    ILogManager Logs { get; }

    IPreviousStepChecker PreviousSteps { get; }

    INextStepParser NextSteps { get; }

    ISignForwardStepMaker SignForwardSteps { get; }
}
