using System;
using saiflow.engine.Client;
using saiflow.engine.Manager;

namespace saiflow.engine.Factories;

/// <summary>
/// A · 默认 Manager 工厂：按 Client 懒创建；不持 Runner/kit。
/// </summary>
public sealed class DefaultManagerFactory : IManagerFactory
{
    private readonly SailClient _sail;
    private ActivityInstanceManager _activities;
    private TaskManager _tasks;
    private ProcessManager _processes;
    private ProcessInstanceManager _processInstances;

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

    /// <summary>引擎内部用（类型暂 internal）。</summary>
    internal ProcessInstanceManager ProcessInstances =>
        _processInstances ??= new ProcessInstanceManager(_sail);
}
