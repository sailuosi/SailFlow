using saiflow.engine.Common;

namespace saiflow.engine.Entity
{
    /// <summary>
    /// Process Query
    /// 流程查询类
    /// </summary>
    public class ProcessQuery : QueryBase
    {
        public string ProcessId { get; set; }
        public string Version { get; set; }
        public string ProcessName { get; set; }
        public ProcessStartTypeEnum StartType { get; set; }

    }
}
