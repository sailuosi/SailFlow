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
using sailflow.engine.Hooks;
using sailflow.engine.Xpdl;
using sailflow.engine.Result;
using sailflow.engine.Client;
using sailflow.engine.Logging;
using sailflow.run;
using sailflow.engine.Factories;
using sailflow.WebUtility;

namespace sailflow.data;

/// <summary>契约 · TransitionInstanceManager（实现位于 sailflow.runtime）。</summary>
public interface ITransitionInstanceManager
{
    TrackEntity CreateTransitionInstanceObject( ProcessInstanceEntity processInstance, string transitionId, ActivityInstanceEntity fromActivityInstance, ActivityInstanceEntity toActivityInstance, TransitionTypeEnum transitionType, TransitionFlyingTypeEnum flyingType, FlowCommandContext runner, byte conditionParseResult);

    string Insert(SQLBuilder kit, TrackEntity entity);

    TrackEntity GetEndTransition(string bizChannel, string bizPK, string processId);

    TrackEntity GetLastTaskTransition(string bizChannel, string bizPK, string processId);

    IEnumerable<TrackEntity> GetWorkItemTransitonInstance(string bizPK, string processId);

    IEnumerable<TrackEntity> GetWorkItemTransitonInstance( SQLBuilder kit, string bizPK, string processId);

    IEnumerable<TrackEntity> GetTransitonInstance( string bizPK, string processId, ActivityTypeEnum toActivityType);

    IEnumerable<TrackEntity> GetTransitonInstance( SQLBuilder kit, string bizPK, string processId, ActivityTypeEnum toActivityType);

    IEnumerable<TrackEntity> GetTransitionInstanceList( string bizPK, string processId, string version);

    IEnumerable<TrackEntity> GetTransitionInstanceListByProcessInstance( string bizPK, string processId, string processInstanceId);

    IEnumerable<TrackEntity> GetTransitionInstanceListByProcessInstance( SQLBuilder kit, string bizPK, string processId, string processInstanceId);

    IList<ActivityInstanceEntity> GetTargetActivityInstanceList(string fromActivityInstanceId);

    IList<ActivityInstanceEntity> GetTargetActivityInstanceList(SQLBuilder kit, string fromActivityInstanceId);
}
