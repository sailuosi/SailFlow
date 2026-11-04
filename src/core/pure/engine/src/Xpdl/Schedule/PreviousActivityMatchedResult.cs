using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using saiflow.engine.Common;

namespace saiflow.engine.Xpdl
{
    /// <summary>
    /// Previous Activity Mathced Result
    /// 上一步节点匹配结果
    /// </summary>
    public class PreviousActivityMatchedResult
    {
        public Int32 PreviousCount { get; set; }
        public Boolean HasGatewayPassed { get; set; }
        public IList<NodeView> PreviousActivityList { get; set; }
    }
}
