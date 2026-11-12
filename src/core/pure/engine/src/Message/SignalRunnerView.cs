using System;
using System.Collections.Generic;
using System.Text;
using sailflow.engine.Common;
using sailflow.engine.Entity;
using sailflow.engine.Xpdl;

namespace sailflow.engine.Message
{
    /// <summary>
    /// Signal Runner View
    /// 信号运行者对象视图
    /// </summary>
    public class SignalRunnerView
    {
        public ProcessEntity ProcessEntity { get; set; }
        public Activity ActivityEntity { get; set; }
        public FlowCommandContext CommandContext { get; set; }
    }
}
