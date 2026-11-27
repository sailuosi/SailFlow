namespace sailflow.utils
{
    /// <summary>
    /// 任务结束类型码（string；写入 <c>TaskEntity.EndType</c>）。
    /// 与 <see cref="TaskStateEnum"/> 正交：State 管态机，本码解释原因。
    /// </summary>
    public static class TaskEndTypes
    {
        public const string SelfComplete = "SelfComplete";
        public const string RaceTaken = "RaceTaken";
        /// <summary>认领/承办（TakeOn）：同活动（或同组）他人待办被独占关闭。</summary>
        public const string TakeOnTaken = "TakeOnTaken";
        public const string ProxyTaken = "ProxyTaken";
        public const string DelegatedOut = "DelegatedOut";
        public const string TransferredOut = "TransferredOut";
        public const string Removed = "Removed";
        /// <summary>扁平抬席：原节点直属待办迁到发起人容器 AI。</summary>
        public const string SignedForwardOut = "SignedForwardOut";
        public const string ConsultRecalled = "ConsultRecalled";
        public const string ActivityCancelled = "ActivityCancelled";
        public const string Withdrawn = "Withdrawn";
        public const string SendBacked = "SendBacked";
    }
}
