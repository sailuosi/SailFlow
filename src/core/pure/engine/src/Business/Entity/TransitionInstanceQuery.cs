using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace saiflow.engine.Business.Entity
{
    /// <summary>
    /// Transition Instance Query
    /// 转移查询实体
    /// </summary>
    public class TransitionInstanceQuery
    {
        public string AppInstanceId { get; set; }
        public string ProcessId { get; set; }
        public string Version { get; set; }
    }
}
