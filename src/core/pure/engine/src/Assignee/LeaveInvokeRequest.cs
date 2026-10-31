using saiflow.engine.Business.Entity;
using saiflow.engine.Common;

namespace saiflow.engine.Assignee
{
    /// <summary>时刻 B：流出判定调用入参（engine 不解析 Type）。</summary>
    public class LeaveInvokeRequest
    {
        public int ProcessInstanceId { get; set; }
        public int ActivityInstanceId { get; set; }
        public ActivityInstanceEntity CurrentActivityInstance { get; set; }
        public ActivityInstanceEntity MainActivityInstance { get; set; }
        public WfAppRunner Runner { get; set; }
    }
}
