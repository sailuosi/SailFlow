using System;
using mooSQL.data;

namespace sailflow.engine.Entity
{
    /// <summary>
    /// Job Info Entity
    /// </summary>
    [SooTable(Name = "wf_job_detail", Caption = "作业信息", Type = DBTableType.Table)]
    public class JobInfoEntity
    {
        [SooColumn(Name = "jb_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string JobInfoId { get; set; }
        [SooColumn(Name = "jb_name", Length = 255)]
        public string JobName { get; set; }
        [SooColumn(Name = "jb_topic", Length = 128)]
        public string Topic { get; set; }
        [SooColumn(Name = "jb_process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }
        [SooColumn(Name = "jb_proc_name", Length = 255)]
        public string ProcessName { get; set; }
        [SooColumn(Name = "jb_version", Length = 32)]
        public string Version { get; set; }
        [SooColumn(Name = "jb_activity_id", Length = 64)]
        public string ActivityId { get; set; }
        [SooColumn(Name = "jb_activity_name", Length = 255)]
        public string ActivityName { get; set; }
        [SooColumn(Name = "jb_activity_type", Length = 64)]
        public string ActivityType { get; set; }
        [SooColumn(Name = "jb_trigger_type", Length = 64)]
        public string TriggerType { get; set; }
        [SooColumn(Name = "jb_msg_direction", Length = 64)]
        public string MessageDirection { get; set; }
        [SooColumn(Name = "jb_job_state", Length = 64)]
        public string JobStatus { get; set; }
        [SooColumn(Name = "jb_crt_time")]
        public DateTime? CreatedDateTime { get; set; }
        [SooColumn(Name = "jb_crt_user_id", Length = 64)]
        public string CreatedUserId { get; set; }
        [SooColumn(Name = "jb_crt_user_name", Length = 128)]
        public string CreatedUserName { get; set; }
        [SooColumn(Name = "jb_upd_time")]
        public DateTime? UpdatedDateTime { get; set; }
        [SooColumn(Name = "jb_upd_user_id", Length = 64)]
        public string UpdatedUserId { get; set; }
        [SooColumn(Name = "jb_upd_user_name", Length = 128)]
        public string UpdatedUserName { get; set; }
    }
}