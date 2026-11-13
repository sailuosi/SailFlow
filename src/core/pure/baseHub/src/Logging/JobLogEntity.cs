using System;
using mooSQL.data;

namespace sailflow.hub.Logging
{
    /// <summary>
    /// Job Log Entity
    /// 作业日志实体
    /// </summary>
    [SooTable(Name = "wf_job_log", Caption = "作业日志", Type = DBTableType.Table)]
    public class JobLogEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64, Tag = "sail.id")]
        public string Id { get; set; }
        [SooColumn(Name = "job_type", Length = 64)]
        public string JobType { get; set; }
        [SooColumn(Name = "job_name", Length = 255)]
        public string JobName { get; set; }
        [SooColumn(Name = "job_key", Length = 128)]
        public string JobKey { get; set; }
        [SooColumn(Name = "ref_class", Length = 255)]
        public string RefClass { get; set; }
        [SooColumn(Name = "ref_ids", Length = 500)]
        public string RefIDs { get; set; }
        [SooColumn(Name = "status")]
        public short? Status { get; set; }
        [SooColumn(Name = "message", Length = 500)]
        public string Message { get; set; }
        [SooColumn(Name = "stack_trace", DataType = DataFam.Json, Tag = "sail.xml")]
        public string StackTrace { get; set; }
        [SooColumn(Name = "inner_stack_trace", DataType = DataFam.Json, Tag = "sail.xml")]
        public string InnerStackTrace { get; set; }
        [SooColumn(Name = "request_data", DataType = DataFam.Json, Tag = "sail.xml")]
        public string RequestData { get; set; }
        [SooColumn(Name = "created_datetime")]
        public DateTime? CreatedDateTime { get; set; }
        [SooColumn(Name = "created_user_id", Length = 64)]
        public string CreatedUserId { get; set; }
        [SooColumn(Name = "created_user_name", Length = 128)]
        public string CreatedUserName { get; set; }
    }
}
