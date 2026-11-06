using System;
using mooSQL.data;
using saiflow.engine.Client;
using saiflow.engine.Entity;
using saiflow.engine.Xpdl;

namespace saiflow.engine.Factories;

/// <summary>
/// A · 默认 ProcessModel 工厂：D2 转发现行 static <see cref="ProcessModelFactory"/>（行为不变）。
/// </summary>
public sealed class DefaultProcessModelFactory : IProcessModelFactory
{
    private readonly SailClient _sail;

    public DefaultProcessModelFactory(SailClient sail)
    {
        _sail = sail ?? throw new ArgumentNullException(nameof(sail));
    }

    public IProcessModel CreateByProcess(ProcessEntity entity)
        => ProcessModelFactory.CreateByProcess(_sail, entity);

    public IProcessModel CreateByProcess(string processId, string version)
        => ProcessModelFactory.CreateByProcess(_sail, processId, version);

    public IProcessModel CreateByProcess(SQLBuilder kit, string processId, string version)
        => ProcessModelFactory.CreateByProcess(_sail, kit, processId, version);

    public IProcessModel CreateByProcessInstance(ProcessInstanceEntity processInstance)
        => ProcessModelFactory.CreateByProcessInstance(_sail, processInstance);

    public IProcessModel CreateByProcessInstance(SQLBuilder kit, ProcessInstanceEntity processInstance)
        => ProcessModelFactory.CreateByProcessInstance(_sail, kit, processInstance);

    public IProcessModel CreateByTask(TaskViewEntity taskView)
        => ProcessModelFactory.CreateByTask(_sail, taskView);

    public IProcessModel CreateByTask(SQLBuilder kit, TaskViewEntity taskView)
        => ProcessModelFactory.CreateByTask(_sail, kit, taskView);

    public void ClearCache(string processId, string version)
        => ProcessModelFactory.ClearCache(processId, version);

    public void ClearAllCache()
        => ProcessModelFactory.ClearAllCache();

    public void Invalidate(string processId, string version)
        => ProcessModelFactory.Invalidate(_sail, processId, version);
}
