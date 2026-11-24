using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using sailflow.data;
using sailflow.hub.Localize;
using sailflow.hub.Resource;
using sailflow.engine.Assignee;
using sailflow.engine.Common;
using sailflow.hooks;
using sailflow.engine.Xpdl;
using sailflow.engine.Result;
using sailflow.engine.Client;
using sailflow.engine.Logging;
using sailflow.run;
using sailflow.engine.Factories;
using sailflow.WebUtility;

namespace sailflow.run;
/// <summary>契约 · PreviousStepChecker（实现位于 sailflow.runtime）。</summary>
public interface IPreviousStepChecker
{
    IList<NodeView> GetPreviousActivityTree(string taskId, out Boolean hasGatewayPassed);

    IList<NodeView> GetPreviousActivityTree(FlowCommandContext runner, out Boolean hasGatewayPassed);

    IList<Activity> GetPreviousActivityList(ActivityInstanceEntity runningNode, IProcessModel processModel, out Boolean hasGatewayPassed);

    ActivityInstanceEntity GetPreviousOfMultipleInstanceNode(string mainActivityInstanceId, string activityInstanceId, float completeOrder);

    IList<ActivityInstanceEntity> GetPreviousActivityInstanceList(ActivityInstanceEntity runningNode, bool isLookUpBackSource, out bool hasPassedGatewayNode);
}
