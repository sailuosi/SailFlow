using System;
using mooSQL.data;
using sailflow.data;

namespace sailflow.storage
{
    /// <summary>
    /// 待办任务表纯库交互（kit 首参、无状态）。
    /// 热点：状态/办结列更新，勿热路径整行 Update。
    /// </summary>
    public interface ITaskStorage
    {
        TaskEntity GetById(SQLBuilder kit, string taskId);

        /// <summary>仅更新 TaskState + Updated*（如 Reading）。</summary>
        int UpdateState(
            SQLBuilder kit,
            string taskId,
            short newState,
            string updatedUserId = null,
            string updatedUserName = null);

        /// <summary>办结：TaskState + CompletedAt + Ended* + 可选 EndType/Msg。</summary>
        int EndState(
            SQLBuilder kit,
            string taskId,
            short newState,
            DateTime completedAt,
            string endedUserId = null,
            string endedUserName = null,
            string endType = null,
            string msg = null);
    }
}
