using System.Collections.Generic;
using mooSQL.data;
using sailflow.data;

namespace sailflow.storage
{
    /// <summary>
    /// 流程变量<strong>定义</strong>表纯库（<c>sf_variable</c>；kit 首参）。
    /// 与运行时 <see cref="IProcessVariableStorage"/>（实例变量）不同。
    /// </summary>
    public interface IVariableDefinitionStorage
    {
        IList<VariableEntity> ListByActivity(
            SQLBuilder kit,
            string processId,
            string version,
            string activityId);

        VariableEntity GetById(SQLBuilder kit, string variableId);

        string Insert(SQLBuilder kit, VariableEntity entity);

        void Update(SQLBuilder kit, VariableEntity entity);

        void DeleteById(SQLBuilder kit, string variableId);

        void DeleteByProcessActivity(
            SQLBuilder kit,
            string processId,
            string version,
            string activityId);
    }
}
