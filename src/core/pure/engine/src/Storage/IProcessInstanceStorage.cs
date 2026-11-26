using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        Task<ProcessInstanceEntity> GetByIdAsync(
            SQLBuilder kit,
            string processInstanceId,
            CancellationToken cancellationToken = default);

        string Insert(SQLBuilder kit, ProcessInstanceEntity entity);

        void Update(SQLBuilder kit, ProcessInstanceEntity entity);

        void DeleteById(SQLBuilder kit, string processInstanceId);

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

        /// <summary>按 BizPK+ProcessId+Version 批量 Discard（含 RecordStatusInvalid）。</summary>
        int DiscardByVersion(
            SQLBuilder kit,
            string bizPK,
            string processId,
            string version,
            string updatedUserId,
            string updatedUserName);

        /// <summary>召回子流程：按 InvokedActInstanceId + Suspended 查最新一条。</summary>
        IList<ProcessInstanceEntity> ListSuspendedByInvokedActivity(
            SQLBuilder kit,
            string invokedActivityInstanceId);

        ProcessInstanceEntity GetByVersion(SQLBuilder kit, string processId, string version);

        ProcessInstanceEntity GetCurrentValid(
            SQLBuilder kit,
            string bizPK,
            string processId,
            string version);

        IList<ProcessInstanceEntity> ListValidByBiz(SQLBuilder kit, string bizPK);

        IList<ProcessInstanceEntity> ListValidByBizProcessVersion(
            SQLBuilder kit,
            string bizPK,
            string processId,
            string version);

        IList<ProcessInstanceEntity> ListValidSubProcess(
            SQLBuilder kit,
            string bizPK,
            string processId,
            string subProcessId);

        IList<ProcessInstanceEntity> ListValidReadyOrRunning(
            SQLBuilder kit,
            string bizPK,
            string processId);

        IList<ProcessInstanceEntity> ListCompletedByInvoked(
            SQLBuilder kit,
            string invokedActivityInstanceId,
            string invokedActivityId);
    }
}
