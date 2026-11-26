using System.Collections.Generic;
using mooSQL.data;

namespace sailflow.storage
{
    /// <summary>
    /// Pick 自选落库仓纯库（kit 首参；表 <c>sf_picked_assignee</c> 由宿主具化）。
    /// 与 <see cref="IPreselectAssigneeStorage"/> <b>分表</b>。覆盖写入语义下 Clear/Replace 为物理删。
    /// </summary>
    public interface IPickedAssigneeStorage
    {
        IReadOnlyList<AssigneeItemRow> ListByActivity(
            SQLBuilder kit,
            string processInstanceId,
            string activityId);

        void ReplaceByActivity(
            SQLBuilder kit,
            string processInstanceId,
            string activityId,
            IReadOnlyList<AssigneeItemRow> rows);

        void ClearByActivity(SQLBuilder kit, string processInstanceId, string activityId);
    }
}
