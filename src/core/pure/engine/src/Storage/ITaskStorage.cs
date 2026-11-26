using System;
using System.Collections.Generic;
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

        string Insert(SQLBuilder kit, TaskEntity entity);

        void DeleteById(SQLBuilder kit, string taskId);

        void DeleteByProcessInstance(SQLBuilder kit, string processInstanceId);

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

        /// <summary>按活动实例取消待办（TaskState=48 + RecordStatusInvalid）。</summary>
        int CancelByActivityInstance(SQLBuilder kit, string activityInstanceId);

        /// <summary>列出进程下 Waiting/Reading 待办（Terminate 发事件前快照）。</summary>
        IList<TaskEntity> ListOpenByProcessInstance(SQLBuilder kit, string processInstanceId);

        /// <summary>批量取消进程下 Waiting/Reading 待办。</summary>
        int CancelOpenByProcessInstance(
            SQLBuilder kit,
            string processInstanceId,
            DateTime completedAt,
            string endedUserId = null,
            string endedUserName = null);
    }
}
