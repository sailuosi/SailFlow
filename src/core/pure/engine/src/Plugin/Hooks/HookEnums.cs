namespace sailflow.engine.Hooks
{
    /// <summary>待办通知生命周期种类。</summary>
    public enum TaskNotifyKind
    {
        Created,
        Completed,
        Cancelled,
        Entrusted,
        /// <summary>催办：不改待办状态，仅旁路通知当前办理人。</summary>
        Urged,
    }

    /// <summary>抄送旁路通知种类（独立于 <see cref="TaskNotifyKind"/>）。</summary>
    public enum CopytoNotifyKind
    {
        /// <summary>抄送记录已创建（知会到达）。</summary>
        Created,
    }

    /// <summary>注册白名单匹配策略。</summary>
    public enum HookMatch
    {
        Whitelist,
        All,
    }

    /// <summary>钩子执行模式（Gate 恒 Sync，忽略本枚举）。</summary>
    public enum HookMode
    {
        /// <summary>
        /// 与流转同线程、同事务当场执行；异常 FailFast 回滚。
        /// </summary>
        /// <remarks>
        /// <para><b>【严重 · 同事务自锁】</b>调度会设置 <see cref="FlowHookContext.Kit"/>。
        /// 实现中若再开引擎库连接读写未提交数据，将与主事务互相等待导致闷锅。
        /// 必须用 <c>ctx.Kit</c>；可延后副作用请改用 <see cref="AsyncOutbox"/>。
        /// 详见《专题-Bug治理-Sync钩子同事务自锁》。</para>
        /// <para><b>事务生命周期</b>（<c>OnTransactionCommitted</c> / <c>OnTransactionFailed</c>）：
        /// 必须用 Sync；此时为 BestEffort（异常只记日志），<c>ctx.Kit</c> 恒空，不得配 AsyncOutbox。</para>
        /// </remarks>
        Sync,

        /// <summary>写入 Outbox，由 Worker 异步调用；不阻塞、不回滚已提交流转。</summary>
        AsyncOutbox,
    }

    /// <summary>流程 API 操作类型，供 Gate 区分场景。</summary>
    public enum FlowOperation
    {
        Run,
        Start,
        Withdraw,
        SendBack,
        Resend,
        Revise,
        Reverse,
        Reject,
        Close,
        Jump,
        SignForward,
        /// <summary>转办：结束本人待办并在本节点交给他人，不推进下一节点。</summary>
        Transfer,
        /// <summary>减签：从当前多人节点移除尚未办完的办理人待办，不推进下一节点。</summary>
        RemoveAssignee,
        /// <summary>改派：用新办理人集合替换当前节点未办待办（差量关旧/建新），不推进下一节点。</summary>
        ChangeAssignee,
        /// <summary>加人：向当前扁节点追加普通办理人待办（不改 Leave / 不压加签栈），不推进下一节点。</summary>
        AddAssignee,
        /// <summary>催办：对当前节点未办待办发催办通知，不改状态、不推进。</summary>
        Urge,
        /// <summary>委派：请人代办；受托人办结后自动回转原责任人（≠ Transfer）。</summary>
        Delegate,
        /// <summary>沟通：为选中人建参与者待办（阅办），锁定发起人审批直至全员阅办或收回。</summary>
        Consult,
        /// <summary>阅办：参与者关闭自己的沟通待办。</summary>
        Acknowledge,
        /// <summary>收回沟通：取消源 Task 下未关闭的参与者待办。</summary>
        RecallConsult,
        /// <summary>挂起流程实例。</summary>
        Suspend,
        /// <summary>恢复已挂起的流程实例。</summary>
        Resume,
        /// <summary>取消流程实例（业务取消）。</summary>
        Cancel,
        /// <summary>作废流程实例。</summary>
        Discard,
        /// <summary>终止流程实例（管理向终态）。</summary>
        Terminate,
    }
}
