namespace saiflow.engine.Hooks
{
    /// <summary>待办通知生命周期种类。</summary>
    public enum TaskNotifyKind
    {
        Created,
        Completed,
        Cancelled,
        Entrusted,
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
    public enum WorkflowOperation
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
    }
}
