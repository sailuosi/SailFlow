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
using sailflow.engine.Result;
using sailflow.hosting;
using sailflow.log;
using sailflow.run;
using sailflow.factory;

namespace sailflow.data;

/// <summary>契约 · TaskManager（实现位于 sailflow.runtime）。</summary>
public interface ITaskManager
{
    TaskViewEntity GetTaskView(string taskId);

    TaskViewEntity GetTaskView(SQLBuilder kit, string taskId);

    Task<TaskViewEntity> GetTaskViewAsync( SQLBuilder kit, string taskId, CancellationToken cancellationToken = default);

    TaskEntity GetTask(string taskId);

    TaskEntity GetTask(SQLBuilder kit, string taskId);

    List<TaskEntity> GetTaskListByActivity(string processInstanceId, string activityInstanceId);

    List<TaskEntity> GetTaskListByActivity(SQLBuilder kit, string processInstanceId, string activityInstanceId);

    List<TaskEntity> GetTaskListByActivityIds( SQLBuilder kit, string processInstanceId, IEnumerable<string> activityInstanceIds);

    TaskEntity GetTaskByActivity(string processInstanceId, string activityInstanceId);

    TaskEntity GetTaskByActivity(SQLBuilder kit, string processInstanceId, string activityInstanceId);

    TaskViewEntity GetTaskViewByActivity(string processInstanceId, string activityInstanceId);

    TaskViewEntity GetTaskViewByActivity(SQLBuilder kit, string processInstanceId, string activityInstanceId);

    Task<TaskViewEntity> GetTaskViewByActivityAsync( SQLBuilder kit, string processInstanceId, string activityInstanceId, CancellationToken cancellationToken = default);

    TaskViewEntity GetTaskOfMineByActivityInstance(string activityInstanceId, string userId);

    TaskViewEntity GetTaskOfMineByActivityInstance(SQLBuilder kit, string activityInstanceId, string userId);

    Task<TaskViewEntity> GetTaskOfMineByActivityInstanceAsync( SQLBuilder kit, string activityInstanceId, string userId, CancellationToken cancellationToken = default);

    TaskViewEntity GetTaskOfMine(string bizPK, string processId, string userId);

    TaskViewEntity GetTaskOfMine(SQLBuilder kit, string bizPK, string processId, string userId);

    TaskViewEntity GetTaskOfMine(FlowCommandContext runner);

    TaskViewEntity GetTaskOfMine(SQLBuilder kit, FlowCommandContext runner);

    bool IsMine(TaskViewEntity entity, string userId);

    Boolean CheckTaskStateInRunningState(TaskViewEntity task);

    void Insert(SQLBuilder kit, ActivityInstanceEntity activityInstance, PerformerList performers, FlowCommandContext runner);

    string Insert(SQLBuilder kit, ActivityInstanceEntity activityInstance, Performer performer, FlowCommandContext runner);

    void Renew(SQLBuilder kit, ActivityInstanceEntity sourceActivityInstance, ActivityInstanceEntity newInstance, FlowCommandContext runner);

    void Update(TaskEntity entity);

    void Update(SQLBuilder kit, TaskEntity entity);

    void MarkRead(SQLBuilder kit, string taskId);

    internal void SetTaskType(SQLBuilder kit, string taskId, TaskTypeEnum taskType);

    void SetTaskRead(FlowCommandContext taskRunner);

    void EndTaskState(SQLBuilder kit, string taskId, TaskStateEnum taskState, FlowCommandContext runner, string endType = null);

    /// <summary>静态：写入任务结束态字段（不落库）。</summary>
    static void ApplyTaskEnd(
        TaskEntity task,
        TaskStateEnum taskState,
        FlowCommandContext runner,
        string endType = null)
    {
        if (task == null)
            return;
        task.TaskState = (byte)taskState;
        task.CompletedAt = DateTime.UtcNow;
        if (runner != null)
        {
            task.EndedUserId = runner.UserId;
            task.EndedUserName = runner.UserName;
            if (string.IsNullOrWhiteSpace(task.Msg))
            {
                string comment = null;
                if (runner.DynamicVariables != null
                    && runner.DynamicVariables.TryGetValue("sail.comment", out var dynComment)
                    && !string.IsNullOrWhiteSpace(dynComment))
                    comment = dynComment;
                else if (runner.Conditions != null
                         && runner.Conditions.TryGetValue("comment", out var condComment)
                         && !string.IsNullOrWhiteSpace(condComment))
                    comment = condComment;
                if (!string.IsNullOrWhiteSpace(comment))
                    task.Msg = comment;
            }
        }
        if (!string.IsNullOrWhiteSpace(endType))
            task.EndType = endType;
    }

    void Complete(SQLBuilder kit, string taskId, FlowCommandContext runner);

    void CloseProxyChainOpenTasks( SQLBuilder kit, TaskEntity completed, FlowCommandContext runner);

    bool TryCompleteAsDelegationReturn(SQLBuilder kit, string taskId, FlowCommandContext runner);

    WfDataManagedResult Delegate( SQLBuilder kit, string taskId, FlowCommandContext runner, PerformerList targets);

    void Withdraw(SQLBuilder kit, string taskId, FlowCommandContext runner);

    void SendBack(SQLBuilder kit, string taskId, FlowCommandContext runner);

    WfDataManagedResult Entrust(TaskEntrustedEntity entity, bool cancelOriginalTask = true);

    WfDataManagedResult Entrust(SQLBuilder kit, TaskEntrustedEntity entity, bool cancelOriginalTask = true);

    WfDataManagedResult Transfer( SQLBuilder kit, string taskId, FlowCommandContext runner, PerformerList targets);

    WfDataManagedResult UrgeOpenTasks( SQLBuilder kit, string taskId, FlowCommandContext runner, ISet<string> filterUserIds = null);

    WfDataManagedResult ChangeAssignees( SQLBuilder kit, string taskId, FlowCommandContext runner, PerformerList newAssignees);

    WfDataManagedResult AddAssignees( SQLBuilder kit, string taskId, FlowCommandContext runner, PerformerList addTargets);

    WfDataManagedResult RemoveAssignees( SQLBuilder kit, string taskId, FlowCommandContext runner, PerformerList removeTargets);

    void RefreshActivitySignFromOpenTasks( SQLBuilder kit, ActivityInstanceEntity activityInstance);

    List<TaskEntity> GetOpenApproverTasks( SQLBuilder kit, string processInstanceId, string activityInstanceId);

    bool HasOpenParticipants(SQLBuilder kit, string consultSrcTaskId);

    List<TaskEntity> GetOpenParticipantsBySrc(SQLBuilder kit, string consultSrcTaskId);

    WfDataManagedResult Consult( SQLBuilder kit, string srcTaskId, FlowCommandContext runner, PerformerList targets);

    WfDataManagedResult Acknowledge( SQLBuilder kit, string taskId, FlowCommandContext runner);

    WfDataManagedResult RecallConsult( SQLBuilder kit, string srcTaskId, FlowCommandContext runner);

    void CancelOpenParticipantsOnActivity( SQLBuilder kit, ActivityInstanceEntity activityInstance, FlowCommandContext runner);

    void CloseOpenApproverTasksOnActivity( SQLBuilder kit, ActivityInstanceEntity activityInstance, FlowCommandContext runner, string preferredEndType = null, string completedSignId = null, string completedProxySignId = null);

    void CloseOpenApproverTasksOnActivityIds( SQLBuilder kit, string processInstanceId, IEnumerable<string> activityInstanceIds, FlowCommandContext runner, string endType = null);

    void CancelEntrustedTask(string taskId);

    void CancelEntrustedTask(SQLBuilder kit, string taskId);

    Boolean Replace(string activityInstanceId, List<TaskReplacedEntity> replaced, FlowCommandContext runner);

    void Replace(SQLBuilder kit, string activityInstanceId, List<TaskReplacedEntity> replaced, FlowCommandContext runner);

    bool Delete(string taskId);

    bool Delete(SQLBuilder kit, string taskId);
}
