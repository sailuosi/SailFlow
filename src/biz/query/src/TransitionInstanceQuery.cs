namespace sailflow.biz.query
{
    /// <summary>
    /// Transition Instance Query
    /// 转移查询实体
    /// </summary>
    public class TransitionInstanceQuery
    {
        // 过滤字段均为可选；非 null string 在 [ApiController]+NRT 下会被当成 Required → 400
        public string? AppInstanceId { get; set; }
        public string? ProcessId { get; set; }
        public string? Version { get; set; }
    }
}
