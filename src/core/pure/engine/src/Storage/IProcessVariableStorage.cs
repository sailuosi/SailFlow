using mooSQL.data;

namespace sailflow.storage
{
    /// <summary>
    /// 流程变量表纯库交互（kit 首参、无状态）。物理表由 Manager/Router 传入。
    /// </summary>
    public interface IProcessVariableStorage
    {
        void DeleteByProcessInstance(SQLBuilder kit, string physicalTable, string processInstanceId);
    }
}
