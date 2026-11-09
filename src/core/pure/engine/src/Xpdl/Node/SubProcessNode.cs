using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using saiflow.data;
using saiflow.engine.Xpdl;

namespace saiflow.engine.Xpdl
{
    /// <summary>
    /// Sub Process Node
    /// 子流程节点
    /// </summary>
    public class SubProcessNode : NodeBase
    {
        /// <summary>
        /// SubProcessId
        /// </summary>
        public string SubProcessDefId { get; set; }
        /// <summary>
        /// SubProcessId
        /// </summary>
        public string SubProcessId { get; set; }
        /// <summary>
        /// Dynamically bound variable name for subprocess Id
        /// 子流程ID的动态绑定变量名称
        /// </summary>
        public string SubVarName { get; set; }

        /// <summary>
        /// Embedded subprocess
        /// 内嵌子流程
        /// </summary>
        public Process SubProcessNested { get; set; }

        internal SubProcessNode(Activity activity) :
            base(activity)
        {

        }
    }
}
