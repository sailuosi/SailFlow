namespace sailflow.engine.Common
{
    /// <summary>
    /// 任务结束类型码（string；写入 <c>TaskEntity.EndType</c>）。
    /// 与 <see cref="TaskStateEnum"/> 正交：State 管态机，本码解释原因。
    /// </summary>
    public static class TaskEndTypes
    {
        public const string SelfComplete = "SelfComplete";
        public const string RaceTaken = "RaceTaken";
        public const string ProxyTaken = "ProxyTaken";
        public const string DelegatedOut = "DelegatedOut";
        public const string TransferredOut = "TransferredOut";
        public const string Removed = "Removed";
        public const string ConsultRecalled = "ConsultRecalled";
        public const string ActivityCancelled = "ActivityCancelled";
        public const string Withdrawn = "Withdrawn";
        public const string SendBacked = "SendBacked";
    }
}
