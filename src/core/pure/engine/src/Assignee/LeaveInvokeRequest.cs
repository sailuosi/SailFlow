using System.Collections.Generic;
using saiflow.engine.Entity;
using saiflow.engine.Common;

namespace saiflow.engine.Assignee
{
    /// <summary>时刻 B：流出判定调用入参（engine 不解析 Type）。</summary>
    public class LeaveInvokeRequest
    {
        public int ProcessInstanceId { get; set; }
        public int ActivityInstanceId { get; set; }
        /// <summary>流程定义 Id（读 BPMN ConfigPair 用）。</summary>
        public string ProcessId { get; set; }
        /// <summary>流程版本（读 BPMN ConfigPair 用）。</summary>
        public string Version { get; set; }
        public ActivityInstanceEntity CurrentActivityInstance { get; set; }
        public ActivityInstanceEntity MainActivityInstance { get; set; }
        /// <summary>会签子活动快照（由 AIM 预加载）；LeaveJudge 只读。</summary>
        public IList<ActivityInstanceEntity> ChildActivityInstances { get; set; }
        public FlowCommandContext Runner { get; set; }
    }
}
