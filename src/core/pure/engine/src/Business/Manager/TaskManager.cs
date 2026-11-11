
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using saiflow.data;
using saiflow.hub.Localize;
using saiflow.engine.Common;
using saiflow.engine.Hooks;
using saiflow.engine.Entity;
using saiflow.engine.Xpdl;
using saiflow.engine.Result;
using saiflow.engine.Client;
using saiflow.WebUtility;


namespace saiflow.engine.Manager
{
    /// <summary>
    /// Task manager: including tasks and task view objects
    /// 任务管理类：包括任务及任务视图对象
    /// </summary>
    public class TaskManager
    {
        public TaskManager(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }
        
        #region TaskView 任务视图
        /// <summary>
        /// Retrieve task view based on task Id
        /// 根据任务ID获取任务视图
        /// </summary>
        public TaskViewEntity GetTaskView(string taskId)
        {
            return Sail.UseRunSQL().findRowById<TaskViewEntity>(taskId);
        }

        internal TaskViewEntity GetTaskView(SQLBuilder kit, string taskId)
            => kit.findRowById<TaskViewEntity>(taskId);

        /// <summary>
        /// 异步按任务 Id 取任务视图（Clip，可取消）。
        /// </summary>
        internal async Task<TaskViewEntity> GetTaskViewAsync(
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
        internal List<TaskEntity> GetTaskListByActivity(string processInstanceId, string activityInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetTaskListByActivity(kit, processInstanceId, activityInstanceId);
        }

        internal List<TaskEntity> GetTaskListByActivity(SQLBuilder kit, string processInstanceId, string activityInstanceId)
        {
            var clip = kit.useClip();
            clip.from<TaskEntity>(out var t)
                .where(() => t.ActivityInstanceId, activityInstanceId)
                .where(() => t.ProcessInstanceId, processInstanceId);
            return clip.select(t).queryList().ToList();
        }

        /// <summary>
        /// Obtain tasks based on process information
        /// 根据流程信息获取任务
        /// </summary>
        internal TaskEntity GetTaskByActivity(string processInstanceId, string activityInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetTaskByActivity(kit, processInstanceId, activityInstanceId);
        }

        internal TaskEntity GetTaskByActivity(SQLBuilder kit, string processInstanceId, string activityInstanceId)
        {
            var list = GetTaskListByActivity(kit, processInstanceId, activityInstanceId);
            return list.Count > 0 ? list[0] : null;
        }

        /// <summary>
        /// Obtain tasks based on process information
        /// 根据流程信息获取任务
        /// </summary>
        internal TaskViewEntity GetTaskViewByActivity(string processInstanceId, string activityInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetTaskViewByActivity(kit, processInstanceId, activityInstanceId);
        }

        internal TaskViewEntity GetTaskViewByActivity(SQLBuilder kit, string processInstanceId, string activityInstanceId)
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
        internal async Task<TaskViewEntity> GetTaskViewByActivityAsync(
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
        internal TaskViewEntity GetTaskOfMineByActivityInstance(string activityInstanceId, string userId)
        {
            var kit = Sail.UseRunSQL();
            return GetTaskOfMineByActivityInstance(kit, activityInstanceId, userId);
        }

        internal TaskViewEntity GetTaskOfMineByActivityInstance(SQLBuilder kit,
            string activityInstanceId,
            string userId)
        {
            var clip = kit.useClip();
            clip.from<TaskViewEntity>(out var t)
                .where(() => t.ActivityInstanceId, activityInstanceId)
                .where(() => t.AssignedUserId, userId)
                .where(() => t.ProcessState, (short)2)
                .whereIn(() => t.ActivityType, new short?[] { (short)4, (short)5, (short)6 })
                .whereIn(() => t.ActivityState, new short?[] { (short)1, (short)2 })
                .orderByDesc(() => t.ActivatedDateTime)
                .top(1);
            var list = clip.select(t).queryList().ToList();

            return ResolveTaskOfMineByActivityInstance(list, activityInstanceId);
        }

        /// <summary>
        /// 异步获取当前用户在活动实例上的办理任务（可取消）。
        /// </summary>
        internal async Task<TaskViewEntity> GetTaskOfMineByActivityInstanceAsync(
            SQLBuilder kit,
            string activityInstanceId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var clip = kit.useClip();
            clip.from<TaskViewEntity>(out var t)
                .where(() => t.ActivityInstanceId, activityInstanceId)
                .where(() => t.AssignedUserId, userId)
                .where(() => t.ProcessState, (short)2)
                .whereIn(() => t.ActivityType, new short?[] { (short)4, (short)5, (short)6 })
                .whereIn(() => t.ActivityState, new short?[] { (short)1, (short)2 })
                .orderByDesc(() => t.ActivatedDateTime)
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
                throw new WorkflowException(
                    string.Format("{0}，ActivityInstanceId: {1}", message, activityInstanceId.ToString())
                );
            }
            if (list.Count > 1)
            {
                throw new WorkflowException(Sail.Messages.GetEngineMessage("taskmanager.gettaskofmine.toomoretasks.error"));
            }
            return list[0];
        }

        /// <summary>
        /// Based on the application instance and process GUID, process the user Id to obtain the task list
        /// 根据应用实例、流程GUID，办理用户Id获取任务列表
        /// </summary>
        internal TaskViewEntity GetTaskOfMine(string appInstanceId, string processId, string userId)
        {
            var kit = Sail.UseRunSQL();
            return GetTaskOfMine(kit, appInstanceId, processId, userId);
        }

        internal TaskViewEntity GetTaskOfMine(SQLBuilder kit,
            string appInstanceId,
            string processId,
            string userId)
        {
            var clip = kit.useClip();
            clip.from<TaskViewEntity>(out var t)
                .where(() => t.AppInstanceId, appInstanceId)
                .where(() => t.ProcessId, processId)
                .where(() => t.AssignedUserId, userId)
                .where(() => t.ProcessState, (short)2)
                .sinkOR()
                .whereIn(() => t.ActivityType, new short?[] { (short)4, (short)5, (short)6 })
                .where(() => t.WorkItemType, (short)1)
                .rise()
                .whereIn(() => t.ActivityState, new short?[] { (short)1, (short)2 })
                .orderByDesc(() => t.ActivatedDateTime)
                .top(1);
            var list = clip.select(t).queryList().ToList();

            if (list.Count == 0)
            {
                var message = Sail.Messages.GetEngineMessage("taskmanager.gettaskofmine.error");
                throw new WorkflowException(
                    string.Format("{0}，AppInstanceId: {1}", message, appInstanceId.ToString())
                );
            }
            if (list.Count > 1)
            {
                throw new WorkflowException(Sail.Messages.GetEngineMessage("taskmanager.gettaskofmine.toomoretasks.error"));
            }
            return list[0];
        }

        /// <summary>
        /// Get my taskview
        /// 获取任务视图
        /// </summary>
        internal TaskViewEntity GetTaskOfMine(FlowCommandContext runner)
        {
            var kit = Sail.UseRunSQL();
            return GetTaskOfMine(kit, runner);
        }

        internal TaskViewEntity GetTaskOfMine(SQLBuilder kit, FlowCommandContext runner)
        {
            if (runner.TaskId != null)
                return GetTaskView(kit, runner.TaskId);
            return GetTaskOfMine(kit, runner.AppInstanceId, runner.ProcessId, runner.UserId);
        }

        /// <summary>
        /// Determine whether the task belongs to a certain user
        /// 判断任务是否属于某个用户
        /// </summary>
        internal bool IsMine(TaskViewEntity entity, string userId)
        {
            var isMine = false;
            if (entity.AssignedUserId == userId) isMine = true;
            return isMine;
        }

        /// <summary>
        /// Determine if the task is in a running state
        /// 判断任务处于运行状态
        /// </summary>
        internal Boolean CheckTaskStateInRunningState(TaskViewEntity task)
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

        #region TaskManager 任务数据基本操作
        private string Insert(SQLBuilder kit, TaskEntity entity)
        {
            string newTaskId = kit.useRepo<TaskEntity, string>().InsertReturnId(entity);
            entity.TaskId = newTaskId;
            return newTaskId;
        }

        internal void Insert(SQLBuilder kit, ActivityInstanceEntity activityInstance,
            PerformerList performers,
            FlowCommandContext runner)
        {
            foreach (Performer performer in performers)
                Insert(kit, activityInstance, performer, runner);
        }

        internal string Insert(SQLBuilder kit, ActivityInstanceEntity activityInstance,
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
                AppName = activityInstance.AppName,
                AppInstanceId = activityInstance.AppInstanceId,
                ActivityInstanceId = activityInstance.ActivityInstanceId,
                ProcessInstanceId = activityInstance.ProcessInstanceId,
                ActivityId = activityInstance.ActivityId,
                ActivityName = activityInstance.ActivityName,
                ProcessId = activityInstance.ProcessId,
                TaskType = (short)TaskTypeEnum.Manual,
                AssignedUserId = performer?.UserId,
                AssignedUserName = performer?.UserName,
                GroupId = !string.IsNullOrEmpty(performer?.GroupId) ? performer.GroupId : activityInstance.GroupId,
                GroupName = !string.IsNullOrEmpty(performer?.GroupName) ? performer.GroupName : activityInstance.GroupName,
                GroupRole = !string.IsNullOrEmpty(performer?.GroupRole) ? performer.GroupRole : activityInstance.GroupRole,
                UnitId = !string.IsNullOrEmpty(performer?.UnitId) ? performer.UnitId : activityInstance.UnitId,
                UnitCode = !string.IsNullOrEmpty(performer?.UnitCode) ? performer.UnitCode : activityInstance.UnitCode,
                UnitName = !string.IsNullOrEmpty(performer?.UnitName) ? performer.UnitName : activityInstance.UnitName,
                TaskState = 1,
                // 业务邮件标记：未发送（saiflow.biz.query.TaskEMailSentStatusEnum.UnSent）
                IsEMailSent = 0,
                CreatedUserId = runnerId,
                CreatedUserName = runnerName,
                CreatedDateTime = DateTime.UtcNow,
                RecordStatusInvalid = 0
            };
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
        internal void Renew(SQLBuilder kit, ActivityInstanceEntity sourceActivityInstance,
            ActivityInstanceEntity newInstance,
            FlowCommandContext runner)
        {
            var performer = new Performer(sourceActivityInstance.AssignedUserIds,
                sourceActivityInstance.AssignedUserNames)
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
            => kit.useRepo<TaskEntity>().Update(entity);

        internal void SetTaskType(SQLBuilder kit, string taskId, TaskTypeEnum taskType)
        {
            var task = GetTask(kit, taskId);
            task.TaskType = (short)taskType;
            Update(kit, task);
        }

        internal void SetTaskRead(FlowCommandContext taskRunner)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var task = GetTask(kit, taskRunner.TaskId);
                    SetTaskState(kit, task, taskRunner.UserId, taskRunner.UserName, TaskStateEnum.Reading);
                    Sail.ManagerFactory.Activities.Read(kit, task.ActivityInstanceId, taskRunner);
                    kit.commit();
                }
                catch (System.Exception e)
                {
                    throw new WorkflowException(Sail.Messages.GetEngineMessage("taskmanager.settaskread.error", e.Message), e);
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

        internal void EndTaskState(SQLBuilder kit, string taskId,
            TaskStateEnum taskState,
            FlowCommandContext runner)
        {
            var task = GetTask(kit, taskId);
            task.TaskState = (byte)taskState;
            task.EndedDateTime = DateTime.UtcNow;
            task.EndedUserId = runner.UserId;
            task.EndedUserName = runner.UserName;
            Update(kit, task);
        }

        internal void Complete(SQLBuilder kit, string taskId, FlowCommandContext runner)
        {
            EndTaskState(kit, taskId, TaskStateEnum.Completed, runner);
            var task = GetTask(kit, taskId);
            FireTaskLifecycle(kit, task, runner, TaskNotifyKind.Completed);
        }

        internal void Withdraw(SQLBuilder kit, string taskId, FlowCommandContext runner)
        {
            EndTaskState(kit, taskId, TaskStateEnum.Withdrawed, runner);
            FireTaskLifecycle(kit, GetTask(kit, taskId), runner, TaskNotifyKind.Cancelled);
        }

        internal void SendBack(SQLBuilder kit, string taskId, FlowCommandContext runner)
        {
            EndTaskState(kit, taskId, TaskStateEnum.SendBacked, runner);
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

        internal WfDataManagedResult Entrust(TaskEntrustedEntity entity, bool cancelOriginalTask = true)
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
                    throw new WorkflowException(Sail.Messages.GetEngineMessage("taskmanager.entrust.error"), e);
                }
            }
        }

        internal WfDataManagedResult Entrust(SQLBuilder kit, TaskEntrustedEntity entity, bool cancelOriginalTask = true)
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
                task.TaskState = (short)TaskStateEnum.Closed;
                Update(kit, task);
                Sail.CapabilityDispatcher.FireTaskLifecycle(
                    kit, task, activityInstance, null, TaskNotifyKind.Cancelled);
            }

            var taskList = GetTaskListByActivity(kit, activityInstance.ProcessInstanceId, activityInstance.ActivityInstanceId);
            if (taskList.Where(t => t.AssignedUserId == entity.EntrustToUserId).Any())
            {
                return WfDataManagedResult.Exception(Sail.Messages.GetEngineMessage("taskmanager.entrust.notagain.warn"));
            }

            Insert(kit, activityInstance, entity.EntrustToUserId, entity.EntrustToUserName,
                entity.RunnerId, entity.RunnerName, entity.TaskId);
            return WfDataManagedResult.Success();
        }

        /// <summary>
        /// 转办：结束本人待办（Closed，不计入会签完成），在本活动实例为接收人建待办，并刷新 AssignedUser*。
        /// 不推进下一节点。与 <see cref="Entrust"/> 相近，但面向产品 Transfer 命令且强制换写活动办理人集合。
        /// </summary>
        internal WfDataManagedResult Transfer(
            SQLBuilder kit,
            string taskId,
            FlowCommandContext runner,
            PerformerList targets)
        {
            if (targets == null || targets.Count == 0)
                return WfDataManagedResult.Exception("Transfer requires at least one target performer.");

            var task = GetTask(kit, taskId);
            if (task == null)
                return WfDataManagedResult.Exception("Transfer task not found.");

            if (!string.Equals(task.AssignedUserId, runner.UserId, StringComparison.OrdinalIgnoreCase))
                return WfDataManagedResult.Exception("Transfer is only allowed on the current user's task.");

            if (task.TaskState != (short)TaskStateEnum.Waiting
                && task.TaskState != (short)TaskStateEnum.Reading)
                return WfDataManagedResult.Exception("Transfer task is not in to-do state.");

            var am = Sail.ManagerFactory.Activities;
            var activityInstance = am.GetById(kit, task.ActivityInstanceId);
            if (activityInstance.ActivityState != (short)ActivityStateEnum.Ready
                && activityInstance.ActivityState != (short)ActivityStateEnum.Running)
            {
                return WfDataManagedResult.Exception("Transfer activity is not ready/running.");
            }

            // 关闭原人待办：Closed（勿用 Completed，避免被会签计为已办）
            task.TaskState = (short)TaskStateEnum.Closed;
            task.EndedDateTime = DateTime.UtcNow;
            task.EndedUserId = runner.UserId;
            task.EndedUserName = runner.UserName;
            Update(kit, task);
            Sail.CapabilityDispatcher.FireTaskLifecycle(
                kit, task, activityInstance, runner, TaskNotifyKind.Entrusted);

            var existing = GetTaskListByActivity(
                kit,
                activityInstance.ProcessInstanceId,
                activityInstance.ActivityInstanceId);

            foreach (var target in targets)
            {
                if (string.IsNullOrWhiteSpace(target?.UserId))
                    continue;
                if (string.Equals(target.UserId, runner.UserId, StringComparison.OrdinalIgnoreCase))
                    return WfDataManagedResult.Exception("Cannot transfer to self.");

                var hasOpen = existing.Any(t =>
                    string.Equals(t.AssignedUserId, target.UserId, StringComparison.OrdinalIgnoreCase)
                    && (t.TaskState == (short)TaskStateEnum.Waiting
                        || t.TaskState == (short)TaskStateEnum.Reading));
                if (hasOpen)
                    return WfDataManagedResult.Exception(
                        $"Transfer target already has an open task: {target.UserId}");

                Insert(kit, activityInstance, target.UserId, target.UserName,
                    runner.UserId, runner.UserName, taskId, runner);
            }

            // 按仍打开的待办重写活动 AssignedUser*，转出人不再占用会签名额
            RefreshActivityAssignedUsersFromOpenTasks(kit, activityInstance);
            return WfDataManagedResult.Success();
        }

        /// <summary>用 Waiting/Reading 待办重建活动 AssignedUserIds/Names。</summary>
        internal void RefreshActivityAssignedUsersFromOpenTasks(
            SQLBuilder kit,
            ActivityInstanceEntity activityInstance)
        {
            var open = GetTaskListByActivity(
                    kit,
                    activityInstance.ProcessInstanceId,
                    activityInstance.ActivityInstanceId)
                .Where(t => t.TaskState == (short)TaskStateEnum.Waiting
                    || t.TaskState == (short)TaskStateEnum.Reading)
                .ToList();

            var ids = new StringBuilder(256);
            var names = new StringBuilder(256);
            foreach (var t in open)
            {
                if (ids.Length > 0) ids.Append(',');
                if (names.Length > 0) names.Append(',');
                ids.Append(t.AssignedUserId);
                names.Append(t.AssignedUserName);
            }
            activityInstance.AssignedUserIds = ids.ToString();
            activityInstance.AssignedUserNames = names.ToString();
            Sail.ManagerFactory.Activities.Update(kit, activityInstance);
        }

        internal void CancelEntrustedTask(string taskId)
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
                    throw new WorkflowException(Sail.Messages.GetEngineMessage("taskmanager.entrust.error"), e);
                }
            }
        }

        internal void CancelEntrustedTask(SQLBuilder kit, string taskId)
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
                AppInstanceId = originalTask.AppInstanceId,
                AppName = originalTask.AppName,
                ProcessInstanceId = originalTask.ProcessInstanceId,
                ProcessId = originalTask.ProcessId,
                ActivityId = originalTask.ActivityId,
                ActivityName = originalTask.ActivityName,
                ActivityInstanceId = originalTask.ActivityInstanceId,
                AssignedUserId = originalTask.AssignedUserId,
                AssignedUserName = originalTask.AssignedUserName,
                GroupId = originalTask.GroupId,
                GroupName = originalTask.GroupName,
                GroupRole = originalTask.GroupRole,
                UnitId = originalTask.UnitId,
                UnitCode = originalTask.UnitCode,
                UnitName = originalTask.UnitName,
                TaskState = (short)TaskStateEnum.Waiting,
                TaskType = originalTask.TaskType,
                CreatedDateTime = DateTime.UtcNow,
                CreatedUserId = originalTask.CreatedUserId,
                CreatedUserName = originalTask.CreatedUserName
            };
            Insert(kit, newTask);
            FireTaskLifecycle(kit, newTask, null, TaskNotifyKind.Created);
        }

        internal Boolean Replace(string activityInstanceId, List<TaskReplacedEntity> replaced, FlowCommandContext runner)
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
                    throw new WorkflowException(Sail.Messages.GetEngineMessage("taskmanager.replace.error"), e);
                }
            }
        }

        internal void Replace(SQLBuilder kit, string activityInstanceId, List<TaskReplacedEntity> replaced, FlowCommandContext runner)
        {
            CancelTask(kit, activityInstanceId);
            var aim = Sail.ManagerFactory.Activities;
            var activityInstance = aim.GetById(kit, activityInstanceId);
            activityInstance.AssignedUserIds = GenerateActivityAssignedUserIDs(replaced);
            activityInstance.AssignedUserNames = GenerateActivityAssignedUserNames(replaced);
            foreach (var user in replaced)
            {
                Insert(kit, activityInstance, user.ReplacedByUserId, user.ReplacedByUserName, runner.UserId, runner.UserName);
            }
        }

        private string GenerateActivityAssignedUserIDs(List<TaskReplacedEntity> userList)
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

        private string GenerateActivityAssignedUserNames(List<TaskReplacedEntity> userList)
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

        internal bool Delete(string taskId)
        {
            var kit = Sail.UseRunSQL();
            return Delete(kit, taskId);
        }

        internal bool Delete(SQLBuilder kit, string taskId)
        {
            kit.useRepo<TaskEntity, string>().DeleteById(taskId);
            return true;
        }
        #endregion
    }
}
