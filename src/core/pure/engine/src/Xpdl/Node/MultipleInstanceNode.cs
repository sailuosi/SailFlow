using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sailflow.data;
using sailflow.engine.Common;
using sailflow.xml;

namespace sailflow.xml
{
    /// <summary>
    /// Multiple Instance Node
    /// 多实例节点类型
    /// </summary>
    internal class MultipleInstanceNode : NodeBase
    {
        internal MultipleInstanceNode(Activity activity) :
            base(activity)
        {

        }
    }
}
