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
        Sync,
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
        /// <summary>催办：对当前节点未办待办发催办通知，不改状态、不推进。</summary>
        Urge,
        /// <summary>委派：请人代办；受托人办结后自动回转原责任人（≠ Transfer）。</summary>
        Delegate,
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
