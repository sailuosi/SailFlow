using saiflow.engine.Xpdl.Entity;
using saiflow.engine.Business.Entity;


namespace saiflow.engine.Core.Pattern
{
    /// <summary>
    /// Link Context
    /// 节点连接线上下文
    /// </summary>
    public class LinkContext
    {
        public Activity FromActivity { get; set; }
        public ActivityInstanceEntity FromActivityInstance { get; set; }
        public Activity CurrentActivity { get; set; }
        public ActivityInstanceEntity CurrentActivityInstance { get; set; } 
    }
}
