using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using saiflow.engine.Common;
using saiflow.engine.Utility;
using saiflow.engine.Business;
using mooSQL.data;

namespace saiflow.engine.Business.Entity
{
    /// <summary>
    /// Task View
    /// 任务视图类
    /// </summary>
    [SooTable(Name = "vw_wf_task_details", Caption = "任务明细视图", Type = DBTableType.View)]
    public class TaskViewEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int? Id { get; set; }

        [SooColumn(Name = "app_name")]
        public string AppName { get; set; }

        [SooColumn(Name = "app_instance_id")]
        public string AppInstanceId { get; set; }

        [SooColumn(Name = "app_instance_code")]
        public string AppInstanceCode { get; set; }

        [SooColumn(Name = "process_instance_id")]
        public int? ProcessInstanceId { get; set; }

        [SooColumn(Name = "process_id")]
        public string ProcessId { get; set; }

        [SooColumn(Name = "version")]
        public string Version { get; set; }

        [SooColumn(Name = "activity_id")]
        public string ActivityId { get; set; }

        [SooColumn(Name = "activity_instance_id")]
        public int? ActivityInstanceId { get; set; }

        [SooColumn(Name = "activity_name")]
        public string ActivityName { get; set; }

        [SooColumn(Name = "activity_code")]
        public string ActivityCode { get; set; }

        [SooColumn(Name = "activity_type")]
        public short? ActivityType { get; set; }

        [SooColumn(Name = "work_item_type")]
        public short? WorkItemType { get; set; }

        [SooColumn(Name = "task_type")]
        public short? TaskType { get; set; }

        [SooColumn(Name = "sub_process_type")]
        public short? SubProcessType { get; set; }

        [SooColumn(Name = "sub_process_def_id")]
        public int? SubProcessDefId { get; set; }

        [SooColumn(Name = "sub_process_id")]
        public string SubProcessId { get; set; }

        [SooColumn(Name = "entrusted_task_id")]
        public int? EntrustedTaskId { get; set; }        //被委托任务Id

        [SooColumn(Name = "main_activity_instance_id")]
        public int? MainActivityInstanceId { get; set; }

        [SooColumn(Name = "complete_order")]
        public float? CompleteOrder { get; set; }

        [SooColumn(Name = "assigned_user_id")]
        public string AssignedUserId { get; set; }

        [SooColumn(Name = "assigned_user_name")]
        public string AssignedUserName { get; set; }

        [SooColumn(Name = "group_id")]
        public string GroupId { get; set; }

        [SooColumn(Name = "group_name")]
        public string GroupName { get; set; }

        [SooColumn(Name = "group_role")]
        public string GroupRole { get; set; }

        [SooColumn(Name = "unit_id")]
        public string UnitId { get; set; }

        [SooColumn(Name = "unit_code")]
        public string UnitCode { get; set; }

        [SooColumn(Name = "unit_name")]
        public string UnitName { get; set; }

        [SooColumn(Name = "is_email_sent")]
        public byte? IsEMailSent { get; set; }

        [SooColumn(Name = "approval_status")]
        public short? ApprovalStatus { get; set; }

        [SooColumn(Name = "created_datetime")]
        public DateTime? CreatedDateTime { get; set; }

        [SooColumn(Name = "updated_datetime")]
        public DateTime? UpdatedDateTime { get; set; }

        [SooColumn(Name = "ended_datetime")]
        public DateTime? EndedDateTime { get; set; }

        [SooColumn(Name = "ended_user_id")]
        public string EndedUserId { get; set; }

        [SooColumn(Name = "ended_user_name")]
        public string EndedUserName { get; set; }

        [SooColumn(Name = "task_state")]
        public short? TaskState { get; set; }

        [SooColumn(Name = "activity_state")]
        public short? ActivityState { get; set; }

        [SooColumn(Name = "main_activity_state")]
        public short? MainActivityState { get; set; }

        [SooColumn(Name = "record_status_invalid")]
        public byte? RecordStatusInvalid { get; set; }

        [SooColumn(Name = "process_state")]
        public short? ProcessState { get; set; }
    }
}
