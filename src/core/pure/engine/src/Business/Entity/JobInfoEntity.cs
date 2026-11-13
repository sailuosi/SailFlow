using System;
using mooSQL.data;

namespace sailflow.engine.Entity
{
    /// <summary>
    /// Job Info Entity
    /// </summary>
    [SooTable(Name = "wf_job_info", Caption = "作业信息", Type = DBTableType.Table)]
    public class JobInfoEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64, Tag = "sail.id")]
        public string JobInfoId { get; set; }
        [SooColumn(Name = "job_name", Length = 255)]
        public string JobName { get; set; }
        [SooColumn(Name = "topic", Length = 128)]
        public string Topic { get; set; }
        [SooColumn(Name = "process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }
        [SooColumn(Name = "process_name", Length = 255)]
        public string ProcessName { get; set; }
        [SooColumn(Name = "version", Length = 32)]
        public string Version { get; set; }
        [SooColumn(Name = "activity_id", Length = 64)]
        public string ActivityId { get; set; }
        [SooColumn(Name = "activity_name", Length = 255)]
        public string ActivityName { get; set; }
        [SooColumn(Name = "activity_type", Length = 64)]
        public string ActivityType { get; set; }
        [SooColumn(Name = "trigger_type", Length = 64)]
        public string TriggerType { get; set; }
        [SooColumn(Name = "message_direction", Length = 64)]
        public string MessageDirection { get; set; }
        [SooColumn(Name = "job_status", Length = 64)]
        public string JobStatus { get; set; }
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
    }
}
