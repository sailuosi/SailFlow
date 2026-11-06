using saiflow.engine.Client;
using saiflow.engine.Manager;

namespace saiflow.engine.Factories;

/// <summary>
/// A · Manager 工厂槽：每 <see cref="SailClient"/> 一套；懒持有 Manager；D3 起唯一 <c>new Manager</c> 入口。
/// </summary>
public interface IManagerFactory
{
    ActivityInstanceManager Activities { get; }

    TaskManager Tasks { get; }

    ProcessManager Processes { get; }

    TransitionInstanceManager Transitions { get; }

    VariableManager Variables { get; }

    HookOutboxManager HookOutbox { get; }

    RuleSetManager RuleSets { get; }

    RuleSetExecutionManager RuleSetExecutions { get; }

    JobInfoManager JobInfos { get; }

    LogManager Logs { get; }
}
