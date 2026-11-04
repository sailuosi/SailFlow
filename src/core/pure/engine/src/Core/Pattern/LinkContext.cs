using saiflow.engine.Xpdl;
using saiflow.engine.Entity;
using saiflow.engine.Core;


namespace saiflow.engine.Pattern
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
