using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using saiflow.data;
using saiflow.engine.Common;
using saiflow.engine.Entity;
using saiflow.engine.Xpdl;

namespace saiflow.engine.Xpdl
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
