using System.Collections.Generic;
using sailflow.data;
using sailflow.utils;
using mooSQL.data;

namespace sailflow.assignee
{
    /// <summary>时刻 B：流出判定调用入参（engine 不解析 Type）。</summary>
    public class LeaveInvokeRequest
    {
        public string ProcessInstanceId { get; set; }
        public string ActivityInstanceId { get; set; }
        /// <summary>流程定义 Id（读 BPMN ConfigPair 用）。</summary>
        public string ProcessId { get; set; }
        /// <summary>流程版本（读 BPMN ConfigPair 用）。</summary>
        public string Version { get; set; }
        public ActivityInstanceEntity CurrentActivityInstance { get; set; }
        public ActivityInstanceEntity MainActivityInstance { get; set; }
        /// <summary>会签子活动快照（由 AIM 预加载）；过渡兼容；计权优先 <see cref="Seats"/>。</summary>
        public IList<ActivityInstanceEntity> ChildActivityInstances { get; set; }
        /// <summary>计权主输入：席位窄投影（Task / 子 AI）；LeaveJudge 只读。</summary>
        public IList<LeaveSeatSnapshot> Seats { get; set; }
        public FlowCommandContext Runner { get; set; }
        /// <summary>流转事务 kit；读 BPMN / 未提交实例时须复用，禁止另开连接。</summary>
        public SQLBuilder Kit { get; set; }
    }
}
