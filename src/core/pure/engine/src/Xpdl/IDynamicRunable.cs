using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using sailflow.engine.Common;
using sailflow.engine.Utility;
using sailflow.engine.Xpdl;

namespace sailflow.engine.Xpdl
{
    /// <summary>
    /// Dynamic Runable
    /// </summary>
    internal interface IDynamicRunable
    {
        /// <summary>
        /// Invoke method
        /// </summary>
        object InvokeMethod(TaskImplementDetail implementation, object[] userParameters);
    }
}
