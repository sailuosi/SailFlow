
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using sailflow.data;
using sailflow.hub.Localize;
using sailflow.engine.Common;
using sailflow.engine.Hooks;
using sailflow.engine.Entity;
using sailflow.engine.Xpdl;
using sailflow.engine.Result;
using sailflow.engine.Client;
using sailflow.engine.Logging;
using sailflow.WebUtility;
using sailflow.engine.Manager;
using sailflow.engine.Runtime;


namespace sailflow.runtime.Manager
{
    /// <summary>
    /// Task manager: including tasks and task view objects
    /// 任务管理类：包括任务及任务视图对象
    /// </summary>
    public class TaskManager : ITaskManager
    {
        public TaskManager(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        Exception FailThrow(Exception ex, string op, string detail = null)
        {
            SailFailLog.Exception(Sail, SailLogCategories.Engine, "Task." + op, ex, detail);
            return ex;
        }
        
        #region TaskView 任务视图
        /// <summary>
        /// Retrieve task view based on task Id
        /// 根据任务ID获取任务视图
        /// </summary>
        public TaskViewEntity GetTaskView(string taskId)
        {
            return Sail.UseRunSQL().findRowById<TaskViewEntity>(taskId);
        }

        public TaskViewEntity GetTaskView(SQLBuilder kit, string taskId)
            => kit.findRowById<TaskViewEntity>(taskId);

        /// <summary>
        /// 异步按任务 Id 取任务视图（Clip，可取消）。
        /// </summary>
        public async Task<TaskViewEntity> GetTaskViewAsync(
            SQLBuilder kit,
            string taskId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var clip = kit.useClip();
            clip.from<TaskViewEntity>(out var t)
                .where(() => t.TaskId, taskId);
            var list = await clip.select(t).take(1).queryListAsync(cancellationToken).ConfigureAwait(false);
            return list?.FirstOrDefault();
        }

        /// <summary>
        /// Retrieve task based on task Id
        /// 获取任务
        /// </summary>
        public TaskEntity GetTask(string taskId)
            => Sail.UseRunSQL().findRowById<TaskEntity>(taskId);

        public TaskEntity GetTask(SQLBuilder kit, string taskId)
            => kit.findRowById<TaskEntity>(taskId);

        /// <summary>
        /// Obtain tasks based on process information
        /// 根据流程信息获取任务
        /// </summary>
        public List<TaskEntity> GetTaskListByActivity(string processInstanceId, string activityInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetTaskListByActivity(kit, processInstanceId, activityInstanceId);
        }

        public List<TaskEntity> GetTaskListByActivity(SQLBuilder kit, string processInstanceId, string activityInstanceId)
        {
            var clip = kit.useClip();
            clip.from<TaskEntity>(out var t)
                .where(() => t.ActivityInstanceId, activityInstanceId)
                .where(() => t.ProcessInstanceId, processInstanceId);
            return clip.select(t).queryList().ToList();
        }

        /// <summary>按多个活动实例 Id 拉取任务（Leave Seats 投影用）。</summary>
        public List<TaskEntity> GetTaskListByActivityIds(
            SQLBuilder kit,
            string processInstanceId,
            IEnumerable<string> activityInstanceIds)
        {
            var ids = activityInstanceIds?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();
            if (ids.Count == 0 || string.IsNullOrWhiteSpace(processInstanceId))
                return new List<TaskEntity>();

            var clip = kit.useClip();
            clip.from<TaskEntity>(out var t)
                .where(() => t.ProcessInstanceId, processInstanceId)
                .whereIn(() => t.ActivityInstanceId, ids);
            return clip.select(t).queryList().ToList();
        }

        /// <summary>
        /// Obtain tasks based on process information
        /// 根据流程信息获取任务
        /// </summary>
        public TaskEntity GetTaskByActivity(string processInstanceId, string activityInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetTaskByActivity(kit, processInstanceId, activityInstanceId);
        }

        public TaskEntity GetTaskByActivity(SQLBuilder kit, string processInstanceId, string activityInstanceId)
        {
            var list = GetTaskListByActivity(kit, processInstanceId, activityInstanceId);
            return list.Count > 0 ? list[0] : null;
        }

        /// <summary>
        /// Obtain tasks based on process information
        /// 根据流程信息获取任务
        /// </summary>
        public TaskViewEntity GetTaskViewByActivity(string processInstanceId, string activityInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetTaskViewByActivity(kit, processInstanceId, activityInstanceId);
        }

        public TaskViewEntity GetTaskViewByActivity(SQLBuilder kit, string processInstanceId, string activityInstanceId)
        {
            var clip = kit.useClip();
            clip.from<TaskViewEntity>(out var t)
                .where(() => t.ActivityInstanceId, activityInstanceId)
                .where(() => t.ProcessInstanceId, processInstanceId);
            var list = clip.select(t).queryList().ToList();
            return list.Count > 0 ? list[0] : null;
        }

        /// <summary>
        /// 异步按活动实例取任务视图（可取消）。
        /// </summary>
        public async Task<TaskViewEntity> GetTaskViewByActivityAsync(
            SQLBuilder kit,
            string processInstanceId,
            string activityInstanceId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var clip = kit.useClip();
            clip.from<TaskViewEntity>(out var t)
                .where(() => t.ActivityInstanceId, activityInstanceId)
                .where(() => t.ProcessInstanceId, processInstanceId);
            var list = (await clip.select(t).queryListAsync(cancellationToken).ConfigureAwait(false)).ToList();
            return list.Count > 0 ? list[0] : null;
        }

        #endregion

        #region Get the current user's processing tasks 获取当前用户的办理任务
        /// <summary>
        /// Get my task
        /// 获取我的任务
        /// </summary>
        public TaskViewEntity GetTaskOfMineByActivityInstance(string activityInstanceId, string userId)
        {
            var kit = Sail.UseRunSQL();
            return GetTaskOfMineByActivityInstance(kit, activityInstanceId, userId);
        }

        public TaskViewEntity GetTaskOfMineByActivityInstance(SQLBuilder kit,
            string activityInstanceId,
            string userId)
        {
            var clip = kit.useClip();
            clip.from<TaskViewEntity>(out var t)
                .where(() => t.ActivityInstanceId, activityInstanceId)
                .where(() => t.SignId, userId)
                .where(() => t.ProcessState, (short)2)
                .whereIn(() => t.ActivityType, new short?[] { (short)4, (short)5, (short)6 })
                .whereIn(() => t.ActivityState, new short?[] { (short)1, (short)2 })
                .orderByDesc(() => t.ActiveAt)
                .top(1);
            var list = clip.select(t).queryList().ToList();

            return ResolveTaskOfMineByActivityInstance(list, activityInstanceId);
        }

        /// <summary>
        /// 异步获取当前用户在活动实例上的办理任务（可取消）。
        /// </summary>
        public async Task<TaskViewEntity> GetTaskOfMineByActivityInstanceAsync(
            SQLBuilder kit,
            string activityInstanceId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var clip = kit.useClip();
            clip.from<TaskViewEntity>(out var t)
                .where(() => t.ActivityInstanceId, activityInstanceId)
                .where(() => t.SignId, userId)
                .where(() => t.ProcessState, (short)2)
                .whereIn(() => t.ActivityType, new short?[] { (short)4, (short)5, (short)6 })
                .whereIn(() => t.ActivityState, new short?[] { (short)1, (short)2 })
                .orderByDesc(() => t.ActiveAt)
                .top(1);
            var list = (await clip.select(t).queryListAsync(cancellationToken).ConfigureAwait(false)).ToList();
            return ResolveTaskOfMineByActivityInstance(list, activityInstanceId);
        }

        private TaskViewEntity ResolveTaskOfMineByActivityInstance(
            List<TaskViewEntity> list, string activityInstanceId)
        {
            if (list.Count == 0)
            {
                var message = Sail.Messages.GetEngineMessage("taskmanager.gettaskofmine.error");
                throw FailThrow(new WorkflowException(
                    string.Format("{0}，ActivityInstanceId: {1}", message, activityInstanceId.ToString())
                ), "GetTaskOfMine");
            }
            if (list.Count > 1)
            {
                throw FailThrow(new WorkflowException(
                    Sail.Messages.GetEngineMessage("taskmanager.gettaskofmine.toomoretasks.error")), "GetTaskOfMine");
            }
            return list[0];
        }

        /// <summary>
        /// Based on the application instance and process GUID, process the user Id to obtain the task list
        /// 根据应用实例、流程GUID，办理用户Id获取任务列表
        /// </summary>
        public TaskViewEntity GetTaskOfMine(string bizPK, string processId, string userId)
        {
            var kit = Sail.UseRunSQL();
            return GetTaskOfMine(kit, bizPK, processId, userId);
        }

        public TaskViewEntity GetTaskOfMine(SQLBuilder kit,
            string bizPK,
            string processId,
            string userId)
        {
            var clip = kit.useClip();
            clip.from<TaskViewEntity>(out var t)
                .where(() => t.BizPK, bizPK)
                .where(() => t.ProcessId, processId)
                .where(() => t.SignId, userId)
                .where(() => t.ProcessState, (short)2)
                .sinkOR()
                .whereIn(() => t.ActivityType, new short?[] { (short)4, (short)5, (short)6 })
                .where(() => t.WorkItemType, (short)1)
                .rise()
                .whereIn(() => t.ActivityState, new short?[] { (short)1, (short)2 })
                .orderByDesc(() => t.ActiveAt)
                .top(1);
            var list = clip.select(t).queryList().ToList();

            if (list.Count == 0)
            {
                var message = Sail.Messages.GetEngineMessage("taskmanager.gettaskofmine.error");
                throw FailThrow(new WorkflowException(
                    string.Format("{0}，BizPK: {1}", message, bizPK.ToString())
                ), "GetTaskOfMine");
            }
            if (list.Count > 1)
            {
                throw FailThrow(new WorkflowException(
                    Sail.Messages.GetEngineMessage("taskmanager.gettaskofmine.toomoretasks.error")), "GetTaskOfMine");
            }
            return list[0];
        }

        /// <summary>
        /// Get my taskview
        /// 获取任务视图
        /// </summary>
        public TaskViewEntity GetTaskOfMine(FlowCommandContext runner)
        {
            var kit = Sail.UseRunSQL();
            return GetTaskOfMine(kit, runner);
        }

        public TaskViewEntity GetTaskOfMine(SQLBuilder kit, FlowCommandContext runner)
        {
            if (runner.TaskId != null)
                return GetTaskView(kit, runner.TaskId);
            return GetTaskOfMine(kit, runner.BizPK, runner.ProcessId, runner.UserId);
        }

        /// <summary>
        /// Determine whether the task belongs to a certain user
        /// 判断任务是否属于某个用户
        /// </summary>
        public bool IsMine(TaskViewEntity entity, string userId)
        {
            var isMine = false;
            if (entity.SignId == userId) isMine = true;
            return isMine;
        }

        /// <summary>
        /// Determine if the task is in a running state
        /// 判断任务处于运行状态
        /// </summary>
        public Boolean CheckTaskStateInRunningState(TaskViewEntity task)
        {
            var isRunning = true;
            if (task.TaskState == (short)TaskStateEnum.Completed)
            {
                isRunning = false;
            }
            else if (task.ActivityState == (short)ActivityStateEnum.Completed){
                isRunning = false;
            }
            return isRunning;
        }

        /// <summary>
        /// Determine if the task is in a todo state
        /// 判断任务处于待办或运行状态
        /// </summary>
        private Boolean CheckTaskStateInToDoState(TaskViewEntity task)
        {
            var isInToDoState = false;
            if (task != null && (task.TaskState == (short)TaskStateEnum.Waiting
                    || task.TaskState == (short)TaskStateEnum.Reading))
            {
                isInToDoState = true;
            }
            return isInToDoState;
        }

        #endregion

        #region ITaskManager 任务数据基本操作
        private string Insert(SQLBuilder kit, TaskEntity entity)
        {
            string newTaskId = kit.useRepo<TaskEntity, string>().InsertReturnId(entity);
            entity.TaskId = newTaskId;
            return newTaskId;
        }

        public void Insert(SQLBuilder kit, ActivityInstanceEntity activityInstance,
            PerformerList performers,
            FlowCommandContext runner)
        {
            foreach (Performer performer in performers)
                Insert(kit, activityInstance, performer, runner);
        }

        public string Insert(SQLBuilder kit, ActivityInstanceEntity activityInstance,
            Performer performer,
            FlowCommandContext runner)
        {
            return Insert(kit, activityInstance, performer, runner.UserId, runner.UserName, null, runner);
        }

        private string Insert(SQLBuilder kit, ActivityInstanceEntity activityInstance,
            string performerId,
            string performerName,
            string runnerId,
            string runnerName,
            string entrustedTaskId = null,
            FlowCommandContext runner = null)
        {
            return Insert(kit, activityInstance,
                new Performer(performerId, performerName)
                {
                    GroupId = activityInstance?.GroupId,
                    GroupName = activityInstance?.GroupName,
                    GroupRole = activityInstance?.GroupRole,
                    UnitId = activityInstance?.UnitId,
                    UnitCode = activityInstance?.UnitCode,
                    UnitName = activityInstance?.UnitName
                },
                runnerId, runnerName, entrustedTaskId, runner);
        }

        private string Insert(SQLBuilder kit, ActivityInstanceEntity activityInstance,
            Performer performer,
            string runnerId,
            string runnerName,
            string entrustedTaskId = null,
            FlowCommandContext runner = null)
        {
            var entity = new TaskEntity
            {
                BizChannel = activityInstance.BizChannel,
                BizPK = activityInstance.BizPK,
                ActivityInstanceId = activityInstance.ActivityInstanceId,
                ProcessInstanceId = activityInstance.ProcessInstanceId,
                ActivityId = activityInstance.ActivityId,
                ActivityName = activityInstance.ActivityName,
                ProcessId = activityInstance.ProcessId,
                TaskType = (short)TaskTypeEnum.Manual,
                SignId = performer?.UserId,
                SignName = performer?.UserName,
                GroupId = !string.IsNullOrEmpty(performer?.GroupId) ? performer.GroupId : activityInstance.GroupId,
                GroupName = !string.IsNullOrEmpty(performer?.GroupName) ? performer.GroupName : activityInstance.GroupName,
                GroupRole = !string.IsNullOrEmpty(performer?.GroupRole) ? performer.GroupRole : activityInstance.GroupRole,
                UnitId = !string.IsNullOrEmpty(performer?.UnitId) ? performer.UnitId : activityInstance.UnitId,
                UnitCode = !string.IsNullOrEmpty(performer?.UnitCode) ? performer.UnitCode : activityInstance.UnitCode,
                UnitName = !string.IsNullOrEmpty(performer?.UnitName) ? performer.UnitName : activityInstance.UnitName,
                Weight = performer != null && performer.Weight > 0
                    ? performer.Weight
                    : (activityInstance.Weight is > 0 ? activityInstance.Weight : 1m),
                ProxySignId = !string.IsNullOrWhiteSpace(performer?.ProxySignId)
                    ? performer.ProxySignId
                    : activityInstance.ProxySignId,
                TaskState = 1,
                // 业务邮件标记：未发送（sailflow.biz.query.TaskEMailSentStatusEnum.UnSent）
                IsEMailSent = 0,
                Idx = 0,
                IsRead = 0,
                CreatedUserId = runnerId,
                CreatedUserName = runnerName,
                CreatedDateTime = DateTime.UtcNow,
                RecordStatusInvalid = 0
            };
            StampNewTaskClocks(entity, activateNow: true);
            if (!string.IsNullOrWhiteSpace(entrustedTaskId))
                entity.EntrustedTaskId = entrustedTaskId;

            string taskId = Insert(kit, entity);
            entity.TaskId = taskId;
            Sail.CapabilityDispatcher.FireTaskLifecycle(kit, entity, activityInstance, runner, TaskNotifyKind.Created);
            return taskId;
        }

        /// <summary>
        /// Regenerate task (limited to co signing child nodes under multiple instances)
        /// 重新生成任务(只限于会签多实例下的子节点)
        /// </summary>
        public void Renew(SQLBuilder kit, ActivityInstanceEntity sourceActivityInstance,
            ActivityInstanceEntity newInstance,
            FlowCommandContext runner)
        {
            var performer = new Performer(sourceActivityInstance.SignIds,
                sourceActivityInstance.SignNames)
            {
                GroupId = sourceActivityInstance.GroupId,
                GroupName = sourceActivityInstance.GroupName,
                GroupRole = sourceActivityInstance.GroupRole,
                UnitId = sourceActivityInstance.UnitId,
                UnitCode = sourceActivityInstance.UnitCode,
                UnitName = sourceActivityInstance.UnitName
            };
            Insert(kit, newInstance, performer, runner);
        }

        public void Update(TaskEntity entity)
        {
            var kit = Sail.UseRunSQL();
            Update(kit, entity);
        }

        public void Update(SQLBuilder kit, TaskEntity entity)
            => kit.update(entity);

        /// <summary>
        /// 新待办时钟：发送/首次下达；可立即办理时写 ActiveAt。
        /// </summary>
        static void StampNewTaskClocks(TaskEntity entity, bool activateNow)
        {
            var now = DateTime.UtcNow;
            entity.SendAt = now;
            if (entity.GotAt == null)
                entity.GotAt = now;
            if (activateNow && entity.ActiveAt == null)
                entity.ActiveAt = now;
            if (entity.Idx == null)
                entity.Idx = 0;
            if (entity.IsRead == null)
                entity.IsRead = 0;
        }

        /// <summary>
        /// 幂等标记已阅：首次写入 IsRead=1 与 ReadAt（不单独落库，由调用方 Update）。
        /// </summary>
        public static void ApplyMarkRead(TaskEntity task)
        {
            if (task == null)
                return;
            if (task.IsRead == 1 && task.ReadAt != null)
                return;
            task.IsRead = 1;
            task.ReadAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 幂等标记已阅并落库。
        /// </summary>
        public void MarkRead(SQLBuilder kit, string taskId)
        {
            var task = GetTask(kit, taskId);
            if (task == null)
                return;
            ApplyMarkRead(task);
            Update(kit, task);
        }

        void ITaskManager.SetTaskType(SQLBuilder kit, string taskId, TaskTypeEnum taskType)
        {
            var task = GetTask(kit, taskId);
            task.TaskType = (short)taskType;
            Update(kit, task);
        }

        public void SetTaskRead(FlowCommandContext taskRunner)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var task = GetTask(kit, taskRunner.TaskId);
                    ApplyMarkRead(task);
                    SetTaskState(kit, task, taskRunner.UserId, taskRunner.UserName, TaskStateEnum.Reading);
                    Sail.ManagerFactory.Activities.Read(kit, task.ActivityInstanceId, taskRunner);
                    kit.commit();
                }
                catch (System.Exception e)
                {
                    throw FailThrow(new WorkflowException(
                        Sail.Messages.GetEngineMessage("taskmanager.settaskread.error", e.Message), e), "SetTaskRead");
                }
            }
        }

        private void SetTaskState(SQLBuilder kit, TaskEntity task,
            string userId,
            string userName,
            TaskStateEnum taskState)
        {
            task.TaskState = (short)taskState;
            task.UpdatedUserId = userId;
            task.UpdatedUserName = userName;
            task.UpdatedDateTime = DateTime.UtcNow;
            Update(kit, task);
        }

        public void EndTaskState(SQLBuilder kit, string taskId,
            TaskStateEnum taskState,
            FlowCommandContext runner,
            string endType = null)
        {
            var task = GetTask(kit, taskId);
            ApplyTaskEnd(task, taskState, runner, endType);
            Update(kit, task);
        }

        public static void ApplyTaskEnd(
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
                // 办理意见：优先 sail.comment（自动审批），否则 Conditions.comment（办理台）
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

        public void Complete(SQLBuilder kit, string taskId, FlowCommandContext runner)
        {
            EndTaskState(kit, taskId, TaskStateEnum.Completed, runner, TaskEndTypes.SelfComplete);
            var task = GetTask(kit, taskId);
            FireTaskLifecycle(kit, task, runner, TaskNotifyKind.Completed);
            // 代批同链：关其余打开待办（不改 Weight）
            CloseProxyChainOpenTasks(kit, task, runner);
        }

        /// <summary>
        /// 关闭同活动打开待办中与 <paramref name="completed"/> 同代理根的其余人（代批旁关）。
        /// </summary>
        public void CloseProxyChainOpenTasks(
            SQLBuilder kit,
            TaskEntity completed,
            FlowCommandContext runner)
        {
            if (completed == null || string.IsNullOrWhiteSpace(completed.ActivityInstanceId))
                return;

            var open = GetTaskListByActivity(
                kit, completed.ProcessInstanceId, completed.ActivityInstanceId);
            var rootSign = string.IsNullOrWhiteSpace(completed.ProxySignId)
                ? completed.SignId
                : completed.ProxySignId;

            foreach (var t in open)
            {
                if (t == null || string.Equals(t.TaskId, completed.TaskId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (t.TaskState != (short)TaskStateEnum.Waiting
                    && t.TaskState != (short)TaskStateEnum.Reading)
                    continue;
                if (AssigneeRoleHelper.IsParticipant(t.AssigneeRole))
                    continue;

                var tRoot = string.IsNullOrWhiteSpace(t.ProxySignId) ? t.SignId : t.ProxySignId;
                var sameChain =
                    string.Equals(tRoot, rootSign, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(t.SignId, rootSign, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(t.ProxySignId, completed.SignId, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(completed.ProxySignId, t.SignId, StringComparison.OrdinalIgnoreCase);
                if (!sameChain)
                    continue;

                ApplyTaskEnd(t, TaskStateEnum.Closed, runner, TaskEndTypes.ProxyTaken);
                Update(kit, t);
                var ai = Sail.ManagerFactory.Activities.GetById(kit, t.ActivityInstanceId);
                Sail.CapabilityDispatcher.FireTaskLifecycle(
                    kit, t, ai, runner, TaskNotifyKind.Cancelled);
            }
        }

        /// <summary>
        /// 若待办为委派受托任务（<see cref="TaskEntity.EntrustedTaskId"/> 非空）：办结受托待办并自动回转原责任人 Waiting，
        /// 返回 true（调用方<strong>不得</strong> Complete 活动实例 / ContinueForward）。否则返回 false，走正常办结。
        /// </summary>
        public bool TryCompleteAsDelegationReturn(SQLBuilder kit, string taskId, FlowCommandContext runner)
        {
            if (string.IsNullOrWhiteSpace(taskId))
                return false;

            var task = GetTask(kit, taskId);
            if (task == null || string.IsNullOrWhiteSpace(task.EntrustedTaskId))
                return false;

            var originalTaskId = task.EntrustedTaskId;
            EndTaskState(kit, taskId, TaskStateEnum.Completed, runner);
            FireTaskLifecycle(kit, GetTask(kit, taskId), runner, TaskNotifyKind.Completed);

            RestoreOriginalAssigneeAfterDelegation(kit, originalTaskId, runner);
            return true;
        }

        /// <summary>委派：关闭原人待办并为受托人建待办（挂 <c>EntrustedTaskId</c>）；不刷新 SignIds/Names（原人仍为责任锚点）。</summary>
        public WfDataManagedResult Delegate(
            SQLBuilder kit,
            string taskId,
            FlowCommandContext runner,
            PerformerList targets)
        {
            if (targets == null || targets.Count == 0)
                return WfDataManagedResult.Exception("Delegate requires at least one target performer.");

            var task = GetTask(kit, taskId);
            if (task == null)
                return WfDataManagedResult.Exception("Delegate task not found.");

            if (!string.IsNullOrWhiteSpace(task.EntrustedTaskId))
                return WfDataManagedResult.Exception("Delegate cannot nest: task is already a delegated task.");

            if (!string.Equals(task.SignId, runner?.UserId, StringComparison.OrdinalIgnoreCase))
                return WfDataManagedResult.Exception("Delegate is only allowed on the current user's task.");

            if (task.TaskState != (short)TaskStateEnum.Waiting
                && task.TaskState != (short)TaskStateEnum.Reading)
                return WfDataManagedResult.Exception("Delegate task is not in to-do state.");

            var am = Sail.ManagerFactory.Activities;
            var activityInstance = am.GetById(kit, task.ActivityInstanceId);
            if (activityInstance.ActivityState != (short)ActivityStateEnum.Ready
                && activityInstance.ActivityState != (short)ActivityStateEnum.Running)
            {
                return WfDataManagedResult.Exception("Delegate activity is not ready/running.");
            }

            Performer target = null;
            foreach (var p in targets)
            {
                if (p != null && !string.IsNullOrWhiteSpace(p.UserId))
                {
                    target = p;
                    break;
                }
            }
            if (target == null)
                return WfDataManagedResult.Exception("Delegate requires at least one target performer.");
            if (string.Equals(target.UserId, runner.UserId, StringComparison.OrdinalIgnoreCase))
                return WfDataManagedResult.Exception("Cannot delegate to self.");

            var existing = GetTaskListByActivity(
                kit, activityInstance.ProcessInstanceId, activityInstance.ActivityInstanceId);
            if (existing.Any(t =>
                    string.Equals(t.SignId, target.UserId, StringComparison.OrdinalIgnoreCase)
                    && (t.TaskState == (short)TaskStateEnum.Waiting
                        || t.TaskState == (short)TaskStateEnum.Reading)))
            {
                return WfDataManagedResult.Exception(
                    $"Delegate target already has an open task: {target.UserId}");
            }

            ApplyTaskEnd(task, TaskStateEnum.Closed, runner, TaskEndTypes.DelegatedOut);
            Update(kit, task);
            Sail.CapabilityDispatcher.FireTaskLifecycle(
                kit, task, activityInstance, runner, TaskNotifyKind.Entrusted);

            Insert(kit, activityInstance, target.UserId, target.UserName,
                runner.UserId, runner.UserName, taskId, runner);
            return WfDataManagedResult.Success();
        }

        void RestoreOriginalAssigneeAfterDelegation(
            SQLBuilder kit,
            string originalTaskId,
            FlowCommandContext runner)
        {
            var originalTask = GetTask(kit, originalTaskId);
            if (originalTask == null)
                throw FailThrow(new WorkflowException("Delegate return: original task not found."), "DelegateReturn");
            var newTask = new TaskEntity
            {
                BizPK = originalTask.BizPK,
                BizChannel = originalTask.BizChannel,
                ProcessInstanceId = originalTask.ProcessInstanceId,
                ProcessId = originalTask.ProcessId,
                ActivityId = originalTask.ActivityId,
                ActivityName = originalTask.ActivityName,
                ActivityInstanceId = originalTask.ActivityInstanceId,
                SignId = originalTask.SignId,
                SignName = originalTask.SignName,
                GroupId = originalTask.GroupId,
                GroupName = originalTask.GroupName,
                GroupRole = originalTask.GroupRole,
                UnitId = originalTask.UnitId,
                UnitCode = originalTask.UnitCode,
                UnitName = originalTask.UnitName,
                TaskState = (short)TaskStateEnum.Waiting,
                TaskType = originalTask.TaskType,
                SignKind = originalTask.SignKind,
                Idx = originalTask.Idx ?? 0,
                IsRead = 0,
                CreatedDateTime = DateTime.UtcNow,
                CreatedUserId = runner?.UserId ?? originalTask.CreatedUserId,
                CreatedUserName = runner?.UserName ?? originalTask.CreatedUserName
            };
            StampNewTaskClocks(newTask, activateNow: true);
            Insert(kit, newTask);
            FireTaskLifecycle(kit, newTask, runner, TaskNotifyKind.Created);
        }

        public void Withdraw(SQLBuilder kit, string taskId, FlowCommandContext runner)
        {
            EndTaskState(kit, taskId, TaskStateEnum.Withdrawed, runner, TaskEndTypes.Withdrawn);
            FireTaskLifecycle(kit, GetTask(kit, taskId), runner, TaskNotifyKind.Cancelled);
        }

        public void SendBack(SQLBuilder kit, string taskId, FlowCommandContext runner)
        {
            EndTaskState(kit, taskId, TaskStateEnum.SendBacked, runner, TaskEndTypes.SendBacked);
            FireTaskLifecycle(kit, GetTask(kit, taskId), runner, TaskNotifyKind.Cancelled);
        }

        private void FireTaskLifecycle(
            SQLBuilder kit,
            TaskEntity task,
            FlowCommandContext runner,
            TaskNotifyKind kind)
        {
            if (task?.ActivityInstanceId == null)
                return;
            var activityInstance = Sail.ManagerFactory.Activities.GetById(kit, task.ActivityInstanceId);
            Sail.CapabilityDispatcher.FireTaskLifecycle(kit, task, activityInstance, runner, kind);
        }

        public WfDataManagedResult Entrust(TaskEntrustedEntity entity, bool cancelOriginalTask = true)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var result = Entrust(kit, entity, cancelOriginalTask);
                    if (result.Status == WfDataManagedStatusEnum.Success)
                        kit.commit();
                    return result;
                }
                catch (System.Exception e)
                {
                    throw FailThrow(new WorkflowException(
                        Sail.Messages.GetEngineMessage("taskmanager.entrust.error"), e), "Entrust");
                }
            }
        }

        public WfDataManagedResult Entrust(SQLBuilder kit, TaskEntrustedEntity entity, bool cancelOriginalTask = true)
        {
            var am = Sail.ManagerFactory.Activities;
            var activityInstance = am.GetByTask(kit, entity.TaskId);

            if (activityInstance.ActivityState != (short)ActivityStateEnum.Ready
                && activityInstance.ActivityState != (short)ActivityStateEnum.Running)
            {
                return WfDataManagedResult.Exception(Sail.Messages.GetEngineMessage("taskmanager.entrust.warn"));
            }

            if (cancelOriginalTask)
            {
                var task = GetTask(kit, entity.TaskId);
                ApplyTaskEnd(task, TaskStateEnum.Closed, null, TaskEndTypes.DelegatedOut);
                Update(kit, task);
                Sail.CapabilityDispatcher.FireTaskLifecycle(
                    kit, task, activityInstance, null, TaskNotifyKind.Cancelled);
            }

            var taskList = GetTaskListByActivity(kit, activityInstance.ProcessInstanceId, activityInstance.ActivityInstanceId);
            if (taskList.Where(t => t.SignId == entity.EntrustToUserId).Any())
            {
                return WfDataManagedResult.Exception(Sail.Messages.GetEngineMessage("taskmanager.entrust.notagain.warn"));
            }

            Insert(kit, activityInstance, entity.EntrustToUserId, entity.EntrustToUserName,
                entity.RunnerId, entity.RunnerName, entity.TaskId);
            return WfDataManagedResult.Success();
        }

        /// <summary>
        /// 转办：结束本人待办（Closed，不计入会签完成），在本活动实例为接收人建待办，并刷新 SignIds/Names。
        /// 不推进下一节点。与 <see cref="Entrust"/> 相近，但面向产品 Transfer 命令且强制换写活动办理人集合。
        /// </summary>
        public WfDataManagedResult Transfer(
            SQLBuilder kit,
            string taskId,
            FlowCommandContext runner,
            PerformerList targets)
        {
            if (targets == null || targets.Count == 0)
                return WfDataManagedResult.Exception(
                    Sail.Messages.GetEngineMessage("taskmanager.transfer.noneTargets.warn"));

            var task = GetTask(kit, taskId);
            if (task == null)
                return WfDataManagedResult.Exception(
                    Sail.Messages.GetEngineMessage("taskmanager.transfer.taskNotFound.warn"));

            if (!string.Equals(task.SignId, runner.UserId, StringComparison.OrdinalIgnoreCase))
                return WfDataManagedResult.Exception(
                    Sail.Messages.GetEngineMessage("taskmanager.transfer.notMine.warn"));

            if (task.TaskState != (short)TaskStateEnum.Waiting
                && task.TaskState != (short)TaskStateEnum.Reading)
                return WfDataManagedResult.Exception(
                    Sail.Messages.GetEngineMessage("taskmanager.transfer.notTodo.warn"));

            var am = Sail.ManagerFactory.Activities;
            var activityInstance = am.GetById(kit, task.ActivityInstanceId);
            if (activityInstance.ActivityState != (short)ActivityStateEnum.Ready
                && activityInstance.ActivityState != (short)ActivityStateEnum.Running)
            {
                return WfDataManagedResult.Exception(
                    Sail.Messages.GetEngineMessage("taskmanager.transfer.activityNotRunning.warn"));
            }

            // 校验须在关原待办之前，避免拒绝时留下 Closed 脏状态
            var existing = GetTaskListByActivity(
                kit,
                activityInstance.ProcessInstanceId,
                activityInstance.ActivityInstanceId);
            var accepted = new PerformerList();
            foreach (var target in targets)
            {
                if (string.IsNullOrWhiteSpace(target?.UserId))
                    continue;
                if (string.Equals(target.UserId, runner.UserId, StringComparison.OrdinalIgnoreCase))
                    return WfDataManagedResult.Exception(
                        Sail.Messages.GetEngineMessage("taskmanager.transfer.toSelf.warn"));

                var hasOpen = existing.Any(t =>
                    string.Equals(t.SignId, target.UserId, StringComparison.OrdinalIgnoreCase)
                    && (t.TaskState == (short)TaskStateEnum.Waiting
                        || t.TaskState == (short)TaskStateEnum.Reading));
                if (hasOpen)
                    return WfDataManagedResult.Exception(
                        Sail.Messages.GetEngineMessage(
                            "taskmanager.transfer.targetHasOpen.warn", target.UserId));

                accepted.Add(target);
            }
            if (accepted.Count == 0)
                return WfDataManagedResult.Exception(
                    Sail.Messages.GetEngineMessage("taskmanager.transfer.noneTargets.warn"));

            // 关闭原人待办：Closed（勿用 Completed，避免被会签计为已办）
            ApplyTaskEnd(task, TaskStateEnum.Closed, runner, TaskEndTypes.TransferredOut);
            Update(kit, task);
            Sail.CapabilityDispatcher.FireTaskLifecycle(
                kit, task, activityInstance, runner, TaskNotifyKind.Entrusted);

            foreach (var target in accepted)
            {
                Insert(kit, activityInstance, target.UserId, target.UserName,
                    runner.UserId, runner.UserName, taskId, runner);
            }

            // 按仍打开的待办重写活动 SignIds/Names，转出人不再占用会签名额
            RefreshActivitySignFromOpenTasks(kit, activityInstance);
            return WfDataManagedResult.Success();
        }

        /// <summary>
        /// 催办：对当前活动打开待办发 <see cref="TaskNotifyKind.Urged"/>，不改 TaskState。
        /// <paramref name="filterUserIds"/> 非空时只催这些人；为空则催全部打开待办。
        /// </summary>
        public WfDataManagedResult UrgeOpenTasks(
            SQLBuilder kit,
            string taskId,
            FlowCommandContext runner,
            ISet<string> filterUserIds = null)
        {
            var task = GetTask(kit, taskId);
            if (task == null)
                return WfDataManagedResult.Exception("Urge task not found.");

            var am = Sail.ManagerFactory.Activities;
            var activityInstance = am.GetById(kit, task.ActivityInstanceId);
            if (activityInstance.ActivityState != (short)ActivityStateEnum.Ready
                && activityInstance.ActivityState != (short)ActivityStateEnum.Running)
            {
                return WfDataManagedResult.Exception("Urge activity is not ready/running.");
            }

            var open = GetTaskListByActivity(
                    kit,
                    activityInstance.ProcessInstanceId,
                    activityInstance.ActivityInstanceId)
                .Where(t => t.TaskState == (short)TaskStateEnum.Waiting
                    || t.TaskState == (short)TaskStateEnum.Reading)
                .ToList();

            if (filterUserIds != null && filterUserIds.Count > 0)
            {
                open = open
                    .Where(t => filterUserIds.Contains(t.SignId))
                    .ToList();
            }

            if (open.Count == 0)
                return WfDataManagedResult.Exception("Urge: no open task to notify.");

            foreach (var t in open)
            {
                Sail.CapabilityDispatcher.FireTaskLifecycle(
                    kit, t, activityInstance, runner, TaskNotifyKind.Urged);
            }

            return WfDataManagedResult.Success();
        }

        /// <summary>
        /// 改派：将当前活动未办待办差量对齐到新办理人集合（关旧 Cancelled / 建新 Created），刷新 SignIds/Names。
        /// 新集合至少一人；已在集合内且仍打开的待办保留。不推进下一节点。
        /// </summary>
        public WfDataManagedResult ChangeAssignees(
            SQLBuilder kit,
            string taskId,
            FlowCommandContext runner,
            PerformerList newAssignees)
        {
            if (newAssignees == null || newAssignees.Count == 0)
                return WfDataManagedResult.Exception("ChangeAssignee requires at least one target performer.");

            var task = GetTask(kit, taskId);
            if (task == null)
                return WfDataManagedResult.Exception("ChangeAssignee task not found.");

            var am = Sail.ManagerFactory.Activities;
            var activityInstance = am.GetById(kit, task.ActivityInstanceId);
            if (activityInstance.ActivityState != (short)ActivityStateEnum.Ready
                && activityInstance.ActivityState != (short)ActivityStateEnum.Running)
            {
                return WfDataManagedResult.Exception("ChangeAssignee activity is not ready/running.");
            }

            var desired = new Dictionary<string, Performer>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in newAssignees)
            {
                if (p == null || string.IsNullOrWhiteSpace(p.UserId))
                    continue;
                desired[p.UserId] = p;
            }
            if (desired.Count == 0)
                return WfDataManagedResult.Exception("ChangeAssignee requires at least one target performer.");

            // 仅改常规办理人；沟通参与者不动
            var open = GetOpenApproverTasks(
                kit,
                activityInstance.ProcessInstanceId,
                activityInstance.ActivityInstanceId);

            foreach (var t in open)
            {
                if (desired.ContainsKey(t.SignId))
                    continue;
                ApplyTaskEnd(t, TaskStateEnum.Closed, runner, TaskEndTypes.Removed);
                Update(kit, t);
                Sail.CapabilityDispatcher.FireTaskLifecycle(
                    kit, t, activityInstance, runner, TaskNotifyKind.Cancelled);
            }

            var stillOpenIds = new HashSet<string>(
                GetOpenApproverTasks(
                        kit,
                        activityInstance.ProcessInstanceId,
                        activityInstance.ActivityInstanceId)
                    .Select(t => t.SignId),
                StringComparer.OrdinalIgnoreCase);

            foreach (var kv in desired)
            {
                if (stillOpenIds.Contains(kv.Key))
                    continue;
                Insert(kit, activityInstance, kv.Value.UserId, kv.Value.UserName,
                    runner?.UserId, runner?.UserName, taskId, runner);
            }

            RefreshActivitySignFromOpenTasks(kit, activityInstance);
            return WfDataManagedResult.Success();
        }

        /// <summary>
        /// 加人：向当前扁节点（一 AI · 多 Task）追加普通办理人待办；已打开者跳过；不关任何人；不改 Leave / CompleteOrder；不推进。
        /// v1 拒绝 SignTogether / SignForward 及挂在其下的子 AI。
        /// </summary>
        public WfDataManagedResult AddAssignees(
            SQLBuilder kit,
            string taskId,
            FlowCommandContext runner,
            PerformerList addTargets)
        {
            if (addTargets == null || addTargets.Count == 0)
                return WfDataManagedResult.Exception("AddAssignee requires at least one target performer.");

            var task = GetTask(kit, taskId);
            if (task == null)
                return WfDataManagedResult.Exception("AddAssignee task not found.");

            var am = Sail.ManagerFactory.Activities;
            var activityInstance = am.GetById(kit, task.ActivityInstanceId);
            if (activityInstance.ActivityState != (short)ActivityStateEnum.Ready
                && activityInstance.ActivityState != (short)ActivityStateEnum.Running)
            {
                return WfDataManagedResult.Exception("AddAssignee activity is not ready/running.");
            }

            var flatGuard = TryEnsureFlatAssigneeNode(kit, activityInstance);
            if (flatGuard != null)
                return flatGuard;

            var desired = new Dictionary<string, Performer>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in addTargets)
            {
                if (p == null || string.IsNullOrWhiteSpace(p.UserId))
                    continue;
                desired[p.UserId] = p;
            }
            if (desired.Count == 0)
                return WfDataManagedResult.Exception("AddAssignee requires at least one target performer.");

            var openIds = new HashSet<string>(
                GetOpenApproverTasks(
                        kit,
                        activityInstance.ProcessInstanceId,
                        activityInstance.ActivityInstanceId)
                    .Select(t => t.SignId),
                StringComparer.OrdinalIgnoreCase);

            var inserted = 0;
            foreach (var kv in desired)
            {
                if (openIds.Contains(kv.Key))
                    continue;
                Insert(kit, activityInstance, kv.Value.UserId, kv.Value.UserName,
                    runner?.UserId, runner?.UserName, taskId, runner);
                openIds.Add(kv.Key);
                inserted++;
            }

            if (inserted == 0)
                return WfDataManagedResult.Exception("AddAssignee: all targets already open assignees.");

            RefreshActivitySignFromOpenTasks(kit, activityInstance);
            return WfDataManagedResult.Success();
        }

        /// <summary>
        /// v1 扁节点守卫：拒绝 SignTogether / SignForward 容器，以及 MainAct 指向会签/加签父的子 AI。
        /// </summary>
        WfDataManagedResult TryEnsureFlatAssigneeNode(SQLBuilder kit, ActivityInstanceEntity activityInstance)
        {
            if (activityInstance == null)
                return WfDataManagedResult.Exception("AddAssignee flat guard: activity is null.");

            if (IActivityInstanceManager.IsSignForwardContainer(activityInstance)
                || activityInstance.ComplexType == (short)ComplexTypeEnum.SignForward)
            {
                return WfDataManagedResult.Exception(
                    "AddAssignee requires flat node (not SignForward).");
            }

            if (activityInstance.ComplexType == (short)ComplexTypeEnum.SignTogether)
            {
                return WfDataManagedResult.Exception(
                    "AddAssignee requires flat node (not SignTogether).");
            }

            if (!string.IsNullOrEmpty(activityInstance.MainActInstanceId)
                && !string.Equals(
                    activityInstance.MainActInstanceId,
                    activityInstance.ActivityInstanceId,
                    StringComparison.OrdinalIgnoreCase))
            {
                var parent = Sail.ManagerFactory.Activities.GetById(kit, activityInstance.MainActInstanceId);
                if (parent != null
                    && (parent.ComplexType == (short)ComplexTypeEnum.SignTogether
                        || parent.ComplexType == (short)ComplexTypeEnum.SignForward
                        || IActivityInstanceManager.IsSignForwardContainer(parent)))
                {
                    return WfDataManagedResult.Exception(
                        "AddAssignee requires flat node (child under SignTogether/SignForward).");
                }
            }

            return null;
        }

        /// <summary>
        /// 减签：取消指定办理人在本活动上的未办待办（Closed + Cancelled 通知），并刷新 SignIds/Names。
        /// 至少保留一名打开待办；不推进下一节点。
        /// </summary>
        public WfDataManagedResult RemoveAssignees(
            SQLBuilder kit,
            string taskId,
            FlowCommandContext runner,
            PerformerList removeTargets)
        {
            if (removeTargets == null || removeTargets.Count == 0)
                return WfDataManagedResult.Exception("RemoveAssignee requires at least one target performer.");

            var task = GetTask(kit, taskId);
            if (task == null)
                return WfDataManagedResult.Exception("RemoveAssignee task not found.");

            var am = Sail.ManagerFactory.Activities;
            var activityInstance = am.GetById(kit, task.ActivityInstanceId);
            if (activityInstance.ActivityState != (short)ActivityStateEnum.Ready
                && activityInstance.ActivityState != (short)ActivityStateEnum.Running)
            {
                return WfDataManagedResult.Exception("RemoveAssignee activity is not ready/running.");
            }

            var removeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in removeTargets)
            {
                if (!string.IsNullOrWhiteSpace(p?.UserId))
                    removeIds.Add(p.UserId);
            }
            if (removeIds.Count == 0)
                return WfDataManagedResult.Exception("RemoveAssignee requires at least one target performer.");

            // 仅减常规办理人；沟通参与者不计名额、不可当减签目标语义主体
            var open = GetOpenApproverTasks(
                kit,
                activityInstance.ProcessInstanceId,
                activityInstance.ActivityInstanceId);

            var toClose = open
                .Where(t => removeIds.Contains(t.SignId))
                .ToList();
            if (toClose.Count == 0)
                return WfDataManagedResult.Exception("RemoveAssignee: no open task matched the targets.");

            if (open.Count - toClose.Count < 1)
                return WfDataManagedResult.Exception("RemoveAssignee cannot remove the last open assignee.");

            foreach (var t in toClose)
            {
                ApplyTaskEnd(t, TaskStateEnum.Closed, runner, TaskEndTypes.Removed);
                Update(kit, t);
                Sail.CapabilityDispatcher.FireTaskLifecycle(
                    kit, t, activityInstance, runner, TaskNotifyKind.Cancelled);
            }

            RefreshActivitySignFromOpenTasks(kit, activityInstance);
            return WfDataManagedResult.Success();
        }

        /// <summary>用 Waiting/Reading 的<strong>常规办理人</strong>待办重建活动 SignIds/Names（排除沟通参与者）。</summary>
        public void RefreshActivitySignFromOpenTasks(
            SQLBuilder kit,
            ActivityInstanceEntity activityInstance)
        {
            var open = GetOpenApproverTasks(
                kit,
                activityInstance.ProcessInstanceId,
                activityInstance.ActivityInstanceId);

            var ids = new StringBuilder(256);
            var names = new StringBuilder(256);
            foreach (var t in open)
            {
                if (ids.Length > 0) ids.Append(',');
                if (names.Length > 0) names.Append(',');
                ids.Append(t.SignId);
                names.Append(t.SignName);
            }
            activityInstance.SignIds = ids.ToString();
            activityInstance.SignNames = names.ToString();
            Sail.ManagerFactory.Activities.Update(kit, activityInstance);
        }

        /// <summary>打开中的常规办理人待办（Waiting/Reading 且非参与者）。</summary>
        public List<TaskEntity> GetOpenApproverTasks(
            SQLBuilder kit,
            string processInstanceId,
            string activityInstanceId) =>
            GetTaskListByActivity(kit, processInstanceId, activityInstanceId)
                .Where(t => (t.TaskState == (short)TaskStateEnum.Waiting
                        || t.TaskState == (short)TaskStateEnum.Reading)
                    && AssigneeRoleHelper.IsApprover(t.AssigneeRole))
                .ToList();

        /// <summary>源审批 Task 下是否仍有打开的沟通参与者待办。</summary>
        public bool HasOpenParticipants(SQLBuilder kit, string consultSrcTaskId)
        {
            if (string.IsNullOrWhiteSpace(consultSrcTaskId))
                return false;
            return GetOpenParticipantsBySrc(kit, consultSrcTaskId).Count > 0;
        }

        /// <summary>按沟通源 Task 取打开的参与者待办。</summary>
        public List<TaskEntity> GetOpenParticipantsBySrc(SQLBuilder kit, string consultSrcTaskId)
        {
            if (string.IsNullOrWhiteSpace(consultSrcTaskId))
                return new List<TaskEntity>();

            var clip = kit.useClip();
            clip.from<TaskEntity>(out var t)
                .where(() => t.ConsultSrcTaskId, consultSrcTaskId)
                .whereIn(() => t.TaskState, new short?[]
                {
                    (short)TaskStateEnum.Waiting,
                    (short)TaskStateEnum.Reading
                });
            return clip.select(t).queryList()
                ?.Where(x => AssigneeRoleHelper.IsParticipant(x.AssigneeRole))
                .ToList()
                ?? new List<TaskEntity>();
        }

        /// <summary>
        /// 沟通：在当前活动为选中人建参与者待办（role=1，ConsultSrcTaskId=源 Task）。
        /// 同轮同人已有打开参与者待办则跳过（幂等）。不刷新 SignIds/Names。
        /// </summary>
        public WfDataManagedResult Consult(
            SQLBuilder kit,
            string srcTaskId,
            FlowCommandContext runner,
            PerformerList targets)
        {
            if (targets == null || targets.Count == 0)
                return WfDataManagedResult.Exception("Consult requires at least one target performer.");

            var srcTask = GetTask(kit, srcTaskId);
            if (srcTask == null)
                return WfDataManagedResult.Exception("Consult source task not found.");
            if (AssigneeRoleHelper.IsParticipant(srcTask.AssigneeRole))
                return WfDataManagedResult.Exception("Consult cannot be started from a participant task.");
            if (!string.Equals(srcTask.SignId, runner?.UserId, StringComparison.OrdinalIgnoreCase))
                return WfDataManagedResult.Exception("Consult requires the current user's to-do.");
            if (srcTask.TaskState != (short)TaskStateEnum.Waiting
                && srcTask.TaskState != (short)TaskStateEnum.Reading)
                return WfDataManagedResult.Exception("Consult task is not ready/running.");

            var am = Sail.ManagerFactory.Activities;
            var activityInstance = am.GetById(kit, srcTask.ActivityInstanceId);
            if (activityInstance.ActivityState != (short)ActivityStateEnum.Ready
                && activityInstance.ActivityState != (short)ActivityStateEnum.Running)
            {
                return WfDataManagedResult.Exception("Consult activity is not ready/running.");
            }

            var existingOpen = GetOpenParticipantsBySrc(kit, srcTaskId);
            var existingUsers = new HashSet<string>(
                existingOpen.Select(t => t.SignId),
                StringComparer.OrdinalIgnoreCase);

            var created = 0;
            foreach (var p in targets)
            {
                if (p == null || string.IsNullOrWhiteSpace(p.UserId))
                    continue;
                if (string.Equals(p.UserId, srcTask.SignId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (existingUsers.Contains(p.UserId))
                    continue;

                var entity = new TaskEntity
                {
                    BizChannel = activityInstance.BizChannel,
                    BizPK = activityInstance.BizPK,
                    ActivityInstanceId = activityInstance.ActivityInstanceId,
                    ProcessInstanceId = activityInstance.ProcessInstanceId,
                    ActivityId = activityInstance.ActivityId,
                    ActivityName = activityInstance.ActivityName,
                    ProcessId = activityInstance.ProcessId,
                    TaskType = (short)TaskTypeEnum.Manual,
                    AssigneeRole = (int)AssigneeRoleEnum.Participant,
                    ConsultSrcTaskId = srcTaskId,
                    SignId = p.UserId,
                    SignName = p.UserName,
                    GroupId = !string.IsNullOrEmpty(p.GroupId) ? p.GroupId : activityInstance.GroupId,
                    GroupName = !string.IsNullOrEmpty(p.GroupName) ? p.GroupName : activityInstance.GroupName,
                    GroupRole = !string.IsNullOrEmpty(p.GroupRole) ? p.GroupRole : activityInstance.GroupRole,
                    UnitId = !string.IsNullOrEmpty(p.UnitId) ? p.UnitId : activityInstance.UnitId,
                    UnitCode = !string.IsNullOrEmpty(p.UnitCode) ? p.UnitCode : activityInstance.UnitCode,
                    UnitName = !string.IsNullOrEmpty(p.UnitName) ? p.UnitName : activityInstance.UnitName,
                    TaskState = (short)TaskStateEnum.Waiting,
                    IsEMailSent = 0,
                    Idx = 0,
                    IsRead = 0,
                    CreatedUserId = runner?.UserId,
                    CreatedUserName = runner?.UserName,
                    CreatedDateTime = DateTime.UtcNow,
                    RecordStatusInvalid = 0
                };
                StampNewTaskClocks(entity, activateNow: true);
                var newId = Insert(kit, entity);
                entity.TaskId = newId;
                Sail.CapabilityDispatcher.FireTaskLifecycle(
                    kit, entity, activityInstance, runner, TaskNotifyKind.Created);
                existingUsers.Add(p.UserId);
                created++;
            }

            if (created == 0 && existingOpen.Count == 0)
                return WfDataManagedResult.Exception("Consult: no valid participant created.");

            return WfDataManagedResult.Success();
        }

        /// <summary>阅办：仅参与者办结自己的待办（Completed）。</summary>
        public WfDataManagedResult Acknowledge(
            SQLBuilder kit,
            string taskId,
            FlowCommandContext runner)
        {
            var task = GetTask(kit, taskId);
            if (task == null)
                return WfDataManagedResult.Exception("Acknowledge task not found.");
            if (!AssigneeRoleHelper.IsParticipant(task.AssigneeRole))
                return WfDataManagedResult.Exception("Acknowledge requires a participant task.");
            if (!string.Equals(task.SignId, runner?.UserId, StringComparison.OrdinalIgnoreCase))
                return WfDataManagedResult.Exception("Acknowledge requires the current user's to-do.");
            if (task.TaskState != (short)TaskStateEnum.Waiting
                && task.TaskState != (short)TaskStateEnum.Reading)
                return WfDataManagedResult.Exception("Acknowledge task is not ready/running.");

            EndTaskState(kit, taskId, TaskStateEnum.Completed, runner);
            FireTaskLifecycle(kit, GetTask(kit, taskId), runner, TaskNotifyKind.Completed);
            return WfDataManagedResult.Success();
        }

        /// <summary>收回沟通：取消源 Task 下仍打开的参与者待办。</summary>
        public WfDataManagedResult RecallConsult(
            SQLBuilder kit,
            string srcTaskId,
            FlowCommandContext runner)
        {
            var srcTask = GetTask(kit, srcTaskId);
            if (srcTask == null)
                return WfDataManagedResult.Exception("RecallConsult source task not found.");
            if (!string.Equals(srcTask.SignId, runner?.UserId, StringComparison.OrdinalIgnoreCase))
                return WfDataManagedResult.Exception("RecallConsult requires the current user's to-do.");

            var open = GetOpenParticipantsBySrc(kit, srcTaskId);
            if (open.Count == 0)
                return WfDataManagedResult.Exception("RecallConsult: no open participants.");

            var am = Sail.ManagerFactory.Activities;
            var activityInstance = am.GetById(kit, srcTask.ActivityInstanceId);
            foreach (var t in open)
            {
                t.TaskState = (short)TaskStateEnum.Canceled;
                t.CompletedAt = DateTime.UtcNow;
                t.EndedUserId = runner?.UserId;
                t.EndedUserName = runner?.UserName;
                Update(kit, t);
                Sail.CapabilityDispatcher.FireTaskLifecycle(
                    kit, t, activityInstance, runner, TaskNotifyKind.Cancelled);
            }

            return WfDataManagedResult.Success();
        }

        /// <summary>节点离开时：取消同活动上未关闭的沟通参与者待办（避免孤儿阅办）。</summary>
        public void CancelOpenParticipantsOnActivity(
            SQLBuilder kit,
            ActivityInstanceEntity activityInstance,
            FlowCommandContext runner)
        {
            if (activityInstance == null)
                return;

            var open = GetTaskListByActivity(
                    kit,
                    activityInstance.ProcessInstanceId,
                    activityInstance.ActivityInstanceId)
                .Where(t => (t.TaskState == (short)TaskStateEnum.Waiting
                        || t.TaskState == (short)TaskStateEnum.Reading)
                    && AssigneeRoleHelper.IsParticipant(t.AssigneeRole))
                .ToList();

            foreach (var t in open)
            {
                ApplyTaskEnd(t, TaskStateEnum.Canceled, runner, TaskEndTypes.ActivityCancelled);
                Update(kit, t);
                Sail.CapabilityDispatcher.FireTaskLifecycle(
                    kit, t, activityInstance, runner, TaskNotifyKind.Cancelled);
            }
        }

        /// <summary>
        /// 节点离开 / 会签阈值已达：关闭同活动剩余打开办理人待办。
        /// 同代理链优先 <see cref="TaskEndTypes.ProxyTaken"/>，否则 <see cref="TaskEndTypes.RaceTaken"/>。
        /// </summary>
        public void CloseOpenApproverTasksOnActivity(
            SQLBuilder kit,
            ActivityInstanceEntity activityInstance,
            FlowCommandContext runner,
            string preferredEndType = null,
            string completedSignId = null,
            string completedProxySignId = null)
        {
            if (activityInstance == null)
                return;

            var open = GetOpenApproverTasks(
                kit,
                activityInstance.ProcessInstanceId,
                activityInstance.ActivityInstanceId);
            if (open.Count == 0)
                return;

            var triggerRoot = string.IsNullOrWhiteSpace(completedProxySignId)
                ? completedSignId
                : completedProxySignId;

            foreach (var t in open)
            {
                var endType = preferredEndType ?? TaskEndTypes.RaceTaken;
                if (!string.IsNullOrWhiteSpace(triggerRoot))
                {
                    var tRoot = string.IsNullOrWhiteSpace(t.ProxySignId) ? t.SignId : t.ProxySignId;
                    var sameChain =
                        string.Equals(tRoot, triggerRoot, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(t.SignId, triggerRoot, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(t.ProxySignId, completedSignId, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(completedProxySignId, t.SignId, StringComparison.OrdinalIgnoreCase);
                    if (sameChain)
                        endType = TaskEndTypes.ProxyTaken;
                }

                ApplyTaskEnd(t, TaskStateEnum.Closed, runner, endType);
                Update(kit, t);
                Sail.CapabilityDispatcher.FireTaskLifecycle(
                    kit, t, activityInstance, runner, TaskNotifyKind.Cancelled);
            }
        }

        /// <summary>
        /// 会签未完成子节点取消时：按子 AI 关掉打开办理人待办（默认 RaceTaken）。
        /// </summary>
        public void CloseOpenApproverTasksOnActivityIds(
            SQLBuilder kit,
            string processInstanceId,
            IEnumerable<string> activityInstanceIds,
            FlowCommandContext runner,
            string endType = null)
        {
            if (string.IsNullOrWhiteSpace(processInstanceId) || activityInstanceIds == null)
                return;

            var ids = activityInstanceIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (ids.Count == 0)
                return;

            var tasks = GetTaskListByActivityIds(kit, processInstanceId, ids);
            var end = string.IsNullOrWhiteSpace(endType) ? TaskEndTypes.RaceTaken : endType;
            foreach (var t in tasks)
            {
                if (t == null)
                    continue;
                if (t.TaskState != (short)TaskStateEnum.Waiting
                    && t.TaskState != (short)TaskStateEnum.Reading)
                    continue;
                if (AssigneeRoleHelper.IsParticipant(t.AssigneeRole))
                    continue;

                ApplyTaskEnd(t, TaskStateEnum.Closed, runner, end);
                Update(kit, t);
                var ai = Sail.ManagerFactory.Activities.GetById(kit, t.ActivityInstanceId);
                Sail.CapabilityDispatcher.FireTaskLifecycle(
                    kit, t, ai, runner, TaskNotifyKind.Cancelled);
            }
        }

        public void CancelEntrustedTask(string taskId)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    CancelEntrustedTask(kit, taskId);
                    kit.commit();
                }
                catch (System.Exception e)
                {
                    throw FailThrow(new WorkflowException(
                        Sail.Messages.GetEngineMessage("taskmanager.entrust.error"), e), "Entrust");
                }
            }
        }

        public void CancelEntrustedTask(SQLBuilder kit, string taskId)
        {
            var clip = kit.useClip();
            clip.from<TaskEntity>(out var t)
                .where(() => t.EntrustedTaskId, taskId);
            var entrustedTaskEntity = clip.select(t).queryList().FirstOrDefault();
            entrustedTaskEntity.TaskState = (short)TaskStateEnum.Canceled;
            entrustedTaskEntity.UpdatedDateTime = DateTime.UtcNow;
            Update(kit, entrustedTaskEntity);
            FireTaskLifecycle(kit, entrustedTaskEntity, null, TaskNotifyKind.Cancelled);

            var originalTask = GetTask(kit, taskId);
            var newTask = new TaskEntity
            {
                BizPK = originalTask.BizPK,
                BizChannel = originalTask.BizChannel,
                ProcessInstanceId = originalTask.ProcessInstanceId,
                ProcessId = originalTask.ProcessId,
                ActivityId = originalTask.ActivityId,
                ActivityName = originalTask.ActivityName,
                ActivityInstanceId = originalTask.ActivityInstanceId,
                SignId = originalTask.SignId,
                SignName = originalTask.SignName,
                GroupId = originalTask.GroupId,
                GroupName = originalTask.GroupName,
                GroupRole = originalTask.GroupRole,
                UnitId = originalTask.UnitId,
                UnitCode = originalTask.UnitCode,
                UnitName = originalTask.UnitName,
                TaskState = (short)TaskStateEnum.Waiting,
                TaskType = originalTask.TaskType,
                SignKind = originalTask.SignKind,
                Idx = originalTask.Idx ?? 0,
                IsRead = 0,
                CreatedDateTime = DateTime.UtcNow,
                CreatedUserId = originalTask.CreatedUserId,
                CreatedUserName = originalTask.CreatedUserName
            };
            StampNewTaskClocks(newTask, activateNow: true);
            Insert(kit, newTask);
            FireTaskLifecycle(kit, newTask, null, TaskNotifyKind.Created);
        }

        public Boolean Replace(string activityInstanceId, List<TaskReplacedEntity> replaced, FlowCommandContext runner)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    Replace(kit, activityInstanceId, replaced, runner);
                    kit.commit();
                    return true;
                }
                catch (System.Exception e)
                {
                    throw FailThrow(new WorkflowException(
                        Sail.Messages.GetEngineMessage("taskmanager.replace.error"), e), "Replace");
                }
            }
        }

        public void Replace(SQLBuilder kit, string activityInstanceId, List<TaskReplacedEntity> replaced, FlowCommandContext runner)
        {
            CancelTask(kit, activityInstanceId);
            var aim = Sail.ManagerFactory.Activities;
            var activityInstance = aim.GetById(kit, activityInstanceId);
            activityInstance.SignIds = GenerateActivitySignIds(replaced);
            activityInstance.SignNames = GenerateActivitySignNames(replaced);
            foreach (var user in replaced)
            {
                Insert(kit, activityInstance, user.ReplacedByUserId, user.ReplacedByUserName, runner.UserId, runner.UserName);
            }
        }

        private string GenerateActivitySignIds(List<TaskReplacedEntity> userList)
        {
            var strBuilder = new StringBuilder(1024);
            foreach (var user in userList)
            {
                if (strBuilder.ToString() != "")
                    strBuilder.Append(",");
                strBuilder.Append(user.ReplacedByUserId);
            }
            return strBuilder.ToString();
        }

        private string GenerateActivitySignNames(List<TaskReplacedEntity> userList)
        {
            var strBuilder = new StringBuilder(1024);
            foreach (var user in userList)
            {
                if (strBuilder.ToString() != "")
                    strBuilder.Append(",");
                strBuilder.Append(user.ReplacedByUserName);
            }
            return strBuilder.ToString();
        }

        private void CancelTask(SQLBuilder kit, string activityInstanceId)
        {
            var clip = kit.useClip();
            clip.setTable<TaskEntity>(out var t)
                .set(() => t.TaskState, (short)48)
                .set(() => t.RecordStatusInvalid, (byte)1)
                .where(() => t.ActivityInstanceId, activityInstanceId)
                .doUpdate();
        }

        public bool Delete(string taskId)
        {
            var kit = Sail.UseRunSQL();
            return Delete(kit, taskId);
        }

        public bool Delete(SQLBuilder kit, string taskId)
        {
            kit.useRepo<TaskEntity, string>().DeleteById(taskId);
            return true;
        }
        #endregion
    }
}
