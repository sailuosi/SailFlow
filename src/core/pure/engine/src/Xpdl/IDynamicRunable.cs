using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using sailflow.utils;
using sailflow.xml;

namespace sailflow.xml
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
