using System.Collections.Generic;
using mooSQL.data;
using saiflow.engine.Common;

namespace saiflow.engine.Assignee
{
    /// <summary>时刻 A：查找待办人调用入参（engine 不解析 Type）。</summary>
    public class AssigneeInvokeRequest
    {
        public string ProcessId { get; set; }
        /// <summary>流程版本（RoleGroup / 读定义时需要）。</summary>
        public string Version { get; set; }
        public string ProcessInstanceId { get; set; }
        public string ActivityId { get; set; }
        public string ActivityCode { get; set; }
        public FlowCommandContext Runner { get; set; }
        public IDictionary<string, PerformerList> NextActivityPerformers { get; set; }

        /// <summary>
        /// 可选：当前发送主路径事务 kit。锚点入 Outbox 时与发送同事务（R10）。
        /// </summary>
        public SQLBuilder Kit { get; set; }

        /// <summary>
        /// Pipeline 出参：已入队自动审批锚点且无真人办理人（R10 零 Performer）。
        /// Mediator 可跳过建待办，由 Job 经 <c>RunProcessAuto</c> 代办。
        /// </summary>
        public bool ResultAutoApprovePending { get; set; }
    }
}
