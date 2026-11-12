using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sailflow.engine.Common;

namespace sailflow.engine.Entity
{
    /// <summary>
    /// Process Variable Query
    /// 流程变量查询实体
    /// </summary>
    public class ProcessVariableQuery
    {
        public ProcessVariableScopeEnum VariableSceope { get; set; }
        public string ProcessInstanceId { get; set; }
        public string ActivityInstanceId { get; set; }
        public string Name { get; set; }
    }
}
