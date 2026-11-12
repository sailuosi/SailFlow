using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using mooSQL.data;

namespace sailflow.engine.Entity
{
    /// <summary>
    /// Log Entity
    /// 日志记录实体
    /// </summary>
    [SooTable(Name = "wf_log", Caption = "日志", Type = DBTableType.Table)]
    public class LogEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64)]
        public string LogId { get; set; }
        [SooColumn(Name = "event_type_id")]
        public int? EventTypeId { get; set; }
        [SooColumn(Name = "priority")]
        public int? Priority { get; set; }
        [SooColumn(Name = "severity")]
        public string Severity { get; set; }
        [SooColumn(Name = "title")]
        public string Title { get; set; }
        [SooColumn(Name = "message")]
        public string Message { get; set; }
        [SooColumn(Name = "stack_trace")]
        public string StackTrace { get; set; }
        [SooColumn(Name = "inner_stack_trace")]
        public string InnerStackTrace { get; set; }
        [SooColumn(Name = "request_data")]
        public string RequestData { get; set; }
        [SooColumn(Name = "time_stamp")]
        public DateTime? Timestamp { get; set; }
    }
}
