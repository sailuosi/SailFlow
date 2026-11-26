using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        IList<ActivityInstanceEntity> ListByIds(SQLBuilder kit, IEnumerable<string> activityInstanceIds);

        Task<ActivityInstanceEntity> GetByIdAsync(
            SQLBuilder kit,
            string activityInstanceId,
            CancellationToken cancellationToken = default);

        IList<ActivityInstanceEntity> ListByProcessInstance(SQLBuilder kit, string processInstanceId);

        ActivityInstanceEntity GetLatestByProcessInstanceActivity(
            SQLBuilder kit,
            string processInstanceId,
            string activityId);

        IList<ActivityInstanceEntity> ListByRunningProcessBizProcessActivity(
            SQLBuilder kit,
            string bizPK,
            string processId,
            string activityId);

        IList<ActivityInstanceEntity> ListReadyOrRunningByRunningProcessVersionActivity(
            SQLBuilder kit,
            string processId,
            string version,
            string activityId);

        IList<ActivityInstanceEntity> ListByProcessInstanceActivityState(
            SQLBuilder kit,
            string processInstanceId,
            string activityId,
            short activityState);

        IList<ActivityInstanceEntity> ListCompletedByRunningProcessBizProcess(
            SQLBuilder kit,
            string bizPK,
            string processId);

        IList<ActivityInstanceEntity> ListCompletedByProcessInstanceActivity(
            SQLBuilder kit,
            string processInstanceId,
            string activityId);

        IList<ActivityInstanceEntity> ListByTaskJoin(SQLBuilder kit, string taskId);

        IList<ActivityInstanceEntity> ListRunningReadyByBizProcessVersion(
            SQLBuilder kit,
            string bizPK,
            string processId,
            string version);

        Task<IList<ActivityInstanceEntity>> ListRunningReadyByBizProcessVersionAsync(
            SQLBuilder kit,
            string bizPK,
            string processId,
            string version,
            CancellationToken cancellationToken = default);

        IList<ActivityInstanceEntity> ListCompletedMultipleInstanceByMain(
            SQLBuilder kit,
            string mainActivityInstanceId);

        IList<ActivityInstanceEntity> ListCompletedByProcessInstanceActivityOrderByEndedDesc(
            SQLBuilder kit,
            string processInstanceId,
            string activityId);

        IList<ActivityInstanceEntity> ListMultipleInstanceChildren(
            SQLBuilder kit,
            string mainActivityInstanceId,
            string processInstanceId,
            short? activityState);

        int CountTracksFromSplit(
            SQLBuilder kit,
            string processInstanceId,
            string splitActivityId,
            string splitActivityInstanceId);

        IList<ActivityInstanceEntity> ListValidSplitedAfterGateway(
            SQLBuilder kit,
            string splitGatewayInstanceId);

        IList<ActivityInstanceEntity> ListValidMultipleInstanceChildren(
            SQLBuilder kit,
            string mainActivityInstanceId,
            string processInstanceId);

        string Insert(SQLBuilder kit, ActivityInstanceEntity entity);

        void Update(SQLBuilder kit, ActivityInstanceEntity entity);

        void DeleteById(SQLBuilder kit, string activityInstanceId);

        void DeleteByProcessInstance(SQLBuilder kit, string processInstanceId);

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

        /// <summary>CAS TokensHad：where 旧值 → 新值；成功返回 &gt;0。</summary>
        int CasIncreaseTokensHad(
            SQLBuilder kit,
            string activityInstanceId,
            int? previousTokensHad,
            int nextTokensHad);

        /// <summary>网关区间内打开态活动置 CanNotRenewInstance=1。</summary>
        int SetBlockedCanNotRenew(
            SQLBuilder kit,
            string processInstanceId,
            IReadOnlyList<string> activityIds);

        /// <summary>Terminate：打开态活动批量 Terminated + Ended*。</summary>
        int TerminateOpenByProcessInstance(
            SQLBuilder kit,
            string processInstanceId,
            DateTime endedDateTime,
            string endedUserId = null,
            string endedUserName = null);

        /// <summary>解析活动所属 ProcessId（供 Transition 分表路由）。</summary>
        string GetProcessId(SQLBuilder kit, string activityInstanceId);
    }
}
