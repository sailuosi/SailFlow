using System;
using System.Collections.Generic;
using System.Text;
using saiflow.engine.Common;
using saiflow.engine.Entity;
using saiflow.engine.Xpdl;

namespace saiflow.engine.Message
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
