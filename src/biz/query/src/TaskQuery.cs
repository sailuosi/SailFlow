using sailflow.engine.Entity;

namespace sailflow.biz.query
{
    /// <summary>
    /// Task Query
    /// 任务查询实体对象
    /// </summary>
    public class TaskQuery : QueryBase
    {
        // 过滤字段均为可选；非 null string 在 [ApiController]+NRT 下会被当成 Required → 400
        public string? AppName { get; set; }
        public string? AppInstanceId { get; set; }
        public string? ProcessId { get; set; }
        public string? Version { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? EndedUserId { get; set; }
        public string? EndedUserName { get; set; }
    }
}
