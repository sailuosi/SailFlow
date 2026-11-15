using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sailflow.engine.Common;
using sailflow.engine.Utility;
using mooSQL.data;

namespace sailflow.engine.Entity
{
    /// <summary>
    /// 任务明细视图（视图 <c>vw_wf_task_detail</c>）。
    /// 投影任务 + 活动实例 + 流程实例关键列，供待办列表/Workbench 一次读取；列名前缀随源表。
    /// </summary>
    [SooTable(Name = "vw_wf_task_detail", Caption = "任务明细视图", Type = DBTableType.View)]
    public class TaskViewEntity
    {
        /// <summary>
        /// 任务主键（列 <c>tk_id</c>）。
        /// </summary>
        [SooColumn(Name = "tk_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64)]
        public string TaskId { get; set; }

        /// <summary>
        /// 业务通道（列 <c>tk_biz_channel</c>）。
        /// </summary>
        [SooColumn(Name = "tk_biz_channel")]
        public string BizChannel { get; set; }

        /// <summary>
        /// 业务主键（列 <c>tk_biz_pk</c>）。
        /// </summary>
        [SooColumn(Name = "tk_biz_pk")]
        public string BizPK { get; set; }

        /// <summary>
        /// 业务单据编码（列 <c>pi_biz_code</c>，来自流程实例）。
        /// </summary>
        [SooColumn(Name = "pi_biz_code")]
        public string BizCode { get; set; }

        /// <summary>
        /// 流程实例标题（列 <c>pi_title</c>）；空时展示可回落 ProcessName。
        /// </summary>
        [SooColumn(Name = "pi_title")]
        public string Title { get; set; }

        /// <summary>
        /// 流程名称快照（列 <c>pi_name</c>），定义名副本，非标题。
        /// </summary>
        [SooColumn(Name = "pi_name")]
        public string ProcessName { get; set; }

        /// <summary>
        /// 流程实例 Id（列 <c>tk_proc_instance_id</c>）。
        /// </summary>
        [SooColumn(Name = "tk_proc_instance_id", Length = 64)]
        public string ProcessInstanceId { get; set; }

        /// <summary>
        /// 逻辑流程 Id（列 <c>pi_process_id</c>）。
        /// </summary>
        [SooColumn(Name = "pi_process_id")]
        public string ProcessId { get; set; }

        /// <summary>
        /// 流程版本（列 <c>pi_version</c>）。
        /// </summary>
        [SooColumn(Name = "pi_version")]
        public string Version { get; set; }

        /// <summary>
        /// 活动定义 Id（列 <c>tk_activity_id</c>）。
        /// </summary>
        [SooColumn(Name = "tk_activity_id")]
        public string ActivityId { get; set; }

        /// <summary>
        /// 活动实例 Id（列 <c>tk_act_instance_id</c>）。
        /// </summary>
        [SooColumn(Name = "tk_act_instance_id", Length = 64)]
        public string ActivityInstanceId { get; set; }

        /// <summary>
        /// 活动名称（列 <c>ai_activity_name</c>）。
        /// </summary>
        [SooColumn(Name = "ai_activity_name")]
        public string ActivityName { get; set; }

        /// <summary>
        /// 活动编码（列 <c>ai_activity_code</c>）。
        /// </summary>
        [SooColumn(Name = "ai_activity_code")]
        public string ActivityCode { get; set; }

        /// <summary>
        /// 活动类型（列 <c>ai_activity_type</c>），见 <see cref="ActivityTypeEnum"/>。
        /// </summary>
        [SooColumn(Name = "ai_activity_type")]
        public short? ActivityType { get; set; }

        /// <summary>
        /// 是否工作项（列 <c>ai_work_item_type</c>），见 <see cref="WorkItemTypeEnum"/>。
        /// </summary>
        [SooColumn(Name = "ai_work_item_type")]
        public short? WorkItemType { get; set; }

        /// <summary>
        /// 任务类型（列 <c>tk_task_type</c>）：1 人工 / 2 自动化。
        /// </summary>
        [SooColumn(Name = "tk_task_type")]
        public short? TaskType { get; set; }

        /// <summary>
        /// 子流程类型（列 <c>pi_sub_proc_type</c>），见 <see cref="SubProcessTypeEnum"/>。
        /// </summary>
        [SooColumn(Name = "pi_sub_proc_type")]
        public short? SubProcessType { get; set; }

        /// <summary>
        /// 外部引用子流程定义 Id（列 <c>pi_sub_process_id</c>）。
        /// </summary>
        [SooColumn(Name = "pi_sub_process_id", Length = 64)]
        public string SubProcessDefId { get; set; }

        /// <summary>
        /// 子流程逻辑 Id（列 <c>pi_sub_proc_id</c>）。
        /// </summary>
        [SooColumn(Name = "pi_sub_proc_id")]
        public string SubProcessId { get; set; }

        /// <summary>
        /// 被委托原任务 Id（列 <c>tk_entr_task_id</c>）。
        /// </summary>
        [SooColumn(Name = "tk_entr_task_id", Length = 64)]
        public string EntrustedTaskId { get; set; }

        /// <summary>
        /// 办理人角色（列 <c>tk_assignee_role</c>）：null/0=常规；1=沟通参与者。见 <see cref="AssigneeRoleEnum"/>。
        /// </summary>
        [SooColumn(Name = "tk_assignee_role")]
        public int? AssigneeRole { get; set; }

        /// <summary>
        /// 沟通源审批 TaskId（列 <c>tk_consult_src_task_id</c>）；参与者待办填写。
        /// </summary>
        [SooColumn(Name = "tk_consult_src_task_id", Length = 64)]
        public string ConsultSrcTaskId { get; set; }

        /// <summary>
        /// 会签主活动实例 Id（列 <c>ai_m_act_instance_id</c>）。
        /// </summary>
        [SooColumn(Name = "ai_m_act_instance_id", Length = 64)]
        public string MainActivityInstanceId { get; set; }

        /// <summary>
        /// 会签序/通过阈值（列 <c>ai_cmp_order</c>）；串行 1,2,3…，并行子实例常 -1。
        /// </summary>
        [SooColumn(Name = "ai_cmp_order")]
        public float? CompleteOrder { get; set; }

        /// <summary>
        /// 办理人 Id（列 <c>tk_asgn_user_id</c>）。
        /// </summary>
        [SooColumn(Name = "tk_asgn_user_id")]
        public string AssignedUserId { get; set; }

        /// <summary>
        /// 办理人姓名（列 <c>tk_asgn_user_name</c>）。
        /// </summary>
        [SooColumn(Name = "tk_asgn_user_name")]
        public string AssignedUserName { get; set; }

        /// <summary>
        /// 组别 Id（列 <c>tk_group_id</c>）。
        /// </summary>
        [SooColumn(Name = "tk_group_id")]
        public string GroupId { get; set; }

        /// <summary>
        /// 组别名称（列 <c>tk_group_name</c>）。
        /// </summary>
        [SooColumn(Name = "tk_group_name")]
        public string GroupName { get; set; }

        /// <summary>
        /// 组内身份（列 <c>tk_group_role</c>）。
        /// </summary>
        [SooColumn(Name = "tk_group_role")]
        public string GroupRole { get; set; }

        /// <summary>
        /// 办理单位 Id（列 <c>tk_unit_id</c>）。
        /// </summary>
        [SooColumn(Name = "tk_unit_id")]
        public string UnitId { get; set; }

        /// <summary>
        /// 办理单位编码（列 <c>tk_unit_code</c>）。
        /// </summary>
        [SooColumn(Name = "tk_unit_code")]
        public string UnitCode { get; set; }

        /// <summary>
        /// 办理单位名称（列 <c>tk_unit_name</c>）。
        /// </summary>
        [SooColumn(Name = "tk_unit_name")]
        public string UnitName { get; set; }

        /// <summary>
        /// 业务侧待办邮件发送标记（列 <c>tk_is_email_sent</c>；见 <see cref="TaskEntity.IsEMailSent"/>）。
        /// </summary>
        [SooColumn(Name = "tk_is_email_sent")]
        public byte? IsEMailSent { get; set; }

        /// <summary>
        /// 活动审批意见态（列 <c>ai_aprv_state</c>），见 <see cref="ApprovalStatusEnum"/>。
        /// </summary>
        [SooColumn(Name = "ai_aprv_state")]
        public short? ApprovalStatus { get; set; }

        /// <summary>
        /// 任务创建时间（列 <c>tk_crt_time</c>）。
        /// </summary>
        [SooColumn(Name = "tk_crt_time")]
        public DateTime? CreatedDateTime { get; set; }

        /// <summary>
        /// 待办激活时间（列 <c>tk_activated_time</c>）；串行未轮到为 null。
        /// </summary>
        [SooColumn(Name = "tk_activated_time")]
        public DateTime? ActivatedDateTime { get; set; }

        /// <summary>
        /// 个人应完成时间（列 <c>tk_overdue_time</c>）；未激活为 null。
        /// </summary>
        [SooColumn(Name = "tk_overdue_time")]
        public DateTime? OverdueDateTime { get; set; }

        /// <summary>
        /// 任务更新时间（列 <c>tk_upd_time</c>）。
        /// </summary>
        [SooColumn(Name = "tk_upd_time")]
        public DateTime? UpdatedDateTime { get; set; }

        /// <summary>
        /// 任务办结时间（列 <c>tk_end_time</c>）。
        /// </summary>
        [SooColumn(Name = "tk_end_time")]
        public DateTime? EndedDateTime { get; set; }

        /// <summary>
        /// 办结人 Id（列 <c>tk_end_user_id</c>）。
        /// </summary>
        [SooColumn(Name = "tk_end_user_id")]
        public string EndedUserId { get; set; }

        /// <summary>
        /// 办结人姓名（列 <c>tk_end_user_name</c>）。
        /// </summary>
        [SooColumn(Name = "tk_end_user_name")]
        public string EndedUserName { get; set; }

        /// <summary>
        /// 任务状态（列 <c>tk_task_state</c>），见 <see cref="TaskStateEnum"/>。
        /// </summary>
        [SooColumn(Name = "tk_task_state")]
        public short? TaskState { get; set; }

        /// <summary>
        /// 当前活动实例状态（列 <c>ai_activity_state</c>），见 <see cref="ActivityStateEnum"/>。
        /// </summary>
        [SooColumn(Name = "ai_activity_state")]
        public short? ActivityState { get; set; }

        /// <summary>
        /// 会签主活动实例状态（列 <c>ai_mn_activity_state</c>）。
        /// 子实例行上投影主活动状态，便于列表判断节点整体进度。
        /// </summary>
        [SooColumn(Name = "ai_mn_activity_state")]
        public short? MainActivityState { get; set; }

        /// <summary>
        /// 任务作废标记（列 <c>tk_rec_invalid</c>）。
        /// </summary>
        [SooColumn(Name = "tk_rec_invalid")]
        public byte? RecordStatusInvalid { get; set; }

        /// <summary>
        /// 流程实例状态（列 <c>pi_process_state</c>），见 <see cref="ProcessStateEnum"/>。
        /// </summary>
        [SooColumn(Name = "pi_process_state")]
        public short? ProcessState { get; set; }
    }
}
