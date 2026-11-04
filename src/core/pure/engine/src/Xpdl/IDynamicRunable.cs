using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using saiflow.engine.Common;
using saiflow.engine.Utility;
using saiflow.engine.Xpdl;

namespace saiflow.engine.Xpdl
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
