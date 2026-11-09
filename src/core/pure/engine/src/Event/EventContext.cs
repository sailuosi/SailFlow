using System;
using System.Collections.Generic;
using System.Text;
using saiflow.engine.Common;

namespace saiflow.engine.Event
{
    /// <summary>
    /// Event execution context
    /// 事件执行上下文
    /// </summary>
    public class EventContext
    {
        public string AppInstanceId { get; set; }
        public string ProcessId { get; set; }
        public string ProcessInstanceId { get; set; }
        public string ActivityInstanceId { get; set; }
        public string ActivityId { get; set; }
        public string ActivityName { get; set; }
        public string ActivityCode { get; set; }
        public ActivityResource ActivityResource { get; set; }
    }
}
