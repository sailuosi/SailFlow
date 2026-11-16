using System.Collections.Generic;
using mooSQL.data;
using sailflow.engine.Common;

namespace sailflow.engine.Assignee
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
        /// Pipeline 出参：已入队自动审批锚点且无真人办理人（遗留零 Performer 路径）。
        /// 新产品：建待办 + Hook 入队，不再依赖本标志。
        /// </summary>
        public bool ResultAutoApprovePending { get; set; }

        /// <summary>
        /// 是否允许同人策略 Filter（跳过/换人）。退回等 Backward 建任务为 false。
        /// </summary>
        public bool AllowSameAsStarterPolicy { get; set; } = true;
    }
}
