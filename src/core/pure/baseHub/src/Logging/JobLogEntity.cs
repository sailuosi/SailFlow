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
        [SooColumn(Name = "jl_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string Id { get; set; }
        [SooColumn(Name = "jl_job_type", Length = 64)]
        public string JobType { get; set; }
        [SooColumn(Name = "jl_job_name", Length = 255)]
        public string JobName { get; set; }
        [SooColumn(Name = "jl_job_key", Length = 128)]
        public string JobKey { get; set; }
        [SooColumn(Name = "jl_ref_cls", Length = 255)]
        public string RefClass { get; set; }
        [SooColumn(Name = "jl_ref_ids", Length = 500)]
        public string RefIDs { get; set; }
        [SooColumn(Name = "jl_status")]
        public short? Status { get; set; }
        [SooColumn(Name = "jl_message", Length = 500)]
        public string Message { get; set; }
        [SooColumn(Name = "jl_stk_trc", DataType = DataFam.Json, Tag = "sail.xml")]
        public string StackTrace { get; set; }
        [SooColumn(Name = "jl_in_stk_trc", DataType = DataFam.Json, Tag = "sail.xml")]
        public string InnerStackTrace { get; set; }
        [SooColumn(Name = "jl_req_data", DataType = DataFam.Json, Tag = "sail.xml")]
        public string RequestData { get; set; }
        [SooColumn(Name = "jl_crt_time")]
        public DateTime? CreatedDateTime { get; set; }
        [SooColumn(Name = "jl_crt_user_id", Length = 64)]
        public string CreatedUserId { get; set; }
        [SooColumn(Name = "jl_crt_user_name", Length = 128)]
        public string CreatedUserName { get; set; }
    }
}