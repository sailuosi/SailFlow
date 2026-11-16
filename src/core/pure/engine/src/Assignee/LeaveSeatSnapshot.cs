namespace sailflow.engine.Assignee
{
    /// <summary>
    /// 时刻 B 只读席位快照（窄投影）。Leave / EffectiveWeight 只读本类型，不塞整颗 TaskEntity。
    /// </summary>
    public sealed class LeaveSeatSnapshot
    {
        public string SignId { get; init; }
        public string SignName { get; init; }

        /// <summary>票权；缺省按 1。</summary>
        public decimal Weight { get; init; } = 1m;

        /// <summary>所代理根 SignId；空=自己是根。</summary>
        public string ProxySignId { get; init; }

        public bool Agreed { get; init; }
        public bool Refused { get; init; }

        /// <summary>已关闭/取消等，不计打开席。</summary>
        public bool ClosedOrCancelled { get; init; }

        public string TaskId { get; init; }
        public string ActivityInstanceId { get; init; }
        public string GroupId { get; init; }
    }
}
