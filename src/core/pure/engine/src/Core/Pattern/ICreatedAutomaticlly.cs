using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sailflow.data;
using mooSQL.data;
using sailflow.utils;
using sailflow.xml;
using sailflow.run;

namespace sailflow.run{
    internal interface ICreatedAutomaticlly
    {
        ActivityInstanceEntity CreatedAutomaticlly(Activity toActivity, 
            ProcessInstanceEntity processInstance, 
            FlowCommandContext context,
            SQLBuilder kit);
    }
}
