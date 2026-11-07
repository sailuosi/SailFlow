using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Entity;
using saiflow.engine.Xpdl;
using saiflow.engine.Core;

namespace saiflow.engine.Pattern
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
