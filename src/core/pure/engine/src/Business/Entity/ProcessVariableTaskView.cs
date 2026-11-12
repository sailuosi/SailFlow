using System;
using System.Collections.Generic;
using System.Text;
using sailflow.engine.Common;

namespace sailflow.engine.Entity
{
    /// <summary>
    /// Process Variable Task View
    /// 流程变量的任务视图
    /// </summary>
    public class ProcessVariableTaskView
    {
        public string TaskId { get; set; }
        public string VariableScope { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string MediaType { get; set;  }
    }
}
