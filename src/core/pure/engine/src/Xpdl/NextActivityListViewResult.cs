using System;
using System.Collections.Generic;
using System.Text;
using saiflow.engine.Common;

namespace saiflow.engine.Xpdl
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
