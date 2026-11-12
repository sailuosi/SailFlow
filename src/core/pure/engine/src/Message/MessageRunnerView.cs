using System;
using System.Collections.Generic;
using System.Text;
using sailflow.engine.Common;
using sailflow.engine.Entity;
using sailflow.engine.Xpdl;

namespace sailflow.engine.Message
{
    /// <summary>
    /// Message Runner View
    /// 消息运行者对象视图
    /// </summary>
    public class MessageRunnerView
    {
        public ProcessEntity ProcessEntity { get; set; }
        public Activity ActivityEntity { get; set; }
        public FlowCommandContext CommandContext { get; set; }
    }
}
