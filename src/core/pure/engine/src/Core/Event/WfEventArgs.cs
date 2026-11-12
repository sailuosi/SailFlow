using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sailflow.engine.Common;
using sailflow.engine.Result;
using sailflow.engine.Core;

namespace sailflow.engine.Runtime
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
