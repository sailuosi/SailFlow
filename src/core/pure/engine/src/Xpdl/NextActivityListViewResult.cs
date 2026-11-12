using System;
using System.Collections.Generic;
using System.Text;
using sailflow.engine.Common;

namespace sailflow.engine.Xpdl
{
    /// <summary>
    /// Next Activity Tree Result
    /// </summary>
    public class NextActivityListViewResult
    {
        public string Message { get; set; }
        public IList<NodeView> StepList { get; set; }
    }
}
