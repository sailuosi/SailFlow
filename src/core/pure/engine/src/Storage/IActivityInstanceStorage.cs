using System;
using System.Collections.Generic;
using mooSQL.data;
using sailflow.data;

namespace sailflow.storage
{
    /// <summary>
    /// 活动实例表纯库交互（kit 首参、无状态）。
    /// </summary>
    public interface IActivityInstanceStorage
    {
        ActivityInstanceEntity GetById(SQLBuilder kit, string activityInstanceId);

        /// <summary>仅更新 ActivityState + Updated*（如 Running/Suspended）。</summary>
        int UpdateState(
            SQLBuilder kit,
            string activityInstanceId,
            short newState,
            string updatedUserId = null,
            string updatedUserName = null);

        /// <summary>办结：ActivityState + Ended*。</summary>
        int EndState(
            SQLBuilder kit,
            string activityInstanceId,
            short newState,
            DateTime endedDateTime,
            string endedUserId = null,
            string endedUserName = null);

        /// <summary>
        /// 条件取消单节点：仅当当前态 ∈ Ready/Running/Suspended。
        /// </summary>
        int CancelOpen(
            SQLBuilder kit,
            string activityInstanceId,
            string updatedUserId = null,
            string updatedUserName = null);

        /// <summary>
        /// 按主节点批量改子节点状态（where MainActInstanceId + whereIn fromStates）。
        /// </summary>
        int UpdateChildrenState(
            SQLBuilder kit,
            string mainActivityInstanceId,
            short newState,
            IReadOnlyList<short> fromStates,
            string updatedUserId = null,
            string updatedUserName = null);
    }
}
