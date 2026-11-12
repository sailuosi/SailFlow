using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sailflow.engine.Common;
using sailflow.data;
using mooSQL.data;
using sailflow.engine.Entity;
using sailflow.engine.Xpdl;
using sailflow.engine.Core;

namespace sailflow.engine.Pattern
{
    /// <summary>
    /// Complete Automatically
    /// </summary>
    internal interface ICompletedAutomaticlly
    {
        NodeAutoExecutedResult CompleteAutomaticlly(ProcessInstanceEntity processInstance,
            string transitionId,
            Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            Activity currentActivity,
            ActivityInstanceEntity currentActivityInstance,
            FlowCommandContext runner,
            SQLBuilder kit);
    }

    /// <summary>
    /// Complete Automatically
    /// </summary>
    internal interface ICompleteGatewayAutomaticlly
    {
        NodeAutoExecutedResult CompleteAutomaticlly(ProcessInstanceEntity processInstance,
            string transitionId,
            Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            FlowCommandContext runner,
            SQLBuilder kit);
    }
}
