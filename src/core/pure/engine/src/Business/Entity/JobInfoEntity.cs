using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data;

namespace saiflow.engine.Business.Entity
{
    /// <summary>
    /// Job Info Entity
    /// 作业信息
    /// </summary>
    [SooTable(Name = "wf_job_info", Caption = "作业信息", Type = DBTableType.Table)]
    public class JobInfoEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        [SooColumn(Name = "job_name")]
        public string JobName { get; set; }
        [SooColumn(Name = "topic")]
        public string Topic { get; set; }
        [SooColumn(Name = "process_id")]
        public string ProcessId { get; set; }
        [SooColumn(Name = "process_name")]
        public string ProcessName { get; set; }
        [SooColumn(Name = "version")]
        public string Version { get; set; }
        [SooColumn(Name = "activity_id")]
        public string ActivityId { get; set; }
        [SooColumn(Name = "activity_name")]
        public string ActivityName { get; set; }
        [SooColumn(Name = "activity_type")]
        public string ActivityType { get; set; }
        [SooColumn(Name = "trigger_type")]
        public string TriggerType { get; set; }
        [SooColumn(Name = "message_direction")]
        public string MessageDirection { get; set; }
        [SooColumn(Name = "job_status")]
        public string JobStatus { get; set; }
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
    }
}
