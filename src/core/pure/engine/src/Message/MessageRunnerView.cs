using System;
using System.Collections.Generic;
using System.Text;
using sailflow.utils;
using sailflow.data;
using sailflow.xml;

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
