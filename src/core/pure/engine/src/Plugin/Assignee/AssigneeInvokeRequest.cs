using System.Collections.Generic;
using mooSQL.data;
using sailflow.utils;

namespace sailflow.assignee
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
        /// 当前发送/发起主路径事务 kit（有则与流转同事务）。
        /// </summary>
        /// <remarks>
        /// <para><b>【严重 · 同事务自锁】</b>解析办理人、读流程实例/定义、写锚点 Outbox 时优先用本 Kit
        ///（或 <c>Kit.useSQL()</c>）。若忽略本属性而 <c>Sail.UseRunSQL()</c> 读刚插入的流程实例，
        /// 会与主事务自锁（发起超时 / SQLite locked）。见《专题-Bug治理-Sync钩子同事务自锁》。</para>
        /// <para>锚点入 Outbox 时与发送同事务（R10）。</para>
        /// </remarks>
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
