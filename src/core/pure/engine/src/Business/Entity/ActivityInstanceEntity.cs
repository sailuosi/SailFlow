using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using mooSQL.data;

namespace saiflow.engine.Business.Entity
{
    /// <summary>
    /// Activity Instance Entity
    /// 活动实例的实体对象
    /// </summary>
    [SooTable(Name = "wf_activity_instance", Caption = "活动实例", Type = DBTableType.Table)]
    public class ActivityInstanceEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int? Id { get; set; }
        [SooColumn(Name = "process_instance_id")]
        public int? ProcessInstanceId { get; set; }
        [SooColumn(Name = "app_name")]
        public string AppName { get; set; }
        [SooColumn(Name = "app_instance_id")]
        public string AppInstanceId { get; set; }
        [SooColumn(Name = "app_instance_code")]
        public string AppInstanceCode { get; set; }
        [SooColumn(Name = "process_id")]
        public string ProcessId { get; set; }
        [SooColumn(Name = "activity_id")]
        public string ActivityId { get; set; }
        [SooColumn(Name = "activity_name")]
        public string ActivityName { get; set; }
        [SooColumn(Name = "activity_code")]
        public string ActivityCode { get; set; }
        [SooColumn(Name = "activity_type")]
        public short? ActivityType { get; set; }
        [SooColumn(Name = "activity_state")]
        public short? ActivityState { get; set; }
        [SooColumn(Name = "work_item_type")]
        public short? WorkItemType { get; set; }
        [SooColumn(Name = "assigned_user_ids")]
        public string AssignedUserIds { get; set; }
        [SooColumn(Name = "assigned_user_names")]
        public string AssignedUserNames { get; set; }
        /// <summary>组别（会签子实例 / 单办理人场景）。</summary>
        [SooColumn(Name = "group_id")]
        public string GroupId { get; set; }
        [SooColumn(Name = "group_name")]
        public string GroupName { get; set; }
        /// <summary>组内身份（string）。</summary>
        [SooColumn(Name = "group_role")]
        public string GroupRole { get; set; }
        [SooColumn(Name = "unit_id")]
        public string UnitId { get; set; }
        [SooColumn(Name = "unit_code")]
        public string UnitCode { get; set; }
        [SooColumn(Name = "unit_name")]
        public string UnitName { get; set; }
        [SooColumn(Name = "gateway_direction_type_id")]
        public short? GatewayDirectionTypeId { get; set; }
        [SooColumn(Name = "cannot_renew_instance")]
        public byte? CanNotRenewInstance { get; set; }
        [SooColumn(Name = "approval_status")]
        public short? ApprovalStatus { get; set; }
        [SooColumn(Name = "tokens_required")]
        public int? TokensRequired { get; set; }
        [SooColumn(Name = "tokens_had")]
        public int? TokensHad { get; set; }
        [SooColumn(Name = "job_timer_type")]
        public short? JobTimerType { get; set; }
        [SooColumn(Name = "job_timer_status")]
        public short? JobTimerStatus { get; set; }
        [SooColumn(Name = "trigger_expression")]
        public string TriggerExpression { get; set; }
        [SooColumn(Name = "overdue_datetime")]
        public DateTime? OverdueDateTime { get; set; }
        [SooColumn(Name = "job_timer_treated_datetime")]
        public DateTime? JobTimerTreatedDateTime { get; set; }
        /// <summary>
        /// Sign Together /Sign Forward Type
        /// 会签或加签类型       
        /// </summary>
        [SooColumn(Name = "complex_type")]
        public short? ComplexType { get; set; }
        /// <summary>
        /// Sign Together Execution Type: Sequecne / Parallel
        /// 会签执行类型：串行或并行
        /// </summary>
        [SooColumn(Name = "merge_type")]
        public short? MergeType { get; set; }
        [SooColumn(Name = "main_activity_instance_id")]
        public int? MainActivityInstanceId { get; set; }
        /// <summary>
        /// Sign Together Passover Type: Count / Percentage
        /// 会签通过率类型：个数或百分比
        /// </summary>
        [SooColumn(Name = "compare_type")]
        public short? CompareType { get; set; }
        /// <summary>
        /// Sign Together Order
        /// Sequence:[1, 2, 3...]
        /// Prallele:[-1]
        /// 会签执行顺序：串行为1，2，3 并行为-1
        /// </summary>
        [SooColumn(Name = "complete_order")]
        public float? CompleteOrder { get; set; }
        [SooColumn(Name = "sign_forward_type")]
        public short? SignForwardType { get; set; }
        [SooColumn(Name = "backward_type")]
        public short? BackwardType { get; set; }
        [SooColumn(Name = "back_src_activity_instance_id")]
        public int? BackSrcActivityInstanceId { get; set; }
        [SooColumn(Name = "back_org_activity_instance_id")]
        public int? BackOrgActivityInstanceId { get; set; }
        [SooColumn(Name = "next_step_performers")]
        public string NextStepPerformers { get; set; }
        [SooColumn(Name = "created_user_id")]
        public string CreatedUserId { get; set; }
        [SooColumn(Name = "created_user_name")]
        public string CreatedUserName { get; set; }
        [SooColumn(Name = "created_datetime")]
        public DateTime? CreatedDateTime { get; set; }
        [SooColumn(Name = "updated_user_id")]
        public string UpdatedUserId { get; set; }
        [SooColumn(Name = "updated_user_name")]
        public string UpdatedUserName { get; set; }
        [SooColumn(Name = "updated_datetime")]
        public DateTime? UpdatedDateTime { get; set; }
        [SooColumn(Name = "ended_datetime")]
        public DateTime? EndedDateTime { get; set; }
        [SooColumn(Name = "ended_user_id")]
        public string EndedUserId { get; set; }
        [SooColumn(Name = "ended_user_name")]
        public string EndedUserName { get; set; }
        [SooColumn(Name = "record_status_invalid")]
        public byte? RecordStatusInvalid { get; set; }
    }
}
