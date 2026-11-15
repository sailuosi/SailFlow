using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using mooSQL.data;

namespace sailflow.engine.Entity
{
    /// <summary>
    /// 活动实例实体（表 <c>wf_act_instance</c>）。
    /// 流程运行中每个节点（含会签子实例、网关）激活一次对应一行；会签主/子、退回源/原点靠多外键角色区分。
    /// </summary>
    [SooTable(Name = "wf_act_instance", Caption = "活动实例", Type = DBTableType.Table)]
    public class ActivityInstanceEntity
    {
        /// <summary>
        /// 活动实例主键（列 <c>ai_id</c>）。
        /// </summary>
        [SooColumn(Name = "ai_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string ActivityInstanceId { get; set; }

        /// <summary>
        /// 所属流程实例 Id（列 <c>ai_proc_instance_id</c>）。
        /// </summary>
        [SooColumn(Name = "ai_proc_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ProcessInstanceId { get; set; }

        /// <summary>
        /// 业务通道冗余（列 <c>ai_biz_channel</c>），与流程实例 BizChannel 对齐。
        /// </summary>
        [SooColumn(Name = "ai_biz_channel", Length = 128)]
        public string BizChannel { get; set; }

        /// <summary>
        /// 业务主键冗余（列 <c>ai_biz_pk</c>），与流程实例 BizPK 对齐。
        /// </summary>
        [SooColumn(Name = "ai_biz_pk", Length = 128)]
        public string BizPK { get; set; }

        /// <summary>
        /// 业务单据编码冗余（列 <c>ai_biz_code</c>），与流程实例 BizCode 对齐（非节点定义 BizCode）。
        /// </summary>
        [SooColumn(Name = "ai_biz_code", Length = 64)]
        public string BizCode { get; set; }

        /// <summary>
        /// 逻辑流程 Id（列 <c>ai_process_id</c>）。
        /// </summary>
        [SooColumn(Name = "ai_process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }

        /// <summary>
        /// BPMN 活动定义 Id（列 <c>ai_activity_id</c>），非表外键。
        /// </summary>
        [SooColumn(Name = "ai_activity_id", Length = 64)]
        public string ActivityId { get; set; }

        /// <summary>
        /// 活动名称快照（列 <c>ai_activity_name</c>）。
        /// </summary>
        [SooColumn(Name = "ai_activity_name", Length = 255)]
        public string ActivityName { get; set; }

        /// <summary>
        /// 活动编码快照（列 <c>ai_activity_code</c>），Gate/Hook 的 ActivityCodes 可匹配。
        /// </summary>
        [SooColumn(Name = "ai_activity_code", Length = 64)]
        public string ActivityCode { get; set; }

        /// <summary>
        /// 活动类型（列 <c>ai_activity_type</c>），见 <see cref="sailflow.engine.Common.ActivityTypeEnum"/>。
        /// 1=Start；2=End；3=Intermediate；4=Task；5=SubProcess；6=MultiSign；8=Gateway；16=Service；17=AIService；18=Script。
        /// </summary>
        [SooColumn(Name = "ai_activity_type")]
        public short? ActivityType { get; set; }

        /// <summary>
        /// 活动运行状态（列 <c>ai_activity_state</c>），见 <see cref="sailflow.engine.Common.ActivityStateEnum"/>。
        /// 1=Ready；2=Running；4=Completed；5=Suspended；6=Withdrawed；7=Sendbacked；8=Cancelled；9=Terminated。
        /// </summary>
        [SooColumn(Name = "ai_activity_state")]
        public short? ActivityState { get; set; }

        /// <summary>
        /// 是否工作项（列 <c>ai_work_item_type</c>），见 <see cref="sailflow.engine.Common.WorkItemTypeEnum"/>。
        /// 0=NonWorkItem（开始/结束/网关/中间事件）；1=IsWorkItem（任务/多实例等可生成待办）。
        /// </summary>
        [SooColumn(Name = "ai_work_item_type")]
        public short? WorkItemType { get; set; }

        /// <summary>
        /// 已分配办理人 Id 列表（列 <c>ai_asgn_user_ids</c>，JSON）。
        /// 活动级汇总；个人待办以 <see cref="TaskEntity.AssignedUserId"/> 为准。
        /// </summary>
        [SooColumn(Name = "ai_asgn_user_ids", DataType = DataFam.Json, Tag = "sail.xml")]
        public string AssignedUserIds { get; set; }

        /// <summary>
        /// 已分配办理人姓名列表（列 <c>ai_asgn_user_names</c>，JSON），与 AssignedUserIds 对应。
        /// </summary>
        [SooColumn(Name = "ai_asgn_user_names", DataType = DataFam.Json, Tag = "sail.xml")]
        public string AssignedUserNames { get; set; }

        /// <summary>
        /// 组别 Id（列 <c>ai_group_id</c>）。
        /// 会签子实例 / 单办理人场景从 Performer.GroupId 带入。
        /// </summary>
        [SooColumn(Name = "ai_group_id", Length = 64)]
        public string GroupId { get; set; }

        /// <summary>
        /// 组别名称（列 <c>ai_group_name</c>）。
        /// </summary>
        [SooColumn(Name = "ai_group_name", Length = 255)]
        public string GroupName { get; set; }

        /// <summary>
        /// 组内身份（列 <c>ai_group_role</c>，字符串，如 Leader/Member）。
        /// </summary>
        [SooColumn(Name = "ai_group_role", Length = 64)]
        public string GroupRole { get; set; }

        /// <summary>
        /// 办理单位 Id（列 <c>ai_unit_id</c>），来自 Performer.Unit*。
        /// </summary>
        [SooColumn(Name = "ai_unit_id", Length = 64)]
        public string UnitId { get; set; }

        /// <summary>
        /// 办理单位编码（列 <c>ai_unit_code</c>）。
        /// </summary>
        [SooColumn(Name = "ai_unit_code", Length = 64)]
        public string UnitCode { get; set; }

        /// <summary>
        /// 办理单位名称（列 <c>ai_unit_name</c>）。
        /// </summary>
        [SooColumn(Name = "ai_unit_name", Length = 255)]
        public string UnitName { get; set; }

        /// <summary>
        /// 网关方向类型（列 <c>ai_gw_dir_type_id</c>），见 <see cref="sailflow.engine.Common.GatewayDirectionEnum"/>。
        /// 分支：OrSplit/XOrSplit/AndSplit/AndSplitMI/ApprovalOrSplit；
        /// 合并：OrJoin/XOrJoin/EOrJoin/AndJoin/AndJoinMI。
        /// </summary>
        [SooColumn(Name = "ai_gw_dir_type_id")]
        public short? GatewayDirectionTypeId { get; set; }

        /// <summary>
        /// 禁止再续活本活动实例（列 <c>ai_no_renew_inst</c>）。
        /// 0=允许继续前进（NodeMediatorTask 用 CanNotRenewInstance==0 判断）；非 0 表示不可再续实例。
        /// </summary>
        [SooColumn(Name = "ai_no_renew_inst")]
        public byte? CanNotRenewInstance { get; set; }

        /// <summary>
        /// 审批意见态（列 <c>ai_aprv_state</c>），见 <see cref="sailflow.engine.Common.ApprovalStatusEnum"/>。
        /// -1=Refused；0=Null 默认；1=Agreed。
        /// </summary>
        [SooColumn(Name = "ai_aprv_state")]
        public short? ApprovalStatus { get; set; }

        /// <summary>
        /// 汇聚所需令牌数（列 <c>ai_tkn_req</c>）。
        /// AndJoin / AndJoinMI 等网关：到达 TokensHad &gt;= TokensRequired 才通过。
        /// </summary>
        [SooColumn(Name = "ai_tkn_req")]
        public int? TokensRequired { get; set; }

        /// <summary>
        /// 已到达令牌数（列 <c>ai_tkn_had</c>）。
        /// 并行汇聚时 CAS 自增，避免丢 token。
        /// </summary>
        [SooColumn(Name = "ai_tkn_had")]
        public int? TokensHad { get; set; }

        /// <summary>
        /// 活动级作业定时器类型（列 <c>ai_job_time_type</c>），见 <see cref="sailflow.engine.Common.JobTimerTypeEnum"/>。
        /// </summary>
        [SooColumn(Name = "ai_job_time_type")]
        public short? JobTimerType { get; set; }

        /// <summary>
        /// 活动级作业定时器状态（列 <c>ai_job_time_state</c>），见 <see cref="sailflow.engine.Common.JobTimerStatusEnum"/>。
        /// </summary>
        [SooColumn(Name = "ai_job_time_state")]
        public short? JobTimerStatus { get; set; }

        /// <summary>
        /// 触发表达式（列 <c>ai_trig_expression</c>），配合 JobTimerType。
        /// </summary>
        [SooColumn(Name = "ai_trig_expression", Length = 2000)]
        public string TriggerExpression { get; set; }

        /// <summary>
        /// 活动应完成/超期时点（列 <c>ai_overdue_time</c>）。
        /// </summary>
        [SooColumn(Name = "ai_overdue_time")]
        public DateTime? OverdueDateTime { get; set; }

        /// <summary>
        /// 活动定时器已处理时间（列 <c>ai_timer_trt_time</c>）。
        /// </summary>
        [SooColumn(Name = "ai_timer_trt_time")]
        public DateTime? JobTimerTreatedDateTime { get; set; }

        /// <summary>
        /// 会签/加签复杂类型（列 <c>ai_cplx_type</c>），见 <see cref="sailflow.engine.Xpdl.ComplexTypeEnum"/>。
        /// 1=SignTogether 会签；2=SignForward 加签。
        /// </summary>
        [SooColumn(Name = "ai_cplx_type")]
        public short? ComplexType { get; set; }

        /// <summary>
        /// 会签执行合并方式（列 <c>ai_merge_type</c>），见 <see cref="sailflow.engine.Xpdl.MergeTypeEnum"/>。
        /// 1=Sequence 串行；2=Parallel 并行。
        /// </summary>
        [SooColumn(Name = "ai_merge_type")]
        public short? MergeType { get; set; }

        /// <summary>
        /// 会签主活动实例 Id（列 <c>ai_m_act_instance_id</c>；角色 m=main）。
        /// 子实例指向主实例；主实例自身通常为空或自指策略以运行代码为准。
        /// </summary>
        [SooColumn(Name = "ai_m_act_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string MainActivityInstanceId { get; set; }

        /// <summary>
        /// 会签通过率比较类型（列 <c>ai_cmp_type</c>），见 <see cref="sailflow.engine.Xpdl.CompareTypeEnum"/>。
        /// 1=Count 个数；2=Percentage 百分比。
        /// </summary>
        [SooColumn(Name = "ai_cmp_type")]
        public short? CompareType { get; set; }

        /// <summary>
        /// 会签完成序/通过阈值（列 <c>ai_cmp_order</c>）。
        /// 子实例：串行序 1,2,3…；并行子实例常为 -1。
        /// 主实例：可存通过阈值（个数或 0~1 比例阈值，与 CompareType 配合）；加签/会签调解器据其判定是否出节点。
        /// </summary>
        [SooColumn(Name = "ai_cmp_order")]
        public float? CompleteOrder { get; set; }

        /// <summary>
        /// 加签类型（列 <c>ai_sign_fwd_type</c>），见 <see cref="sailflow.engine.Xpdl.SignForwardTypeEnum"/>。
        /// 0=None；1=SignForwardBefore 前加签；2=SignForwardBehind 后加签；3=SignForwardParallel 并加签。
        /// </summary>
        [SooColumn(Name = "ai_sign_fwd_type")]
        public short? SignForwardType { get; set; }

        /// <summary>
        /// 退回/撤销类后退类型（列 <c>ai_bwd_type</c>），见 <see cref="sailflow.engine.Common.BackwardTypeEnum"/>。
        /// 1=Withdrawed 撤销；2=Sendback 退回；3=Reversed 返签。
        /// </summary>
        [SooColumn(Name = "ai_bwd_type")]
        public short? BackwardType { get; set; }

        /// <summary>
        /// 退回源活动实例（列 <c>ai_s_act_instance_id</c>；角色 s=back_src）。
        /// 发起退回/撤销的那次活动实例。
        /// </summary>
        [SooColumn(Name = "ai_s_act_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string BackSrcActivityInstanceId { get; set; }

        /// <summary>
        /// 退回原点活动实例（列 <c>ai_o_act_instance_id</c>；角色 o=back_org）。
        /// 退回到的目标/原始活动实例。
        /// </summary>
        [SooColumn(Name = "ai_o_act_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string BackOrgActivityInstanceId { get; set; }

        /// <summary>
        /// 下一步办理人预选序列化（列 <c>ai_nxt_perf</c>，JSON）。
        /// 会签/加签路径上缓存后续步骤 Performer 映射，供继续前进消费。
        /// </summary>
        [SooColumn(Name = "ai_nxt_perf", DataType = DataFam.Json, Tag = "sail.xml")]
        public string NextStepPerformers { get; set; }

        /// <summary>
        /// 创建人 Id（列 <c>ai_crt_user_id</c>）。
        /// </summary>
        [SooColumn(Name = "ai_crt_user_id", Length = 64)]
        public string CreatedUserId { get; set; }

        /// <summary>
        /// 创建人姓名（列 <c>ai_crt_user_name</c>）。
        /// </summary>
        [SooColumn(Name = "ai_crt_user_name", Length = 128)]
        public string CreatedUserName { get; set; }

        /// <summary>
        /// 创建时间（列 <c>ai_crt_time</c>）。
        /// </summary>
        [SooColumn(Name = "ai_crt_time")]
        public DateTime? CreatedDateTime { get; set; }

        /// <summary>
        /// 更新人 Id（列 <c>ai_upd_user_id</c>）。
        /// </summary>
        [SooColumn(Name = "ai_upd_user_id", Length = 64)]
        public string UpdatedUserId { get; set; }

        /// <summary>
        /// 更新人姓名（列 <c>ai_upd_user_name</c>）。
        /// </summary>
        [SooColumn(Name = "ai_upd_user_name", Length = 128)]
        public string UpdatedUserName { get; set; }

        /// <summary>
        /// 更新时间（列 <c>ai_upd_time</c>）。
        /// </summary>
        [SooColumn(Name = "ai_upd_time")]
        public DateTime? UpdatedDateTime { get; set; }

        /// <summary>
        /// 办结时间（列 <c>ai_end_time</c>）。
        /// </summary>
        [SooColumn(Name = "ai_end_time")]
        public DateTime? EndedDateTime { get; set; }

        /// <summary>
        /// 办结人 Id（列 <c>ai_end_user_id</c>）。
        /// </summary>
        [SooColumn(Name = "ai_end_user_id", Length = 64)]
        public string EndedUserId { get; set; }

        /// <summary>
        /// 办结人姓名（列 <c>ai_end_user_name</c>）。
        /// </summary>
        [SooColumn(Name = "ai_end_user_name", Length = 128)]
        public string EndedUserName { get; set; }

        /// <summary>
        /// 记录作废标记（列 <c>ai_rec_invalid</c>）：0=有效；1=软删。
        /// </summary>
        [SooColumn(Name = "ai_rec_invalid")]
        public byte? RecordStatusInvalid { get; set; }
    }
}
