using System.Collections.Generic;
using mooSQL.data;
using sailflow.data;

namespace sailflow.storage
{
    /// <summary>
    /// 转移轨迹表（<see cref="TrackEntity"/> / <c>sf_track</c>）纯库交互（kit 首参、无状态）。
    /// 物理表名由 Manager/Router 解析后传入，Storage 不持分片策略。
    /// </summary>
    public interface ITransitionInstanceStorage
    {
        /// <summary>插入轨迹行；<paramref name="physicalTable"/> 非空时写入热分表。</summary>
        string Insert(SQLBuilder kit, TrackEntity entity, string physicalTable = null);

        void DeleteByProcessInstance(SQLBuilder kit, string physicalTable, string processInstanceId);

        IList<TrackEntity> ListWorkItemByBiz(
            SQLBuilder kit,
            string physicalTable,
            string bizPK,
            string processId);

        IList<TrackEntity> ListByBizAndToActType(
            SQLBuilder kit,
            string physicalTable,
            string bizPK,
            string processId,
            short toActivityType);

        IList<TrackEntity> ListByProcessInstance(
            SQLBuilder kit,
            string physicalTable,
            string bizPK,
            string processId,
            string processInstanceId);

        IList<TrackEntity> ListByFromActivityInstance(
            SQLBuilder kit,
            string physicalTable,
            string fromActivityInstanceId);

        /// <summary>从拆分节点出发的轨迹条数（默认表；非热分表路径）。</summary>
        int CountFromSplit(
            SQLBuilder kit,
            string processInstanceId,
            string splitActivityId,
            string splitActivityInstanceId);

        /// <summary>
        /// 网关拆分后有效目标活动（Join Track→AI；状态 ∈ Ready/Running/Completed/Suspended）。
        /// </summary>
        IList<ActivityInstanceEntity> ListValidActivitiesAfterGateway(
            SQLBuilder kit,
            string splitGatewayInstanceId);
    }
}
