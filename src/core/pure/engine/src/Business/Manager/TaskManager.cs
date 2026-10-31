
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using mooSQL.data;
using saiflow.data;
using saiflow.hub.Localize;
using saiflow.engine.Common;
using saiflow.engine.Hooks;
using saiflow.engine.Business.Entity;
using saiflow.engine.Xpdl.Common;
using saiflow.engine.Business.Result;
using saiflow.engine.Client;
using saiflow.WebUtility;


namespace saiflow.engine.Business.Manager
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
        private static SQLBuilder Sibling(SQLBuilder kit)
        {
            var next = kit.DBLive.useSQL();
            if (kit.Executor != null)
                next.useTransaction(kit.Executor);
            return next;
        }

        
        #region TaskView 任务视图
        /// <summary>
        /// Retrieve task view based on task Id
        /// 根据任务ID获取任务视图
        /// </summary>
        public TaskViewEntity GetTaskView(int taskId)
        {
            return Sail.UseRunSQL().useRepo<TaskViewEntity, int>().GetById(taskId);
        }

        internal TaskViewEntity GetTaskView(SQLBuilder kit, int taskId)
            => Sibling(kit).useRepo<TaskViewEntity, int>().GetById(taskId);

        /// <summary>
        /// Retrieve task based on task Id
        /// 获取任务
        /// </summary>
        public TaskEntity GetTask(int taskId)
            => Sail.UseRunSQL().useRepo<TaskEntity, int>().GetById(taskId);

        public TaskEntity GetTask(SQLBuilder kit, int taskId)
            => Sibling(kit).useRepo<TaskEntity, int>().GetById(taskId);

        /// <summary>
        /// Obtain tasks based on process information
        /// 根据流程信息获取任务
        /// </summary>
        internal List<TaskEntity> GetTaskListByActivity(int processInstanceId, int activityInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetTaskListByActivity(kit, processInstanceId, activityInstanceId);
        }

        internal List<TaskEntity> GetTaskListByActivity(SQLBuilder kit, int processInstanceId, int activityInstanceId)
        {
            var clip = Sibling(kit).useClip();
            clip.from<TaskEntity>(out var t);
            clip.where(() => t.ActivityInstanceId, activityInstanceId);
            clip.where(() => t.ProcessInstanceId, processInstanceId);
            return clip.select(t).queryList().ToList();
        }

        /// <summary>
        /// Obtain tasks based on process information
        /// 根据流程信息获取任务
        /// </summary>
        internal TaskEntity GetTaskByActivity(int processInstanceId, int activityInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetTaskByActivity(kit, processInstanceId, activityInstanceId);
        }

        internal TaskEntity GetTaskByActivity(SQLBuilder kit, int processInstanceId, int activityInstanceId)
        {
            var list = GetTaskListByActivity(kit, processInstanceId, activityInstanceId);
            return list.Count > 0 ? list[0] : null;
        }

        /// <summary>
        /// Obtain tasks based on process information
        /// 根据流程信息获取任务
        /// </summary>
        internal TaskViewEntity GetTaskViewByActivity(int processInstanceId, int activityInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetTaskViewByActivity(kit, processInstanceId, activityInstanceId);
        }

        internal TaskViewEntity GetTaskViewByActivity(SQLBuilder kit, int processInstanceId, int activityInstanceId)
        {
            var clip = Sibling(kit).useClip();
            clip.from<TaskViewEntity>(out var t);
            clip.where(() => t.ActivityInstanceId, activityInstanceId);
            clip.where(() => t.ProcessInstanceId, processInstanceId);
            var list = clip.select(t).queryList().ToList();
            return list.Count > 0 ? list[0] : null;
        }

        #endregion

        #region Get the current user's processing tasks 获取当前用户的办理任务
        /// <summary>
        /// Get my task
        /// 获取我的任务
        /// </summary>
        internal TaskViewEntity GetTaskOfMineByActivityInstance(int activityInstanceId, string userId)
        {
            var kit = Sail.UseRunSQL();
            return GetTaskOfMineByActivityInstance(kit, activityInstanceId, userId);
        }

        internal TaskViewEntity GetTaskOfMineByActivityInstance(SQLBuilder kit,
            int activityInstanceId,
            string userId)
        {
            var clip = Sibling(kit).useClip();
            clip.from<TaskViewEntity>(out var t);
            clip.where(() => t.ActivityInstanceId, activityInstanceId);
            clip.where(() => t.AssignedUserId, userId);
            clip.where(() => t.ProcessState, (short)2);
            clip.where(() => t.ActivityType == 4 || t.ActivityType == 5 || t.ActivityType == 6);
            clip.whereIn(() => t.ActivityState, new short?[] { (short)1, (short)2 });
            clip.orderByDesc(() => t.Id);
            clip.top(1);
            var list = clip.select(t).queryList().ToList();

            return ResolveTaskOfMineByActivityInstance(list, activityInstanceId);
        }

        private TaskViewEntity ResolveTaskOfMineByActivityInstance(
            List<TaskViewEntity> list, int activityInstanceId)
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
            var clip = Sibling(kit).useClip();
            clip.from<TaskViewEntity>(out var t);
            clip.where(() => t.AppInstanceId, appInstanceId);
            clip.where(() => t.ProcessId, processId);
            clip.where(() => t.AssignedUserId, userId);
            clip.where(() => t.ProcessState, (short)2);
            clip.sinkOR();
            clip.whereIn(() => t.ActivityType, new short?[] { (short)4, (short)5, (short)6 });
            clip.where(() => t.WorkItemType, (short)1);
            clip.rise();
            clip.whereIn(() => t.ActivityState, new short?[] { (short)1, (short)2 });
            clip.orderByDesc(() => t.Id);
            clip.top(1);
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
        internal TaskViewEntity GetTaskOfMine(WfAppRunner runner)
        {
            var kit = Sail.UseRunSQL();
            return GetTaskOfMine(kit, runner);
        }

        internal TaskViewEntity GetTaskOfMine(SQLBuilder kit, WfAppRunner runner)
        {
            if (runner.TaskId != null)
                return GetTaskView(kit, runner.TaskId.Value);
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
        private int Insert(SQLBuilder kit, TaskEntity entity)
        {
            int newTaskId = Sibling(kit).useRepo<TaskEntity, int>().InsertReturnId(entity);
            entity.Id = newTaskId;
            return newTaskId;
        }

        internal void Insert(SQLBuilder kit, ActivityInstanceEntity activityInstance,
            PerformerList performers,
            WfAppRunner runner)
        {
            foreach (Performer performer in performers)
                Insert(kit, activityInstance, performer, runner);
        }

        internal int Insert(SQLBuilder kit, ActivityInstanceEntity activityInstance,
            Performer performer,
            WfAppRunner runner)
        {
            return Insert(kit, activityInstance, performer.UserId, performer.UserName,
                runner.UserId, runner.UserName, null, runner);
        }

        private int Insert(SQLBuilder kit, ActivityInstanceEntity activityInstance,
            string performerId,
            string performerName,
            string runnerId,
            string runnerName,
            int? entrustedTaskId = null,
            WfAppRunner runner = null)
        {
            var entity = new TaskEntity
            {
                AppName = activityInstance.AppName,
                AppInstanceId = activityInstance.AppInstanceId,
                ActivityInstanceId = activityInstance.Id,
                ProcessInstanceId = activityInstance.ProcessInstanceId,
                ActivityId = activityInstance.ActivityId,
                ActivityName = activityInstance.ActivityName,
                ProcessId = activityInstance.ProcessId,
                TaskType = (short)TaskTypeEnum.Manual,
                AssignedUserId = performerId,
                AssignedUserName = performerName,
                TaskState = 1,
                IsEMailSent = 0,
                CreatedUserId = runnerId,
                CreatedUserName = runnerName,
                CreatedDateTime = DateTime.UtcNow,
                RecordStatusInvalid = 0
            };
            if (entrustedTaskId != null)
                entity.EntrustedTaskId = entrustedTaskId.Value;

            int taskId = Insert(kit, entity);
            entity.Id = taskId;
            Sail.CapabilityDispatcher.FireTaskLifecycle(kit, entity, activityInstance, runner, TaskNotifyKind.Created);
            return taskId;
        }

        /// <summary>
        /// Regenerate task (limited to co signing child nodes under multiple instances)
        /// 重新生成任务(只限于会签多实例下的子节点)
        /// </summary>
        internal void Renew(SQLBuilder kit, ActivityInstanceEntity sourceActivityInstance,
            ActivityInstanceEntity newInstance,
            WfAppRunner runner)
        {
            var performer = new Performer(sourceActivityInstance.AssignedUserIds,
                sourceActivityInstance.AssignedUserNames);
            Insert(kit, newInstance, performer, runner);
        }

        public void Update(TaskEntity entity)
        {
            var kit = Sail.UseRunSQL();
            Update(kit, entity);
        }

        public void Update(SQLBuilder kit, TaskEntity entity)
            => Sibling(kit).useRepo<TaskEntity>().Update(entity);

        internal void SetTaskType(SQLBuilder kit, int taskId, TaskTypeEnum taskType)
        {
            var task = GetTask(kit, taskId);
            task.TaskType = (short)taskType;
            Update(kit, task);
        }

        internal void SetTaskRead(WfAppRunner taskRunner)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var task = GetTask(kit, taskRunner.TaskId.Value);
                    SetTaskState(kit, task, taskRunner.UserId, taskRunner.UserName, TaskStateEnum.Reading);
                    new ActivityInstanceManager(Sail).Read(kit, task.ActivityInstanceId.Value, taskRunner);
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

        internal void EndTaskState(SQLBuilder kit, long taskId,
            TaskStateEnum taskState,
            WfAppRunner runner)
        {
            var task = GetTask(kit, (int)taskId);
            task.TaskState = (byte)taskState;
            task.EndedDateTime = DateTime.UtcNow;
            task.EndedUserId = runner.UserId;
            task.EndedUserName = runner.UserName;
            Update(kit, task);
        }

        internal void Complete(SQLBuilder kit, int taskId, WfAppRunner runner)
        {
            EndTaskState(kit, taskId, TaskStateEnum.Completed, runner);
            var task = GetTask(kit, taskId);
            var activityInstance = new ActivityInstanceManager(Sail).GetById(kit, task.ActivityInstanceId.Value);
            Sail.CapabilityDispatcher.FireTaskLifecycle(kit, task, activityInstance, runner, TaskNotifyKind.Completed);
        }

        internal void Withdraw(SQLBuilder kit, long taskId, WfAppRunner runner)
            => EndTaskState(kit, taskId, TaskStateEnum.Withdrawed, runner);

        internal void SendBack(SQLBuilder kit, long taskId, WfAppRunner runner)
            => EndTaskState(kit, taskId, TaskStateEnum.SendBacked, runner);

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
            var am = new ActivityInstanceManager(Sail);
            var activityInstance = am.GetByTask(kit, entity.TaskId.Value);

            if (activityInstance.ActivityState != (short)ActivityStateEnum.Ready
                && activityInstance.ActivityState != (short)ActivityStateEnum.Running)
            {
                return WfDataManagedResult.Exception(Sail.Messages.GetEngineMessage("taskmanager.entrust.warn"));
            }

            if (cancelOriginalTask)
            {
                var task = GetTask(kit, entity.TaskId.Value);
                task.TaskState = (short)TaskStateEnum.Closed;
                Update(kit, task);
            }

            var taskList = GetTaskListByActivity(kit, activityInstance.ProcessInstanceId.Value, activityInstance.Id.Value);
            if (taskList.Where(t => t.AssignedUserId == entity.EntrustToUserId).Any())
            {
                return WfDataManagedResult.Exception(Sail.Messages.GetEngineMessage("taskmanager.entrust.notagain.warn"));
            }

            Insert(kit, activityInstance, entity.EntrustToUserId, entity.EntrustToUserName,
                entity.RunnerId, entity.RunnerName, entity.TaskId);
            return WfDataManagedResult.Success();
        }

        internal void CancelEntrustedTask(int taskId)
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

        internal void CancelEntrustedTask(SQLBuilder kit, int taskId)
        {
            var clip = Sibling(kit).useClip();
            clip.from<TaskEntity>(out var t);
            clip.where(() => t.EntrustedTaskId, taskId);
            var entrustedTaskEntity = clip.select(t).queryList().FirstOrDefault();
            entrustedTaskEntity.TaskState = (short)TaskStateEnum.Canceled;
            entrustedTaskEntity.UpdatedDateTime = DateTime.UtcNow;
            Update(kit, entrustedTaskEntity);

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
                TaskState = (short)TaskStateEnum.Waiting,
                TaskType = originalTask.TaskType,
                CreatedDateTime = DateTime.UtcNow,
                CreatedUserId = originalTask.CreatedUserId,
                CreatedUserName = originalTask.CreatedUserName
            };
            Insert(kit, newTask);
        }

        internal Boolean Replace(int activityInstanceId, List<TaskReplacedEntity> replaced, WfAppRunner runner)
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

        internal void Replace(SQLBuilder kit, int activityInstanceId, List<TaskReplacedEntity> replaced, WfAppRunner runner)
        {
            CancelTask(kit, activityInstanceId);
            var aim = new ActivityInstanceManager(Sail);
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

        private void CancelTask(SQLBuilder kit, int activityInstanceId)
        {
            var clip = Sibling(kit).useClip();
            clip.setTable<TaskEntity>(out var t);
            clip.set(() => t.TaskState, (short)48);
            clip.set(() => t.RecordStatusInvalid, (byte)1);
            clip.where(() => t.ActivityInstanceId, activityInstanceId);
            clip.doUpdate();
        }

        internal bool Delete(int taskId)
        {
            var kit = Sail.UseRunSQL();
            return Delete(kit, taskId);
        }

        internal bool Delete(SQLBuilder kit, long taskId)
        {
            Sibling(kit).useRepo<TaskEntity, int>().DeleteById((int)taskId);
            return true;
        }
        #endregion
    }
}
