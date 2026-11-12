using sailflow.engine.Entity;

namespace sailflow.biz.query
{
    /// <summary>
    /// Process Instance Query
    /// 流程实例查询实体
    /// </summary>
    public class ProcessInstanceQuery : QueryBase
    {
        // 过滤字段均为可选；非 null string 在 [ApiController]+NRT 下会被当成 Required → 400
        public string? ProcessId { get; set; }
        public string? ApplicationInstanceId { get; set; }
        public string? AppName { get; set; }
        /// <summary>发起人用户 Id（我的流程）。</summary>
        public string? CreatedUserId { get; set; }
        /// <summary>流程实例状态过滤（可选）。</summary>
        public short? ProcessState { get; set; }
    }
}
