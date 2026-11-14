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
        [SooColumn(Name = "lg_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string LogId { get; set; }
        [SooColumn(Name = "lg_evt_type_id")]
        public int? EventTypeId { get; set; }
        [SooColumn(Name = "lg_priority")]
        public int? Priority { get; set; }
        [SooColumn(Name = "lg_severity", Length = 64)]
        public string Severity { get; set; }
        [SooColumn(Name = "lg_title", Length = 255)]
        public string Title { get; set; }
        [SooColumn(Name = "lg_message", Length = 500)]
        public string Message { get; set; }
        [SooColumn(Name = "lg_stk_trc", DataType = DataFam.Json, Tag = "sail.xml")]
        public string StackTrace { get; set; }
        [SooColumn(Name = "lg_in_stk_trc", DataType = DataFam.Json, Tag = "sail.xml")]
        public string InnerStackTrace { get; set; }
        [SooColumn(Name = "lg_req_data", DataType = DataFam.Json, Tag = "sail.xml")]
        public string RequestData { get; set; }
        [SooColumn(Name = "lg_timestamp")]
        public DateTime? Timestamp { get; set; }
    }
}