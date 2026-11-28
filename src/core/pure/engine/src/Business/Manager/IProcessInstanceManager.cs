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
using sailflow.utils;
using sailflow.hooks;
using sailflow.xml;
using sailflow.result;
using sailflow.hosting;
using sailflow.log;
using sailflow.run;
using sailflow.factory;

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

    ProcessInstanceEntity CreateNewProcessInstanceObject( FlowCommandContext context, ProcessEntity processEntity, SubProcessNode subProcessNode = null, SQLBuilder kit = null);

    ProcessInstanceEntity Complete(string processInstanceId, FlowCommandContext context);

    ProcessInstanceEntity Complete(SQLBuilder kit, string processInstanceId, FlowCommandContext context);

    void Suspend(string processInstanceId, FlowCommandContext context);

    void Suspend(SQLBuilder kit, string processInstanceId, FlowCommandContext context);

    void Resume(string processInstanceId, FlowCommandContext context);

    void Resume(SQLBuilder kit, string processInstanceId, FlowCommandContext context);

    void RecallSubProcess(SQLBuilder kit, string invokedActivityInstanceId, FlowCommandContext context);

    void Reverse(SQLBuilder kit, string processInstanceId, FlowCommandContext currentUser);

    bool Cancel(FlowCommandContext context);

    bool Cancel(SQLBuilder kit, FlowCommandContext context);

    bool Discard(FlowCommandContext context);

    void Discard(SQLBuilder kit, FlowCommandContext context);

    bool Terminate(FlowCommandContext context);

    bool Terminate(SQLBuilder kit, ProcessInstanceEntity entity, string userId, string userName);

    bool Terminate(SQLBuilder kit, ProcessInstanceEntity entity, FlowCommandContext context);

    bool SetOverdue(string processInstanceId, DateTime overdueDateTime, FlowCommandContext context);

    bool SetOverdue(SQLBuilder kit, string processInstanceId, DateTime overdueDateTime, FlowCommandContext context);

    bool Delete(string processInstanceId, bool isForced = false);

    bool Delete(SQLBuilder kit, string processInstanceId, bool isForced = false);

    bool Delete(string processId, string version);
}
