using System.Collections.Generic;
using mooSQL.data;
using sailflow.data;

namespace sailflow.storage
{
    /// <summary>
    /// 流程变量表纯库交互（kit 首参、无状态）。
    /// 物理表名由 Manager/Router 解析后传入，Storage 不持分片策略。
    /// </summary>
    public interface IProcessVariableStorage
    {
        ProcessVariableEntity GetById(SQLBuilder kit, string physicalTable, string variableId);

        string Insert(SQLBuilder kit, ProcessVariableEntity entity, string physicalTable = null);

        void Update(SQLBuilder kit, ProcessVariableEntity entity, string physicalTable = null);

        void DeleteById(SQLBuilder kit, string physicalTable, string variableId);

        void DeleteByProcessInstance(SQLBuilder kit, string physicalTable, string processInstanceId);

        IList<ProcessVariableEntity> ListByProcessInstance(
            SQLBuilder kit,
            string physicalTable,
            string processInstanceId);

        IList<ProcessVariableEntity> ListByActivityInstance(
            SQLBuilder kit,
            string physicalTable,
            string processInstanceId,
            string activityInstanceId);

        IList<ProcessVariableEntity> ListByProcessScopeName(
            SQLBuilder kit,
            string physicalTable,
            string processInstanceId,
            string name);

        IList<ProcessVariableEntity> ListByActivityScopeName(
            SQLBuilder kit,
            string physicalTable,
            string processInstanceId,
            string activityInstanceId,
            string name);

        IList<ProcessVariableEntity> ListByActivityScopeNames(
            SQLBuilder kit,
            string physicalTable,
            string processInstanceId,
            string activityInstanceId,
            IList<string> names);
    }
}
