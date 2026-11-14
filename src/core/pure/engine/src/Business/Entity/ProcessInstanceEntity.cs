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
    [SooTable(Name = "wf_proc_instance", Caption = "流程实例", Type = DBTableType.Table)]
    public class ProcessInstanceEntity
    {
        [SooColumn(Name = "pi_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string ProcessInstanceId { get; set; }
        [SooColumn(Name = "pi_process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }
        /// <summary>流程名称（定义名副本；不是实例标题）。</summary>
        [SooColumn(Name = "pi_name", Length = 255)]
        public string ProcessName { get; set; }
        /// <summary>流程实例标题。</summary>
        [SooColumn(Name = "pi_title", Length = 255)]
        public string Title { get; set; }
        [SooColumn(Name = "pi_version", Length = 32)]
        public string Version { get; set; }
        [SooColumn(Name = "pi_biz_type", Length = 64)]
        public string BizType { get; set; }
        [SooColumn(Name = "pi_biz_channel", Length = 128)]
        public string BizChannel { get; set; }
        [SooColumn(Name = "pi_biz_pk", Length = 128)]
        public string BizPK { get; set; }
        [SooColumn(Name = "pi_biz_code", Length = 64)]
        public string BizCode { get; set; }
        [SooColumn(Name = "pi_biz_name", Length = 128)]
        public string BizName { get; set; }
        [SooColumn(Name = "pi_process_state")]
        public short? ProcessState { get; set; }
        [SooColumn(Name = "pi_sub_proc_type")]
        public short? SubProcessType { get; set; }
        [SooColumn(Name = "pi_sub_process_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string SubProcessDefId { get; set; }
        [SooColumn(Name = "pi_sub_proc_id", Length = 64, Tag = "sail.id")]
        public string SubProcessId { get; set; }
                /// <summary>唤起方活动实例；含义 i=invoked（ik+act_instance 超 20）。</summary>
        [SooColumn(Name = "pi_i_act_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string InvokedActivityInstanceId { get; set; }
        [SooColumn(Name = "pi_i_activity_id", Length = 64)]
        public string InvokedActivityId { get; set; }
        [SooColumn(Name = "pi_job_time_type")]
        public short? JobTimerType { get; set; }
        [SooColumn(Name = "pi_job_time_state")]
        public short? JobTimerStatus { get; set; }
        [SooColumn(Name = "pi_trig_expression", Length = 2000)]
        public string TriggerExpression { get; set; }
        [SooColumn(Name = "pi_overdue_time")]
        public DateTime? OverdueDateTime { get; set; }
        [SooColumn(Name = "pi_timer_trt_time")]
        public DateTime? JobTimerTreatedDateTime { get; set; }
        /// <summary>发起单位键（可选；不覆盖节点任务 Unit*）。</summary>
        [SooColumn(Name = "pi_start_unit_id", Length = 64)]
        public string StartUnitId { get; set; }
        [SooColumn(Name = "pi_start_unit_code", Length = 64)]
        public string StartUnitCode { get; set; }
        [SooColumn(Name = "pi_start_unit_name", Length = 255)]
        public string StartUnitName { get; set; }
        [SooColumn(Name = "pi_crt_time")]
        public DateTime? CreatedDateTime { get; set; }
        [SooColumn(Name = "pi_crt_user_id", Length = 64)]
        public string CreatedUserId { get; set; }
        [SooColumn(Name = "pi_crt_user_name", Length = 128)]
        public string CreatedUserName { get; set; }
        [SooColumn(Name = "pi_upd_time")]
        public DateTime? UpdatedDateTime { get; set; }
        [SooColumn(Name = "pi_upd_user_id", Length = 64)]
        public string UpdatedUserId { get; set; }
        [SooColumn(Name = "pi_upd_user_name", Length = 128)]
        public string UpdatedUserName { get; set; }
        [SooColumn(Name = "pi_end_time")]
        public DateTime? EndedDateTime { get; set; }
        [SooColumn(Name = "pi_end_user_id", Length = 64)]
        public string EndedUserId { get; set; }
        [SooColumn(Name = "pi_end_user_name", Length = 128)]
        public string EndedUserName { get; set; }
        [SooColumn(Name = "pi_rec_invalid")]
        public byte? RecordStatusInvalid { get; set; }
    }
}