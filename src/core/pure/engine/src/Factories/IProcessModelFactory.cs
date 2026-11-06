using mooSQL.data;
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using saiflow.engine.Xpdl;

namespace saiflow.engine.Factories;

/// <summary>
/// A · ProcessModel 工厂槽：每 Client 一套；缓存属本实例。
/// 在途 B 沿用创建时模型引用；失效只影响之后的 Create。
/// </summary>
public interface IProcessModelFactory
{
    /// <summary>按 Client.Config 创建外部 XPDL 存储（无配置则 null）。</summary>
    IXPDLStorage CreateXpdlStorage();

    IProcessModel CreateByProcess(ProcessEntity entity);

    IProcessModel CreateByProcess(string processId, string version);

    IProcessModel CreateByProcess(SQLBuilder kit, string processId, string version);

    IProcessModel CreateByProcessInstance(ProcessInstanceEntity processInstance);

    IProcessModel CreateByProcessInstance(SQLBuilder kit, ProcessInstanceEntity processInstance);

    IProcessModel CreateByTask(TaskViewEntity taskView);

    IProcessModel CreateByTask(SQLBuilder kit, TaskViewEntity taskView);

    IProcessModel CreateSubByNode(SQLBuilder kit, SubProcessNode subProcessNode);

    void ClearCache(string processId, string version);

    void ClearAllCache();

    void Invalidate(string processId, string version);
}
