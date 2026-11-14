using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using mooSQL.data;

namespace sailflow.engine.Entity
{
    /// <summary>
    /// Activity Instance Entity
    /// 活动实例的实体对象
    /// </summary>
    [SooTable(Name = "wf_act_instance", Caption = "活动实例", Type = DBTableType.Table)]
    public class ActivityInstanceEntity
    {
        [SooColumn(Name = "ai_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string ActivityInstanceId { get; set; }
        [SooColumn(Name = "ai_proc_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ProcessInstanceId { get; set; }
        [SooColumn(Name = "ai_app_name", Length = 128)]
        public string AppName { get; set; }
        [SooColumn(Name = "ai_app_instance_id", Length = 128)]
        public string AppInstanceId { get; set; }
        [SooColumn(Name = "ai_app_instance_code", Length = 64)]
        public string AppInstanceCode { get; set; }
        [SooColumn(Name = "ai_process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }
        [SooColumn(Name = "ai_activity_id", Length = 64)]
        public string ActivityId { get; set; }
        [SooColumn(Name = "ai_activity_name", Length = 255)]
        public string ActivityName { get; set; }
        [SooColumn(Name = "ai_activity_code", Length = 64)]
        public string ActivityCode { get; set; }
        [SooColumn(Name = "ai_activity_type")]
        public short? ActivityType { get; set; }
        [SooColumn(Name = "ai_activity_state")]
        public short? ActivityState { get; set; }
        [SooColumn(Name = "ai_work_item_type")]
        public short? WorkItemType { get; set; }
        [SooColumn(Name = "ai_asgn_user_ids", DataType = DataFam.Json, Tag = "sail.xml")]
        public string AssignedUserIds { get; set; }
        [SooColumn(Name = "ai_asgn_user_names", DataType = DataFam.Json, Tag = "sail.xml")]
        public string AssignedUserNames { get; set; }
        /// <summary>组别（会签子实例 / 单办理人场景）。</summary>
        [SooColumn(Name = "ai_group_id", Length = 64)]
        public string GroupId { get; set; }
        [SooColumn(Name = "ai_group_name", Length = 255)]
        public string GroupName { get; set; }
        /// <summary>组内身份（string）。</summary>
        [SooColumn(Name = "ai_group_role", Length = 64)]
        public string GroupRole { get; set; }
        [SooColumn(Name = "ai_unit_id", Length = 64)]
        public string UnitId { get; set; }
        [SooColumn(Name = "ai_unit_code", Length = 64)]
        public string UnitCode { get; set; }
        [SooColumn(Name = "ai_unit_name", Length = 255)]
        public string UnitName { get; set; }
        [SooColumn(Name = "ai_gw_dir_type_id")]
        public short? GatewayDirectionTypeId { get; set; }
        [SooColumn(Name = "ai_no_renew_inst")]
        public byte? CanNotRenewInstance { get; set; }
        [SooColumn(Name = "ai_aprv_state")]
        public short? ApprovalStatus { get; set; }
        [SooColumn(Name = "ai_tkn_req")]
        public int? TokensRequired { get; set; }
        [SooColumn(Name = "ai_tkn_had")]
        public int? TokensHad { get; set; }
        [SooColumn(Name = "ai_job_time_type")]
        public short? JobTimerType { get; set; }
        [SooColumn(Name = "ai_job_time_state")]
        public short? JobTimerStatus { get; set; }
        [SooColumn(Name = "ai_trig_expression", Length = 2000)]
        public string TriggerExpression { get; set; }
        [SooColumn(Name = "ai_overdue_time")]
        public DateTime? OverdueDateTime { get; set; }
        [SooColumn(Name = "ai_timer_trt_time")]
        public DateTime? JobTimerTreatedDateTime { get; set; }
        /// <summary>
        /// Sign Together /Sign Forward Type
        /// 会签或加签类型       
        /// </summary>
        [SooColumn(Name = "ai_cplx_type")]
        public short? ComplexType { get; set; }
        /// <summary>
        /// Sign Together Execution Type: Sequecne / Parallel
        /// 会签执行类型：串行或并行
        /// </summary>
        [SooColumn(Name = "ai_merge_type")]
        public short? MergeType { get; set; }
                /// <summary>会签主活动实例；含义 m=main。</summary>
        [SooColumn(Name = "ai_m_act_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string MainActivityInstanceId { get; set; }
        /// <summary>
        /// Sign Together Passover Type: Count / Percentage
        /// 会签通过率类型：个数或百分比
        /// </summary>
        [SooColumn(Name = "ai_cmp_type")]
        public short? CompareType { get; set; }
        /// <summary>
        /// Sign Together Order
        /// Sequence:[1, 2, 3...]
        /// Prallele:[-1]
        /// 会签执行顺序：串行为1，2，3 并行为-1
        /// </summary>
        [SooColumn(Name = "ai_cmp_order")]
        public float? CompleteOrder { get; set; }
        [SooColumn(Name = "ai_sign_fwd_type")]
        public short? SignForwardType { get; set; }
        [SooColumn(Name = "ai_bwd_type")]
        public short? BackwardType { get; set; }
                /// <summary>退回源活动实例；含义 s=back_src。</summary>
        [SooColumn(Name = "ai_s_act_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string BackSrcActivityInstanceId { get; set; }
        /// <summary>退回原点活动实例；含义 o=back_org。</summary>
        [SooColumn(Name = "ai_o_act_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string BackOrgActivityInstanceId { get; set; }
        [SooColumn(Name = "ai_nxt_perf", DataType = DataFam.Json, Tag = "sail.xml")]
        public string NextStepPerformers { get; set; }
        [SooColumn(Name = "ai_crt_user_id", Length = 64)]
        public string CreatedUserId { get; set; }
        [SooColumn(Name = "ai_crt_user_name", Length = 128)]
        public string CreatedUserName { get; set; }
        [SooColumn(Name = "ai_crt_time")]
        public DateTime? CreatedDateTime { get; set; }
        [SooColumn(Name = "ai_upd_user_id", Length = 64)]
        public string UpdatedUserId { get; set; }
        [SooColumn(Name = "ai_upd_user_name", Length = 128)]
        public string UpdatedUserName { get; set; }
        [SooColumn(Name = "ai_upd_time")]
        public DateTime? UpdatedDateTime { get; set; }
        [SooColumn(Name = "ai_end_time")]
        public DateTime? EndedDateTime { get; set; }
        [SooColumn(Name = "ai_end_user_id", Length = 64)]
        public string EndedUserId { get; set; }
        [SooColumn(Name = "ai_end_user_name", Length = 128)]
        public string EndedUserName { get; set; }
        [SooColumn(Name = "ai_rec_invalid")]
        public byte? RecordStatusInvalid { get; set; }
    }
}