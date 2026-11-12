using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sailflow.engine.Core
{
    /// <summary>
    /// The parameter class returned by rollback
    /// 回退返回的参数类
    /// </summary>
    public class ReturnDataContext
    {
        public string ActivityInstanceId { get; set; }
        public string ProcessInstanceId { get; set; }
    }
}
