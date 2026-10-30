using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using mooSQL.data;

namespace saiflow.engine.Business.Entity
{
    /// <summary>
    /// Process Instance Entity
    /// 流程实例类
    /// </summary>
    [SooTable(Name = "wf_process_instance", Caption = "流程实例", Type = DBTableType.Table)]
    public class ProcessInstanceEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        [SooColumn(Name = "process_id")]
        public string ProcessId { get; set; }
        [SooColumn(Name = "process_name")]
        public string ProcessName { get; set; }
        [SooColumn(Name = "version")]
        public string Version { get; set; }
        [SooColumn(Name = "app_name")]
        public string AppName { get; set; }
        [SooColumn(Name = "app_instance_id")]
        public string AppInstanceId { get; set; }
        [SooColumn(Name = "app_instance_code")]
        public string AppInstanceCode { get; set; }
        [SooColumn(Name = "process_state")]
        public short ProcessState { get; set; }
        [SooColumn(Name = "sub_process_type")]
        public Nullable<short> SubProcessType { get; set; }
        [SooColumn(Name = "sub_process_def_id")]
        public int SubProcessDefId { get; set; }
        [SooColumn(Name = "sub_process_Id")]
        public string SubProcessId { get; set; }
        [SooColumn(Name = "invoked_activity_instance_id")]
        public int InvokedActivityInstanceId { get; set; }
        [SooColumn(Name = "invoked_activity_id")]
        public string InvokedActivityId { get; set; }
        [SooColumn(Name = "job_timer_type")]
        public Nullable<short> JobTimerType { get; set; }
        [SooColumn(Name = "job_timer_status")]
        public Nullable<short> JobTimerStatus { get; set; }
        [SooColumn(Name = "trigger_expression")]
        public string TriggerExpression { get; set; }
        [SooColumn(Name = "overdue_datetime")]
        public Nullable<DateTime> OverdueDateTime { get; set; }
        [SooColumn(Name = "job_timer_treated_datetime")]
        public Nullable<DateTime> JobTimerTreatedDateTime { get; set; }
        [SooColumn(Name = "created_datetime")]
        public DateTime CreatedDateTime { get; set; }
        [SooColumn(Name = "created_user_id")]
        public string CreatedUserId { get; set; }
        [SooColumn(Name = "created_user_name")]
        public string CreatedUserName { get; set; }
        [SooColumn(Name = "updated_datetime")]
        public Nullable<DateTime> UpdatedDateTime { get; set; }
        [SooColumn(Name = "updated_user_id")]
        public string UpdatedUserId { get; set; }
        [SooColumn(Name = "updated_user_name")]
        public string UpdatedUserName { get; set; }
        [SooColumn(Name = "ended_datetime")]
        public Nullable<DateTime> EndedDateTime { get; set; }
        [SooColumn(Name = "ended_user_id")]
        public string EndedUserId { get; set; }
        [SooColumn(Name = "ended_user_name")]
        public string EndedUserName { get; set; }
        [SooColumn(Name = "record_status_invalid")]
        public byte RecordStatusInvalid { get; set; }
    }
}
