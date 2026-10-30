using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using saiflow.engine.Common;
using saiflow.engine.Core.Result;

namespace saiflow.engine.Core.Event
{
    /// <summary>
    /// Workflow Event
    /// 工作流Event
    /// </summary>
    public class WfEventArgs : EventArgs
    {
        /// <summary>
        /// Workflow Executed REsult
        /// 工作项执行结果
        /// </summary>
        public WfExecutedResult WfExecutedResult { get; set; }

        /// <summary>
        /// Constructor function
        /// </summary>
        /// <param name="result"></param>
        public WfEventArgs(WfExecutedResult result)
            : base()
        {
            WfExecutedResult = result;
        }
    }
}
