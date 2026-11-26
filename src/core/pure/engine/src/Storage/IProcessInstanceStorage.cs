using System;
using mooSQL.data;
using sailflow.data;

namespace sailflow.storage
{
    /// <summary>
    /// 流程实例表纯库交互（kit 首参、无状态）。
    /// 热点：状态列更新走 <see cref="UpdateState"/>，勿热路径整行 Update。
    /// </summary>
    public interface IProcessInstanceStorage
    {
        ProcessInstanceEntity GetById(SQLBuilder kit, string processInstanceId);

        /// <summary>
        /// 仅更新状态及相关审计列；返回影响行数。
        /// <paramref name="endedDateTime"/> 非 null 时同步写 Ended*（Complete 场景）。
        /// </summary>
        int UpdateState(
            SQLBuilder kit,
            string processInstanceId,
            short newState,
            string updatedUserId = null,
            string updatedUserName = null,
            DateTime? endedDateTime = null,
            string endedUserId = null,
            string endedUserName = null);
    }
}
