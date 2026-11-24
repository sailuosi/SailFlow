using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sailflow.data;
using mooSQL.data;
using sailflow.engine.Common;
using sailflow.engine.Xpdl;
using sailflow.run;

namespace sailflow.run{
    internal interface ICreatedAutomaticlly
    {
        ActivityInstanceEntity CreatedAutomaticlly(Activity toActivity, 
            ProcessInstanceEntity processInstance, 
            FlowCommandContext runner,
            SQLBuilder kit);
    }
}
