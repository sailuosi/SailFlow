namespace sailflow.engine.Common
{
    /// <summary>B · 启动/创建时注入的业务数据袋；不注入保持空。</summary>
    public sealed class BizDataDto
    {
        /// <summary>业务主键（原 AppInstanceId）。</summary>
        public string BizPK { get; set; }

        /// <summary>业务类型。</summary>
        public string BizType { get; set; }

        /// <summary>业务编码（原 AppInstanceCode）。</summary>
        public string BizCode { get; set; }

        /// <summary>业务通道（原 AppName / FormCode 路由键）。</summary>
        public string BizChannel { get; set; }

        /// <summary>业务名称。</summary>
        public string BizName { get; set; }
    }
}
