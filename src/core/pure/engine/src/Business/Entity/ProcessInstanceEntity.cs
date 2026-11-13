using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using mooSQL.data;

namespace sailflow.engine.Entity
{
    /// <summary>
    /// Process Instance Entity
    /// 流程实例类
    /// </summary>
    [SooTable(Name = "wf_process_instance", Caption = "流程实例", Type = DBTableType.Table)]
    public class ProcessInstanceEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64, Tag = "sail.id")]
        public string ProcessInstanceId { get; set; }
        [SooColumn(Name = "process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }
        [SooColumn(Name = "process_name", Length = 255)]
        public string ProcessName { get; set; }
        [SooColumn(Name = "version", Length = 32)]
        public string Version { get; set; }
        [SooColumn(Name = "app_name", Length = 128)]
        public string AppName { get; set; }
        [SooColumn(Name = "app_instance_id", Length = 128)]
        public string AppInstanceId { get; set; }
        [SooColumn(Name = "app_instance_code", Length = 64)]
        public string AppInstanceCode { get; set; }
        [SooColumn(Name = "process_state")]
        public short? ProcessState { get; set; }
        [SooColumn(Name = "sub_process_type")]
        public short? SubProcessType { get; set; }
        [SooColumn(Name = "sub_process_def_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string SubProcessDefId { get; set; }
        [SooColumn(Name = "sub_process_Id", Length = 64, Tag = "sail.id")]
        public string SubProcessId { get; set; }
        [SooColumn(Name = "invoked_activity_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string InvokedActivityInstanceId { get; set; }
        [SooColumn(Name = "invoked_activity_id", Length = 64)]
        public string InvokedActivityId { get; set; }
        [SooColumn(Name = "job_timer_type")]
        public short? JobTimerType { get; set; }
        [SooColumn(Name = "job_timer_status")]
        public short? JobTimerStatus { get; set; }
        [SooColumn(Name = "trigger_expression", Length = 2000)]
        public string TriggerExpression { get; set; }
        [SooColumn(Name = "overdue_datetime")]
        public DateTime? OverdueDateTime { get; set; }
        [SooColumn(Name = "job_timer_treated_datetime")]
        public DateTime? JobTimerTreatedDateTime { get; set; }
        /// <summary>发起单位键（可选；不覆盖节点任务 Unit*）。</summary>
        [SooColumn(Name = "start_unit_id", Length = 64)]
        public string StartUnitId { get; set; }
        [SooColumn(Name = "start_unit_code", Length = 64)]
        public string StartUnitCode { get; set; }
        [SooColumn(Name = "start_unit_name", Length = 255)]
        public string StartUnitName { get; set; }
        [SooColumn(Name = "created_datetime")]
        public DateTime? CreatedDateTime { get; set; }
        [SooColumn(Name = "created_user_id", Length = 64)]
        public string CreatedUserId { get; set; }
        [SooColumn(Name = "created_user_name", Length = 128)]
        public string CreatedUserName { get; set; }
        [SooColumn(Name = "updated_datetime")]
        public DateTime? UpdatedDateTime { get; set; }
        [SooColumn(Name = "updated_user_id", Length = 64)]
        public string UpdatedUserId { get; set; }
        [SooColumn(Name = "updated_user_name", Length = 128)]
        public string UpdatedUserName { get; set; }
        [SooColumn(Name = "ended_datetime")]
        public DateTime? EndedDateTime { get; set; }
        [SooColumn(Name = "ended_user_id", Length = 64)]
        public string EndedUserId { get; set; }
        [SooColumn(Name = "ended_user_name", Length = 128)]
        public string EndedUserName { get; set; }
        [SooColumn(Name = "record_status_invalid")]
        public byte? RecordStatusInvalid { get; set; }
    }
}
