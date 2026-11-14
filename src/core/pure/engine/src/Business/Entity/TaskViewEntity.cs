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
    /// Task View
    /// 任务视图类
    /// </summary>
    [SooTable(Name = "vw_wf_task_detail", Caption = "任务明细视图", Type = DBTableType.View)]
    public class TaskViewEntity
    {
        [SooColumn(Name = "tk_id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64)]
        public string TaskId { get; set; }

        [SooColumn(Name = "tk_app_name")]
        public string AppName { get; set; }

        [SooColumn(Name = "tk_app_instance_id")]
        public string AppInstanceId { get; set; }

        [SooColumn(Name = "pi_app_instance_code")]
        public string AppInstanceCode { get; set; }

        [SooColumn(Name = "tk_proc_instance_id", Length = 64)]
        public string ProcessInstanceId { get; set; }

        [SooColumn(Name = "pi_process_id")]
        public string ProcessId { get; set; }

        [SooColumn(Name = "pi_version")]
        public string Version { get; set; }

        [SooColumn(Name = "tk_activity_id")]
        public string ActivityId { get; set; }

        [SooColumn(Name = "tk_act_instance_id", Length = 64)]
        public string ActivityInstanceId { get; set; }

        [SooColumn(Name = "ai_activity_name")]
        public string ActivityName { get; set; }

        [SooColumn(Name = "ai_activity_code")]
        public string ActivityCode { get; set; }

        [SooColumn(Name = "ai_activity_type")]
        public short? ActivityType { get; set; }

        [SooColumn(Name = "ai_work_item_type")]
        public short? WorkItemType { get; set; }

        [SooColumn(Name = "tk_task_type")]
        public short? TaskType { get; set; }

        [SooColumn(Name = "pi_sub_proc_type")]
        public short? SubProcessType { get; set; }

        [SooColumn(Name = "pi_sub_process_id", Length = 64)]
        public string SubProcessDefId { get; set; }

        [SooColumn(Name = "pi_sub_proc_id")]
        public string SubProcessId { get; set; }

        [SooColumn(Name = "tk_entr_task_id", Length = 64)]
        public string EntrustedTaskId { get; set; }        //被委托任务Id

        [SooColumn(Name = "ai_m_act_instance_id", Length = 64)]
        public string MainActivityInstanceId { get; set; }

        [SooColumn(Name = "ai_cmp_order")]
        public float? CompleteOrder { get; set; }

        [SooColumn(Name = "tk_asgn_user_id")]
        public string AssignedUserId { get; set; }

        [SooColumn(Name = "tk_asgn_user_name")]
        public string AssignedUserName { get; set; }

        [SooColumn(Name = "tk_group_id")]
        public string GroupId { get; set; }

        [SooColumn(Name = "tk_group_name")]
        public string GroupName { get; set; }

        [SooColumn(Name = "tk_group_role")]
        public string GroupRole { get; set; }

        [SooColumn(Name = "tk_unit_id")]
        public string UnitId { get; set; }

        [SooColumn(Name = "tk_unit_code")]
        public string UnitCode { get; set; }

        [SooColumn(Name = "tk_unit_name")]
        public string UnitName { get; set; }

        /// <summary>
        /// 业务侧待办邮件发送标记（非引擎主路径语义；见 TaskEntity.IsEMailSent）。
        /// </summary>
        [SooColumn(Name = "tk_is_email_sent")]
        public byte? IsEMailSent { get; set; }

        [SooColumn(Name = "ai_aprv_state")]
        public short? ApprovalStatus { get; set; }

        [SooColumn(Name = "tk_crt_time")]
        public DateTime? CreatedDateTime { get; set; }

        /// <summary>待办激活（可办）时间；串行未轮到为 null。</summary>
        [SooColumn(Name = "tk_activated_time")]
        public DateTime? ActivatedDateTime { get; set; }

        /// <summary>个人应完成时间；未激活为 null。</summary>
        [SooColumn(Name = "tk_overdue_time")]
        public DateTime? OverdueDateTime { get; set; }

        [SooColumn(Name = "tk_upd_time")]
        public DateTime? UpdatedDateTime { get; set; }

        [SooColumn(Name = "tk_end_time")]
        public DateTime? EndedDateTime { get; set; }

        [SooColumn(Name = "tk_end_user_id")]
        public string EndedUserId { get; set; }

        [SooColumn(Name = "tk_end_user_name")]
        public string EndedUserName { get; set; }

        [SooColumn(Name = "tk_task_state")]
        public short? TaskState { get; set; }

        [SooColumn(Name = "ai_activity_state")]
        public short? ActivityState { get; set; }

        [SooColumn(Name = "ai_mn_activity_state")]
        public short? MainActivityState { get; set; }

        [SooColumn(Name = "tk_rec_invalid")]
        public byte? RecordStatusInvalid { get; set; }

        [SooColumn(Name = "pi_process_state")]
        public short? ProcessState { get; set; }
    }
}