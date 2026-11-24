using System;
using System.Text.Json.Serialization;
using mooSQL.data;
using sailflow.events;
using sailflow.engine.Result;

namespace sailflow.hooks
{
    /// <summary>Gate / Hook 统一上下文（Gate 默认只读）。</summary>
    public sealed class FlowHookContext
    {
        public string BizChannel { get; set; }
        public string BizPK { get; set; }
        public string ProcessId { get; set; }
        public string ProcessCode { get; set; }
        public string Version { get; set; }
        public string ProcessInstanceId { get; set; }
        public string ActivityId { get; set; }
        public string ActivityCode { get; set; }
        public string ActivityInstanceId { get; set; }
        /// <summary>当前节点定义 BizState。</summary>
        public int ActivityBizState { get; set; }
        /// <summary>当前节点定义 BizCode。</summary>
        public string ActivityBizCode { get; set; }
        public string TaskId { get; set; }
        public string SignId { get; set; }
        public string SignName { get; set; }
        public FlowOperation Operation { get; set; }
        public EventFireTypeEnum EventType { get; set; }

        /// <summary>
        /// 当前流转事务上的 <see cref="SQLBuilder"/>。
        /// </summary>
        /// <remarks>
        /// <para><b>【严重 · 同事务自锁】</b>Sync Hook 与主路径共用此连接/事务。
        /// 读写未提交的流程实例、变量、活动、待办时<strong>必须</strong>用本属性（或 <c>Kit.useSQL()</c>），
        /// <strong>禁止</strong>再 <c>Sail.UseRunSQL()</c> 或无 kit 的 <c>WorkflowService</c>/<c>*Manager</c> 访问同一引擎库，
        /// 否则第二连接等待提交、主事务等待钩子 → 自锁闷锅（SQLite Error 5/6、命令超时；调试强制停止后查询全挂）。
        /// 案例与检查单见 theme《专题-Bug治理-Sync钩子同事务自锁》；可观测见《专题-钩子治理与同事务可观测》。</para>
        /// <para>AsyncOutbox 回放、<see cref="EventFireTypeEnum.OnTransactionCommitted"/> /
        /// <see cref="EventFireTypeEnum.OnTransactionFailed"/> 时为空；禁止序列化进 Outbox（SQLBuilder 自引用环）。
        /// Enqueue 前会临时摘掉；STJ 亦忽略本属性。</para>
        /// </remarks>
        [JsonIgnore]
        public SQLBuilder Kit { get; set; }

        /// <summary>
        /// 事务生命周期失败钩子：导致未提交的异常（有则填）。
        /// 仅 <see cref="EventFireTypeEnum.OnTransactionFailed"/>；不进 Outbox。
        /// </summary>
        [JsonIgnore]
        public Exception FailureException { get; set; }

        /// <summary>
        /// 事务生命周期：办理结果状态（Committed 为 Success；Failed 为非 Success 或异常前已有结果）。
        /// </summary>
        [JsonIgnore]
        public WfExecutedStatus? ExecutedStatus { get; set; }

        /// <summary>
        /// 事务失败说明：来自 <c>result.Message</c> 或异常 Message。
        /// </summary>
        [JsonIgnore]
        public string FailureMessage { get; set; }
    }

    /// <summary>待办 Notifier 上下文。</summary>
    public sealed class TaskNotifyContext
    {
        public string BizChannel { get; set; }
        public string BizPK { get; set; }
        public string ProcessId { get; set; }
        public string ProcessCode { get; set; }
        public string Version { get; set; }
        public string ProcessInstanceId { get; set; }
        public string ActivityId { get; set; }
        public string ActivityCode { get; set; }
        public string ActivityInstanceId { get; set; }
        /// <summary>当前节点定义 BizState。</summary>
        public int ActivityBizState { get; set; }
        /// <summary>当前节点定义 BizCode。</summary>
        public string ActivityBizCode { get; set; }
        public string TaskId { get; set; }
        public string SignId { get; set; }
        public string SignName { get; set; }
        public TaskNotifyKind Kind { get; set; }
    }

    /// <summary>抄送 Notifier 上下文（产品抄送箱；禁止与待办 TaskNotify 混用）。</summary>
    public sealed class CopytoNotifyContext
    {
        public string ProcessCopytoId { get; set; }
        public string BizChannel { get; set; }
        public string BizPK { get; set; }
        public string ProcessId { get; set; }
        public string ProcessCode { get; set; }
        public string ProcessName { get; set; }
        public string Version { get; set; }
        public string ProcessInstanceId { get; set; }
        public string TaskId { get; set; }
        public string TaskName { get; set; }
        /// <summary>被抄送人。</summary>
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string StartUserId { get; set; }
        public string CreatorId { get; set; }
        public string CreatorName { get; set; }
        public string Reason { get; set; }
        public CopytoNotifyKind Kind { get; set; }
    }
}
