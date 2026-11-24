using System;
using sailflow.utils;
using sailflow.data;
using sailflow.xml;


namespace sailflow.run{
    /// <summary>
    /// Context object during process backward processing
    /// 流程回退处理时的上下文对象
    /// </summary>
    internal class BackwardContext
    {
        internal Activity BackwardToTaskActivity { get; set; }
        internal ActivityInstanceEntity BackwardToTaskActivityInstance { get; set; }
        internal Activity BackwardFromActivity { get; set; }
        internal ActivityInstanceEntity BackwardFromActivityInstance { get; set; }
        internal ProcessInstanceEntity ProcessInstance { get; set; }
        internal String BackwardToTargetTransitionId { get; set; }
        internal WfBackwardTaskReceiver BackwardTaskReceiver { get; set; }
    }
}
