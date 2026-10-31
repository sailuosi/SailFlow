using System.Collections.Generic;
using saiflow.engine.Common;

namespace saiflow.engine.Assignee
{
    /// <summary>时刻 A：查找待办人调用入参（engine 不解析 Type）。</summary>
    public class AssigneeInvokeRequest
    {
        public string ProcessId { get; set; }
        /// <summary>流程版本（RoleGroup / 读定义时需要）。</summary>
        public string Version { get; set; }
        public int? ProcessInstanceId { get; set; }
        public string ActivityId { get; set; }
        public string ActivityCode { get; set; }
        public WfAppRunner Runner { get; set; }
        public IDictionary<string, PerformerList> NextActivityPerformers { get; set; }
    }
}
