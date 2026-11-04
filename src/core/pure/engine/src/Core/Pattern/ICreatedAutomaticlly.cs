using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Entity;
using saiflow.engine.Common;
using saiflow.engine.Xpdl;
using saiflow.engine.Core;

namespace saiflow.engine.Pattern
{
    internal interface ICreatedAutomaticlly
    {
        ActivityInstanceEntity CreatedAutomaticlly(Activity toActivity, 
            ProcessInstanceEntity processInstance, 
            WfAppRunner runner,
            SQLBuilder kit);
    }
}
