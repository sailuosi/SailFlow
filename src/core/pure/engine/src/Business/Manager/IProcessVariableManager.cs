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
using sailflow.events;
using sailflow.engine.Logging;
using sailflow.run;
using sailflow.engine.Factories;
using sailflow.WebUtility;

namespace sailflow.data;

/// <summary>契约 · ProcessVariableManager（实现位于 sailflow.runtime）。</summary>
public interface IProcessVariableManager
{
    ProcessVariableEntity GetVariableById(string processId, string variableId);

    ProcessVariableEntity GetVariableById(SQLBuilder kit, string processId, string variableId);

    string GetVariable( SQLBuilder kit, ProcessVariableScopeEnum variableType, string name, EventContext eventContext);

    string GetVariableByScopePriority(SQLBuilder kit, string name, EventContext eventContext);

    IDictionary<string, string> GetVariableByTask( SQLBuilder kit, TaskViewEntity taskView, IDictionary<string, string> condition);

    IList<ProcessVariableEntity> GetVariableList(string processInstanceId);

    IList<ProcessVariableEntity> GetVariableList(SQLBuilder kit, string processInstanceId);

    IList<ProcessVariableEntity> GetVariableList( SQLBuilder kit, string processInstanceId, string activityInstanceId);

    ProcessVariableEntity GetVariableEntity(ProcessVariableQuery query);

    ProcessVariableEntity GetVariableEntity(SQLBuilder kit, ProcessVariableQuery query);

    ProcessVariableEntity GetVariableByProcess(SQLBuilder kit, string processInstanceId, string name);

    ProcessVariableEntity GetVariableByActivity( SQLBuilder kit, string processInstanceId, string activityInstanceId, string name);

    IList<ProcessVariableEntity> GetVariableListByActivity( SQLBuilder kit, string processInstanceId, string activityInstanceId, IList<string> variableNameList);

    string SaveVariable(ProcessVariableEntity entity);

    string SaveVariable(SQLBuilder kit, ProcessVariableEntity entity);

    bool ValidateProcessVariable(string processInstanceId, string expression);

    bool ValidateProcessVariable(SQLBuilder kit, string processInstanceId, string expression);

    void DeleteVariable(string processId, string variableId);

    void DeleteVariable(SQLBuilder kit, string processId, string variableId);
}
