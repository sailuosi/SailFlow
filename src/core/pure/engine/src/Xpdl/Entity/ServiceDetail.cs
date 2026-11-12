using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sailflow.engine.Common;
using sailflow.engine.Xpdl;


namespace sailflow.engine.Xpdl
{
    /// <summary>
    /// Service Detail
    /// </summary>
    public class ServiceDetail
    {
        public ServiceMethodEnum Method { get; set; }
        public SubMethodEnum SubMethod { get; set; }
        public string Arguments { get; set; }
        public string Expression { get; set; }
        /// <summary>
        /// Reflection method configuration information
        /// 反射方法配置信息
        /// </summary>
        public MethodInfo MethodInfo { get; set; }
    }
}
