using System;
using System.Collections.Generic;
using System.Text;
using saiflow.engine.Common;
using saiflow.engine.Entity;
using saiflow.engine.Xpdl;

namespace saiflow.engine.Message
{
    /// <summary>
    /// Message Runner View
    /// 消息运行者对象视图
    /// </summary>
    public class MessageRunnerView
    {
        public ProcessEntity ProcessEntity { get; set; }
        public Activity ActivityEntity { get; set; }
        public WfAppRunner WfAppRunner { get; set; }
    }
}
