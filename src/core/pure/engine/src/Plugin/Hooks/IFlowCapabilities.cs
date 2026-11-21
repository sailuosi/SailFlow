using System.Threading;
using System.Threading.Tasks;
using sailflow.engine.Event;

namespace sailflow.engine.Hooks
{
    /// <summary>发送前闸门：仅 Sync；用结果拒绝，不抛业务校验异常。</summary>
    public interface IFlowGate
    {
        string Name { get; }
        HookResult Validate(FlowHookContext ctx);
    }

    /// <summary>
    /// 业务钩子：Sync 当场调；Async 由 Outbox Worker 调同一方法。
    /// </summary>
    /// <remarks>
    /// <para><b>【严重 · 同事务自锁】</b>当注册为 <see cref="HookMode.Sync"/> 且调度传入
    /// <see cref="FlowHookContext.Kit"/>（发起/办理主路径几乎总是如此）时，本方法与流转
    /// <b>共用同一数据库事务</b>。此时若再调用 <c>Sail.UseRunSQL()</c>，或无 kit 重载的
    /// <c>WorkflowService</c>/<c>*Manager</c> 去读写引擎库中刚插入、尚未提交的行，
    /// 将形成「第二连接等锁 ↔ 主事务等钩子返回」的自锁，表现为请求闷锅无响应、
    /// SQLite Error 5/6、或 SQL 命令超时。强制终止调试后还可能污染连接，导致后续查询全挂。</para>
    /// <para><b>正确做法</b>：读写 <c>sf_*</c> 流程表一律使用 <see cref="FlowHookContext.Kit"/>
    ///（或 <c>kit.useSQL()</c>）及带 kit 的 API；可延后的副作用改用 <see cref="HookMode.AsyncOutbox"/>。
    /// 详见 theme《专题-Bug治理-Sync钩子同事务自锁》《专题-钩子治理与同事务可观测》。</para>
    /// </remarks>
    public interface IFlowHook
    {
        string Name { get; }

        /// <summary>
        /// 执行钩子逻辑。Sync 模式下勿在实现中另开引擎库连接（见接口 remarks）。
        /// </summary>
        void Handle(FlowHookContext ctx);
    }

    /// <summary>待办旁路：默认异步 Outbox 后再调业务实现。</summary>
    public interface ITaskNotifier
    {
        string Name { get; }
        Task NotifyAsync(TaskNotifyContext ctx, CancellationToken ct);
    }

    /// <summary>抄送旁路：独立于待办 Notifier；默认异步 Outbox。</summary>
    public interface ICopytoNotifier
    {
        string Name { get; }
        Task NotifyAsync(CopytoNotifyContext ctx, CancellationToken ct);
    }
}
