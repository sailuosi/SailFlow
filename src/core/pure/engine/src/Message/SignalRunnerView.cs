using System;
using System.Collections.Generic;
using System.Text;
using sailflow.utils;
using sailflow.data;
using sailflow.xml;

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
