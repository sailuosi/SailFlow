using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using sailflow.data;

namespace sailflow.storage
{
    /// <summary>
    /// 待办任务表 / 任务视图纯库交互（kit 首参、无状态）。
    /// 热点：状态/办结列更新，勿热路径整行 Update。
    /// </summary>
    public interface ITaskStorage
    {
        TaskEntity GetById(SQLBuilder kit, string taskId);

        TaskViewEntity GetViewById(SQLBuilder kit, string taskId);

        Task<TaskViewEntity> GetViewByIdAsync(
            SQLBuilder kit,
            string taskId,
            CancellationToken cancellationToken = default);

        string Insert(SQLBuilder kit, TaskEntity entity);

        void Update(SQLBuilder kit, TaskEntity entity);

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

        IList<TaskEntity> ListByActivity(
            SQLBuilder kit,
            string processInstanceId,
            string activityInstanceId);

        IList<TaskEntity> ListByActivityIds(
            SQLBuilder kit,
            string processInstanceId,
            IReadOnlyList<string> activityInstanceIds);

        IList<TaskViewEntity> ListViewsByActivity(
            SQLBuilder kit,
            string processInstanceId,
            string activityInstanceId);

        Task<IList<TaskViewEntity>> ListViewsByActivityAsync(
            SQLBuilder kit,
            string processInstanceId,
            string activityInstanceId,
            CancellationToken cancellationToken = default);

        /// <summary>本人在活动实例上的办理任务视图（ProcessState=2, ActivityType∈4/5/6, ActivityState∈1/2）。</summary>
        IList<TaskViewEntity> ListMineViewsByActivityInstance(
            SQLBuilder kit,
            string activityInstanceId,
            string userId);

        Task<IList<TaskViewEntity>> ListMineViewsByActivityInstanceAsync(
            SQLBuilder kit,
            string activityInstanceId,
            string userId,
            CancellationToken cancellationToken = default);

        /// <summary>本人在 Biz+Process 上的办理任务视图（含 WorkItemType=1 OR）。</summary>
        IList<TaskViewEntity> ListMineViewsByBizProcess(
            SQLBuilder kit,
            string bizPK,
            string processId,
            string userId);

        /// <summary>按沟通源 Task 取 Waiting/Reading 行（Participant 过滤留 Manager）。</summary>
        IList<TaskEntity> ListOpenByConsultSrc(SQLBuilder kit, string consultSrcTaskId);

        /// <summary>按委托源 TaskId 取被委托任务（至多一条语义由调用方处理）。</summary>
        TaskEntity GetByEntrustedTaskId(SQLBuilder kit, string entrustedTaskId);
    }
}
