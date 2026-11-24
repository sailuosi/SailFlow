using sailflow.utils;

namespace sailflow.data
{
    /// <summary>
    /// 流程定义查询条件（非落库），继承分页基类。
    /// </summary>
    public class ProcessQuery : QueryBase
    {
        /// <summary>按逻辑流程 Id 过滤。</summary>
        public string ProcessId { get; set; }

        /// <summary>按版本号过滤。</summary>
        public string Version { get; set; }

        /// <summary>按流程名称过滤（模糊/精确由查询实现决定）。</summary>
        public string ProcessName { get; set; }

        /// <summary>
        /// 按启动类型过滤，见 <see cref="ProcessStartTypeEnum"/>。
        /// </summary>
        public ProcessStartTypeEnum StartType { get; set; }

    }
}
