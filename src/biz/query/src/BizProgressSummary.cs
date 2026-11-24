namespace sailflow.biz.query
{
    /// <summary>按 BizPK 批量进度摘要（列表「我的申请」）。</summary>
    public sealed class BizProgressSummary
    {
        public string BizChannel { get; set; } = "";
        public string BizPK { get; set; } = "";
        public string? ProcessInstanceId { get; set; }
        public string? ProcessId { get; set; }
        public string? Version { get; set; }
        public string? Title { get; set; }

        /// <summary><see cref="sailflow.utils.ProcessStateEnum"/> 数值。</summary>
        public short? ProcessState { get; set; }

        public string? CreatedUserId { get; set; }
        public DateTime? CreatedDateTime { get; set; }
        public DateTime? EndedDateTime { get; set; }
    }
}
