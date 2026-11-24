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
using sailflow.assignee;
using sailflow.engine.Common;
using sailflow.hooks;
using sailflow.engine.Xpdl;
using sailflow.engine.Result;
using sailflow.engine.Client;
using sailflow.log;
using sailflow.run;
using sailflow.engine.Factories;
using sailflow.WebUtility;

namespace sailflow.data;

/// <summary>契约 · ProcessInstanceManager（实现位于 sailflow.runtime）。</summary>
public interface IProcessInstanceManager
{
    ProcessInstanceEntity GetProcessInstanceLatest(string bizPK, string processId, string version);

    ProcessInstanceEntity GetProcessInstanceLatest( SQLBuilder kit, string bizPK, string processId, string version);

    ProcessInstanceEntity GetById(string processInstanceId);

    ProcessInstanceEntity GetById(SQLBuilder kit, string processInstanceId);

    Task<ProcessInstanceEntity> GetByIdAsync( SQLBuilder kit, string processInstanceId, CancellationToken cancellationToken = default);

    ProcessInstanceEntity GetByActivity(string activityInstanceId);

    ProcessInstanceEntity GetByActivity(SQLBuilder kit, string activityInstanceId);

    bool IsSubProcess(ProcessInstanceEntity entity);

    User GetProcessInitiator(string processInstanceId);

    User GetProcessInitiator(SQLBuilder kit, string processInstanceId);

    ProcessInstanceEntity GetProcessInstanceCurrent(string bizPK, string processId, string version);

    ProcessInstanceEntity GetProcessInstanceCurrent( SQLBuilder kit, string bizPK, string processId, string version);

    IEnumerable<ProcessInstanceEntity> GetProcessInstance(string bizPK);

    IEnumerable<ProcessInstanceEntity> GetProcessInstance( SQLBuilder kit, string bizPK, string processId, string version);

    ProcessInstanceEntity GetSubProcessInstance( SQLBuilder kit, string bizPK, string processId, string subProcessId);

    ProcessInstanceEntity GetRunningProcessInstance(string bizPK, string processId);

    bool CheckSubProcessInstanceCompleted(SQLBuilder kit, string activityInstanceId, string activityId);

    string Insert(ProcessInstanceEntity entity);

    string Insert(SQLBuilder kit, ProcessInstanceEntity entity);

    void Update(ProcessInstanceEntity entity);

    void Update(SQLBuilder kit, ProcessInstanceEntity entity);

    ProcessInstanceEntity CreateNewProcessInstanceObject( FlowCommandContext runner, ProcessEntity processEntity, SubProcessNode subProcessNode = null, SQLBuilder kit = null);

    ProcessInstanceEntity Complete(string processInstanceId, FlowCommandContext runner);

    ProcessInstanceEntity Complete(SQLBuilder kit, string processInstanceId, FlowCommandContext runner);

    void Suspend(string processInstanceId, FlowCommandContext runner);

    void Suspend(SQLBuilder kit, string processInstanceId, FlowCommandContext runner);

    void Resume(string processInstanceId, FlowCommandContext runner);

    void Resume(SQLBuilder kit, string processInstanceId, FlowCommandContext runner);

    void RecallSubProcess(SQLBuilder kit, string invokedActivityInstanceId, FlowCommandContext runner);

    void Reverse(SQLBuilder kit, string processInstanceId, FlowCommandContext currentUser);

    bool Cancel(FlowCommandContext runner);

    bool Cancel(SQLBuilder kit, FlowCommandContext runner);

    bool Discard(FlowCommandContext runner);

    void Discard(SQLBuilder kit, FlowCommandContext runner);

    bool Terminate(FlowCommandContext runner);

    bool Terminate(SQLBuilder kit, ProcessInstanceEntity entity, string userId, string userName);

    bool Terminate(SQLBuilder kit, ProcessInstanceEntity entity, FlowCommandContext runner);

    bool SetOverdue(string processInstanceId, DateTime overdueDateTime, FlowCommandContext runner);

    bool SetOverdue(SQLBuilder kit, string processInstanceId, DateTime overdueDateTime, FlowCommandContext runner);

    bool Delete(string processInstanceId, bool isForced = false);

    bool Delete(SQLBuilder kit, string processInstanceId, bool isForced = false);

    bool Delete(string processId, string version);
}
