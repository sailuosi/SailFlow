using System;
using mooSQL.data;
using sailflow.engine.Entity;
using sailflow.engine.Result;
using sailflow.engine.Xpdl;

namespace sailflow.engine.Runtime;

/// <summary>
/// B · Runtime 命令执行表面（工厂 Create 产物）。具体实现仍为引擎内部 <c>WfRuntimeManager</c>。
/// </summary>
public interface IWfRuntime
{
    bool Execute(SQLBuilder kit);

    WfExecutedResult WfExecutedResult { get; }

    string ProcessInstanceId { get; }

    ActivityInstanceEntity RunningActivityInstance { get; }

    TaskViewEntity TaskView { get; }

    IProcessModel ProcessModel { get; }

    event EventHandler<WfEventArgs> OnWfProcessExecuted;
}
