using System;
using mooSQL.data;

namespace saiflow.hub.Logging
{
    /// <summary>
    /// Job Log Entity
    /// 作业日志实体
    /// </summary>
    [SooTable(Name = "wf_job_log", Caption = "作业日志", Type = DBTableType.Table)]
    public class JobLogEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64)]
        public string Id { get; set; }
        [SooColumn(Name = "job_type")]
        public string JobType { get; set; }
        [SooColumn(Name = "job_name")]
        public string JobName { get; set; }
        [SooColumn(Name = "job_key")]
        public string JobKey { get; set; }
        [SooColumn(Name = "ref_class")]
        public string RefClass { get; set; }
        [SooColumn(Name = "ref_ids")]
        public string RefIDs { get; set; }
        [SooColumn(Name = "status")]
        public short? Status { get; set; }
        [SooColumn(Name = "message")]
        public string Message { get; set; }
        [SooColumn(Name = "stack_trace")]
        public string StackTrace { get; set; }
        [SooColumn(Name = "inner_stack_trace")]
        public string InnerStackTrace { get; set; }
        [SooColumn(Name = "request_data")]
        public string RequestData { get; set; }
        [SooColumn(Name = "created_datetime")]
        public DateTime? CreatedDateTime { get; set; }
        [SooColumn(Name = "created_user_id")]
        public string CreatedUserId { get; set; }
        [SooColumn(Name = "created_user_name")]
        public string CreatedUserName { get; set; }
    }
}
