namespace saiflow.biz.query
{
    /// <summary>
    /// Transition Instance Query
    /// 转移查询实体
    /// </summary>
    public class TransitionInstanceQuery
    {
        public string AppInstanceId { get; set; }
        public string ProcessId { get; set; }
        public string Version { get; set; }
    }
}
