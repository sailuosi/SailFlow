using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using mooSQL.data;

namespace sailflow.data
{
    /// <summary>
    /// 引擎运行日志实体（表 <c>sf_log</c>）。
    /// 记录告警/异常/错误事件，含堆栈与请求数据，供排查。
    /// </summary>
    [SooTable(Name = "sf_log", Caption = "日志", Type = DBTableType.Table)]
    public class LogEntity
    {
        /// <summary>
        /// 日志主键（列 <c>lg_id</c>）。
        /// </summary>
        [SooColumn(Name = "lg_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string LogId { get; set; }

        /// <summary>
        /// 事件类型 Id（列 <c>lg_evt_type_id</c>），见 <see cref="sailflow.utils.LogEventType"/>。
        /// 0=Warnning；1=Exception；2=Error。
        /// </summary>
        [SooColumn(Name = "lg_evt_type_id")]
        public int? EventTypeId { get; set; }

        /// <summary>
        /// 优先级（列 <c>lg_priority</c>），见 <see cref="sailflow.utils.LogPriority"/>。
        /// 0=Emergency；1=High；2=Normal；3=Low。
        /// </summary>
        [SooColumn(Name = "lg_priority")]
        public int? Priority { get; set; }

        /// <summary>
        /// 严重级别文案（列 <c>lg_severity</c>），展示用字符串。
        /// </summary>
        [SooColumn(Name = "lg_severity", Length = 64)]
        public string Severity { get; set; }

        /// <summary>
        /// 日志标题（列 <c>lg_title</c>）。
        /// </summary>
        [SooColumn(Name = "lg_title", Length = 255)]
        public string Title { get; set; }

        /// <summary>
        /// 日志正文/摘要（列 <c>lg_message</c>）。
        /// </summary>
        [SooColumn(Name = "lg_message", Length = 500)]
        public string Message { get; set; }

        /// <summary>
        /// 异常堆栈（列 <c>lg_stk_trc</c>）。
        /// </summary>
        [SooColumn(Name = "lg_stk_trc", DataType = DataFam.Json, Tag = "sail.xml")]
        public string StackTrace { get; set; }

        /// <summary>
        /// 内层异常堆栈（列 <c>lg_in_stk_trc</c>）。
        /// </summary>
        [SooColumn(Name = "lg_in_stk_trc", DataType = DataFam.Json, Tag = "sail.xml")]
        public string InnerStackTrace { get; set; }

        /// <summary>
        /// 相关请求/上下文数据（列 <c>lg_req_data</c>，JSON）。
        /// </summary>
        [SooColumn(Name = "lg_req_data", DataType = DataFam.Json, Tag = "sail.xml")]
        public string RequestData { get; set; }

        /// <summary>
        /// 日志发生时间戳（列 <c>lg_timestamp</c>）。
        /// </summary>
        [SooColumn(Name = "lg_timestamp")]
        public DateTime? Timestamp { get; set; }
    }
}
