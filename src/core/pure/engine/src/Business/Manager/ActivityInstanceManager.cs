
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using saiflow.data;
using saiflow.engine.Assignee;
using saiflow.engine.Entity;
using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.engine.Runtime;
using saiflow.engine.Xpdl;
using saiflow.hub.Localize;

namespace saiflow.engine.Manager
{
    /// <summary>
    /// Activity Instance Manager
    /// 活动实例管理类
    /// </summary>
    public class ActivityInstanceManager
    {
        public ActivityInstanceManager(SailClient client)
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

        #region Activity instance data acquisition 活动实例数据获取
        /// <summary>
        /// Retrieve activity instances based on Id
        /// 根据ID获取活动实例
        /// </summary>
        public ActivityInstanceEntity GetById(int activityInstanceId)
        {
            try
            {
                return Sail.UseRunSQL().useRepo<ActivityInstanceEntity, int>().GetById(activityInstanceId);
            }
            catch (System.Exception e)
            {
                throw new ApplicationException(Sail.Messages.GetEngineMessage("activityinstancemanager.getbyid.error", e.Message),
                    e);
            }
        }

        internal ActivityInstanceEntity GetById(SQLBuilder kit, int activityInstanceId)
            => Sibling(kit).useRepo<ActivityInstanceEntity, int>().GetById(activityInstanceId);

        /// <summary>
        /// 异步按 Id 取活动实例（Clip take(1)，可取消）。与同步 Repo.GetById 双轨。
        /// </summary>
        internal async Task<ActivityInstanceEntity> GetByIdAsync(
            SQLBuilder kit,
            int activityInstanceId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var clip = Sibling(kit).useClip();
            clip.from<ActivityInstanceEntity>(out var ai);
            clip.where(() => ai.Id, activityInstanceId);
            var list = await clip.select(ai).take(1).queryListAsync(cancellationToken).ConfigureAwait(false);
            return list?.FirstOrDefault();
        }

        /// <summary>
        /// Obtain the current running node information of the process
        /// 获取流程当前运行节点信息
        /// </summary>
        internal ActivityInstanceEntity GetRunningNode(WfAppRunner runner)
        {
            var kit = Sail.UseRunSQL();
            return GetRunningNode(kit, runner, out _);
        }

        /// <summary>
        /// Obtain the current running node information of the process
        /// 获取流程当前运行节点信息
        /// </summary>
        internal ActivityInstanceEntity GetRunningNode(WfAppRunner runner,
            out TaskViewEntity taskView)
        {
            var kit = Sail.UseRunSQL();
            return GetRunningNode(kit, runner, out taskView);
        }

        /// <summary>
        /// Obtain the current running node information of the process
        /// 获取流程当前运行节点信息
        /// </summary>
        internal ActivityInstanceEntity GetRunningNode(SQLBuilder kit,
            WfAppRunner runner,
            out TaskViewEntity taskView)
        {
            var taskId = runner.TaskId;
            taskView = null;

            var tm = Sail.ManagerFactory.Tasks;
            ActivityInstanceEntity runningNode = null;
            if (taskId != null && taskId.Value > 0)
            {
                taskView = tm.GetTaskView(kit, taskId.Value);
                runningNode = GetById(kit, taskView.ActivityInstanceId.Value);
                return runningNode;
            }

            var activityInstanceList = GetRunningActivityInstanceList(kit, runner.AppInstanceId, runner.ProcessId, runner.Version).ToList();
            if (activityInstanceList == null || activityInstanceList.Count == 0)
            {
                var message = Sail.Messages.GetEngineMessage("activityinstancemanager.getrunningnode.error");
                var e = new WorkflowException(message);
                LogManager.RecordLog(Sail, message, LogEventType.Exception, LogPriority.Normal, null, e);
                throw e;
            }

            if (activityInstanceList.Count == 1)
            {
                runningNode = activityInstanceList[0];
                taskView = tm.GetTaskOfMineByActivityInstance(kit, runningNode.Id.Value, runner.UserId);
            }
            else if (activityInstanceList.Count > 1)
            {
                foreach (var ai in activityInstanceList)
                {
                    if (ai.AssignedUserIds == runner.UserId)
                    {
                        runningNode = ai;
                        break;
                    }
                }

                if (runningNode != null)
                {
                    taskView = tm.GetTaskOfMineByActivityInstance(kit, runningNode.Id.Value, runner.UserId);
                }
                else
                {
                    var msgException = Sail.Messages.GetEngineMessage("activityinstancemanager.getrunningnode.unique.error");
                    var e = new WorkflowException(msgException);
                    LogManager.RecordLog(Sail, Sail.Messages.GetEngineMessage("activityinstancemanager.getrunningnode.error"),
                        LogEventType.Exception, LogPriority.Normal, null, e);
                    throw e;
                }
            }
            return runningNode;
        }

        /// <summary>
        /// 异步获取当前运行节点（可取消）。返回 (Node, TaskView)；语义对齐同步 <see cref="GetRunningNode(SQLBuilder, WfAppRunner, out TaskViewEntity)"/>。
        /// </summary>
        internal async Task<(ActivityInstanceEntity Node, TaskViewEntity TaskView)> GetRunningNodeAsync(
            SQLBuilder kit,
            WfAppRunner runner,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tm = Sail.ManagerFactory.Tasks;
            TaskViewEntity taskView = null;
            ActivityInstanceEntity runningNode = null;

            var taskId = runner.TaskId;
            if (taskId != null && taskId.Value > 0)
            {
                taskView = await tm.GetTaskViewAsync(kit, taskId.Value, cancellationToken).ConfigureAwait(false);
                runningNode = await GetByIdAsync(kit, taskView.ActivityInstanceId.Value, cancellationToken)
                    .ConfigureAwait(false);
                return (runningNode, taskView);
            }

            var activityInstanceList = (await GetRunningActivityInstanceListAsync(
                    kit, runner.AppInstanceId, runner.ProcessId, runner.Version, cancellationToken)
                .ConfigureAwait(false)).ToList();

            if (activityInstanceList == null || activityInstanceList.Count == 0)
            {
                var message = Sail.Messages.GetEngineMessage("activityinstancemanager.getrunningnode.error");
                var e = new WorkflowException(message);
                LogManager.RecordLog(Sail, message, LogEventType.Exception, LogPriority.Normal, null, e);
                throw e;
            }

            if (activityInstanceList.Count == 1)
            {
                runningNode = activityInstanceList[0];
                taskView = await tm.GetTaskOfMineByActivityInstanceAsync(
                        kit, runningNode.Id.Value, runner.UserId, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                foreach (var ai in activityInstanceList)
                {
                    if (ai.AssignedUserIds == runner.UserId)
                    {
                        runningNode = ai;
                        break;
                    }
                }

                if (runningNode != null)
                {
                    taskView = await tm.GetTaskOfMineByActivityInstanceAsync(
                            kit, runningNode.Id.Value, runner.UserId, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    var msgException = Sail.Messages.GetEngineMessage("activityinstancemanager.getrunningnode.unique.error");
                    var e = new WorkflowException(msgException);
                    LogManager.RecordLog(Sail, Sail.Messages.GetEngineMessage("activityinstancemanager.getrunningnode.error"),
                        LogEventType.Exception, LogPriority.Normal, null, e);
                    throw e;
                }
            }

            return (runningNode, taskView);
        }

        /// <summary>
        /// Get the instance of the return source activity
        /// 获取退回源活动实例
        /// </summary>
        internal ActivityInstanceEntity GetBackSrcActivityInstance(int currentActivityInstanceId)
        {
            var currentActivityInstance = GetById(currentActivityInstanceId);
            var backSrcActivityInstance = GetById(currentActivityInstance.BackSrcActivityInstanceId.Value);

            return backSrcActivityInstance;
        }

        /// <summary>
        /// Determine whether it is a processing task for a certain user
        /// 判断是否是某个用户的办理任务
        /// </summary>
        internal bool IsMineTask(ActivityInstanceEntity entity,
            string userId)
        {
            bool isMine = entity.AssignedUserIds.Contains(userId.ToString());
            return isMine;
        }

        /// <summary>
        /// Determine whether the node is in a running state
        /// 判断节点是否处于运行状态
        /// </summary>
        internal Boolean IncludeRunningState(ActivityInstanceEntity activityInstance)
        {
            var isIncluded = false;
            if (activityInstance.ActivityState == (short)ActivityStateEnum.Ready
                || activityInstance.ActivityState == (short)ActivityStateEnum.Running
                || activityInstance.ActivityState == (short)ActivityStateEnum.Suspended)
            {
                isIncluded = true;
            }
            return isIncluded;
        }

        /// <summary>
        /// Get Activity Instance
        /// 获取活动节点实例
        /// </summary>
        internal IList<ActivityInstanceEntity> GetActivityInstanceList(SQLBuilder kit, int processInstanceId)
        {
            var clip = Sibling(kit).useClip();
            clip.from<ActivityInstanceEntity>(out var ai);
            clip.where(() => ai.ProcessInstanceId, processInstanceId);
            clip.orderBy(() => ai.Id);
            return clip.select(ai).queryList().ToList();
        }

        /// <summary>
        /// Get Activity Instance Latest
        /// 获取活动节点实例
        /// </summary>
        internal ActivityInstanceEntity GetActivityInstanceLatest(int processInstanceId,
            string activityId)
        {
            var kit = Sail.UseRunSQL();
            return GetActivityInstanceLatest(kit, processInstanceId, activityId);
        }

        /// <summary>
        /// Get Activity Instance Latest
        /// 获取最近的节点实例
        /// </summary>
        internal ActivityInstanceEntity GetActivityInstanceLatest(SQLBuilder kit, int processInstanceId,
            string activityId)
        {
            var clip = Sibling(kit).useClip();
            clip.from<ActivityInstanceEntity>(out var ai);
            clip.where(() => ai.ProcessInstanceId, processInstanceId);
            clip.where(() => ai.ActivityId, activityId);
            clip.orderByDesc(() => ai.Id);
            return clip.select(ai).take(1).queryList()?.FirstOrDefault();
        }

        /// <summary>
        /// Get Activity Instance Latest
        /// 获取最近的节点实例
        /// </summary>
        internal ActivityInstanceEntity GetActivityInstanceLatest(string appInstanceId,
            string processId,
            string activityId)
        {
            var kit = Sail.UseRunSQL();
            return GetActivityInstanceLatest(kit, appInstanceId, processId, activityId);
        }

        internal ActivityInstanceEntity GetActivityInstanceLatest(SQLBuilder kit, string appInstanceId,
            string processId,
            string activityId)
        {
            var clip = Sibling(kit).useClip();
            clip.from<ActivityInstanceEntity>(out var ai);
            clip.InnerJoin<ProcessInstanceEntity>(out var pi).on(() => ai.ProcessInstanceId == pi.Id);
            clip.where(() => pi.ProcessState, (short)ProcessStateEnum.Running);
            clip.where(() => ai.AppInstanceId, appInstanceId);
            clip.where(() => ai.ProcessId, processId);
            clip.where(() => ai.ActivityId, activityId);
            var list = clip.select(ai).queryList().ToList();

            if (list.Count > 0)
            {
                var matchedActivityInstance = list[0];
                return GetById(kit, matchedActivityInstance.Id.Value);
            }

            return null;
        }

        /// <summary>
        /// Get Activity Instance List Latest
        /// 获取最近的节点实例
        /// </summary>
        internal List<ActivityInstanceEntity> GetActivityInstanceList(string processId,
            string version,
            string activityId)
        {
            var kit = Sail.UseRunSQL();
            var clip = Sibling(kit).useClip();
            clip.from<ActivityInstanceEntity>(out var ai);
            clip.InnerJoin<ProcessInstanceEntity>(out var pi).on(() => ai.ProcessInstanceId == pi.Id);
            clip.where(() => pi.ProcessState, (short)ProcessStateEnum.Running);
            clip.where(() => pi.ProcessId, processId);
            clip.where(() => pi.Version, version);
            clip.where(() => ai.ActivityId, activityId);
            clip.where(() => ai.ActivityState == (short)ActivityStateEnum.Ready
                || ai.ActivityState == (short)ActivityStateEnum.Running);
            return clip.select(ai).queryList().ToList();
        }

        /// <summary>
        /// Get activity instances with running status
        /// 获取运行状态的活动实例
        /// </summary>
        internal ActivityInstanceEntity GetActivityRunning(SQLBuilder kit, int processInstanceId,
            string activityId)
        {
            return GetActivityByState(kit, processInstanceId, activityId, ActivityStateEnum.Running);
        }

        /// <summary>
        /// Obtain activity instances based on states
        /// 根据状态获取活动实例
        /// </summary>
        internal ActivityInstanceEntity GetActivityByState(SQLBuilder kit, int processInstanceId,
            string activityId,
            ActivityStateEnum activityState)
        {
            ActivityInstanceEntity entity = null;
            var clip = Sibling(kit).useClip();
            clip.from<ActivityInstanceEntity>(out var ai);
            clip.where(() => ai.ProcessInstanceId, processInstanceId);
            clip.where(() => ai.ActivityId, activityId);
            clip.where(() => ai.ActivityState, (short)activityState);
            clip.orderByDesc(() => ai.Id);
            var list = clip.select(ai).queryList().ToList();
            if (list.Count == 1)
            {
                entity = list[0];
            }
            return entity;
        }

        /// <summary>
        /// Retrieve nodes that have already completed running
        /// 获取已经运行完成的节点
        /// </summary>
        public List<ActivityInstanceEntity> GetActivityInstanceListCompleted(string appInstanceId,
            string processId)
        {
            var kit = Sail.UseRunSQL();
            var clip = Sibling(kit).useClip();
            clip.from<ActivityInstanceEntity>(out var ai);
            clip.InnerJoin<ProcessInstanceEntity>(out var pi).on(() => ai.ProcessInstanceId == pi.Id);
            clip.where(() => pi.ProcessState, (short)ProcessStateEnum.Running);
            clip.where(() => ai.AppInstanceId, appInstanceId);
            clip.where(() => ai.ProcessId, processId);
            clip.where(() => ai.ActivityState, (short)ActivityStateEnum.Completed);
            return clip.select(ai).queryList().ToList();
        }

        /// <summary>
        /// Obtain instances of activities that have already been completed in the previous step
        /// 获取上一步已经完成活动的实例
        /// </summary>
        internal ActivityInstanceEntity GetPreviousActivityInstanceSimple(ActivityInstanceEntity runningNode,
            string previousActivityId)
        {
            var kit = Sail.UseRunSQL();
            return GetPreviousActivityInstanceSimple(kit, runningNode, previousActivityId);
        }

        /// <summary>
        /// Obtain instances of activities that have already been completed in the previous step
        /// 获取上一步已经完成活动的实例
        /// </summary>
        internal ActivityInstanceEntity GetPreviousActivityInstanceSimple(SQLBuilder kit, ActivityInstanceEntity runningNode,
            string previousActivityId)
        {
            ActivityInstanceEntity originalRunningNode = null;
            var processInstanceId = runningNode.ProcessInstanceId;

            if (runningNode.BackSrcActivityInstanceId != null)
            {
                originalRunningNode = GetById(kit, runningNode.BackOrgActivityInstanceId.Value);
            }
            else
            {
                originalRunningNode = runningNode;
            }

            var instanceList = GetActivityInstanceListCompletedSimple(kit, processInstanceId.Value, previousActivityId);

            var withoutBackSrcInfoList = instanceList.Where(a => a.BackSrcActivityInstanceId == null);

            var previousList = withoutBackSrcInfoList.Where(a => a.EndedUserId == originalRunningNode.CreatedUserId).ToList();
            if (previousList.Count > 0)
            {
                return previousList[0];
            }
            else if (withoutBackSrcInfoList.Count() == 1)
            {
                return withoutBackSrcInfoList.ToList()[0];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Obtain activity instances with completion status
        /// 获取完成状态的活动实例
        /// </summary>
        internal IList<ActivityInstanceEntity> GetActivityInstanceListCompletedSimple(int processInstanceId,
            string activityId)
        {
            var kit = Sail.UseRunSQL();
            return GetActivityInstanceListCompletedSimple(kit, processInstanceId, activityId);
        }

        /// <summary>
        /// Obtain activity instances with completion status
        /// 获取完成状态的活动实例
        /// </summary>
        internal IList<ActivityInstanceEntity> GetActivityInstanceListCompletedSimple(SQLBuilder kit, int processInstanceId,
            string activityId)
        {
            var clip = Sibling(kit).useClip();
            clip.from<ActivityInstanceEntity>(out var ai);
            clip.where(() => ai.ProcessInstanceId, processInstanceId);
            clip.where(() => ai.ActivityId, activityId);
            clip.where(() => ai.ActivityState, (short)ActivityStateEnum.Completed);
            clip.orderByDesc(() => ai.Id);
            return clip.select(ai).queryList().ToList();
        }

        /// <summary>
        /// Retrieve activity instance information from task Id
        /// 由任务ID获取活动实例信息
        /// </summary>
        internal ActivityInstanceEntity GetByTask(int taskId)
        {
            var kit = Sail.UseRunSQL();
            return GetByTask(kit, taskId);
        }

        /// <summary>
        /// Retrieve activity instance information from task Id
        /// 由任务ID获取活动实例信息
        /// </summary>
        internal ActivityInstanceEntity GetByTask(SQLBuilder kit, int taskId)
        {
            ActivityInstanceEntity entity = null;
            var clip = Sibling(kit).useClip();
            clip.from<ActivityInstanceEntity>(out var ai);
            clip.InnerJoin<TaskEntity>(out var t).on(() => ai.Id == t.ActivityInstanceId);
            clip.where(() => t.Id, taskId);
            var list = clip.select(ai).queryList().ToList();
            if (list.Count == 1)
            {
                entity = list[0];
            }
            return entity;
        }

        /// <summary>
        /// Get the activity nodes running in the process instance
        /// 获取流程实例中运行的活动节点
        /// </summary>
        public IEnumerable<ActivityInstanceEntity> GetRunningActivityInstanceList(string appInstanceId,
            string processId,
            string version = null)
        {
            var kit = Sail.UseRunSQL();
            return GetRunningActivityInstanceList(kit, appInstanceId, processId, version);
        }

        /// <summary>
        /// Get the activity nodes running in the process instance
        /// 获取流程实例中运行的活动节点
        /// </summary>
        internal IEnumerable<ActivityInstanceEntity> GetRunningActivityInstanceList(SQLBuilder kit, string appInstanceId,
            string processId,
            string version)
        {
            if (string.IsNullOrEmpty(version)) version = "1";
            var clip = Sibling(kit).useClip();
            clip.from<ActivityInstanceEntity>(out var ai);
            clip.InnerJoin<ProcessInstanceEntity>(out var pi).on(() => ai.ProcessInstanceId == pi.Id);
            clip.where(() => ai.ActivityState == (short)ActivityStateEnum.Ready
                || ai.ActivityState == (short)ActivityStateEnum.Running);
            clip.where(() => pi.ProcessState, (short)ProcessStateEnum.Running);
            clip.where(() => pi.Version, version);
            clip.where(() => ai.AppInstanceId, appInstanceId);
            clip.where(() => ai.ProcessId, processId);
            return clip.select(ai).queryList();
        }

        /// <summary>
        /// 异步获取运行中活动实例列表（可取消）。
        /// </summary>
        internal async Task<IEnumerable<ActivityInstanceEntity>> GetRunningActivityInstanceListAsync(
            SQLBuilder kit,
            string appInstanceId,
            string processId,
            string version,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(version)) version = "1";
            var clip = Sibling(kit).useClip();
            clip.from<ActivityInstanceEntity>(out var ai);
            clip.InnerJoin<ProcessInstanceEntity>(out var pi).on(() => ai.ProcessInstanceId == pi.Id);
            clip.where(() => ai.ActivityState == (short)ActivityStateEnum.Ready
                || ai.ActivityState == (short)ActivityStateEnum.Running);
            clip.where(() => pi.ProcessState, (short)ProcessStateEnum.Running);
            clip.where(() => pi.Version, version);
            clip.where(() => ai.AppInstanceId, appInstanceId);
            clip.where(() => ai.ProcessId, processId);
            return await clip.select(ai).queryListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieve the list of completed child nodes under the main node
        /// 获取主节点下已经完成的子节点列表
        /// </summary>
        internal List<ActivityInstanceEntity> GetMultipleInstanceListCompleted(int mainActivityInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetMultipleInstanceListCompleted(kit, mainActivityInstanceId);
        }

        /// <summary>
        /// Retrieve the list of completed child nodes under the main node
        /// 获取主节点下已经完成的子节点列表
        /// </summary>
        internal List<ActivityInstanceEntity> GetMultipleInstanceListCompleted(SQLBuilder kit, int mainActivityInstanceId)
        {
            var clip = Sibling(kit).useClip();
            clip.from<ActivityInstanceEntity>(out var ai);
            clip.InnerJoin<ProcessInstanceEntity>(out var pi).on(() => ai.ProcessInstanceId == pi.Id);
            clip.where(() => pi.ProcessState, (short)ProcessStateEnum.Running);
            clip.where(() => ai.MainActivityInstanceId, mainActivityInstanceId);
            clip.where(() => ai.ActivityState, (short)ActivityStateEnum.Completed);
            return clip.select(ai).queryList().ToList();
        }

        /// <summary>
        /// Retrieve completed multi instance nodes
        /// 获取已经完成的多实例子节点
        /// </summary>
        internal List<ActivityInstanceEntity> GetPreviousParallelMultipleInstanceListCompleted(ActivityInstanceEntity runningNode,
            string previousActivityId)
        {
            var kit = Sail.UseRunSQL();
            return GetPreviousParallelMultipleInstanceListCompleted(kit, runningNode, previousActivityId);
        }

        /// <summary>
        /// Retrieve completed multi instance nodes
        /// 获取已经完成的多实例子节点
        /// </summary>
        internal List<ActivityInstanceEntity> GetPreviousParallelMultipleInstanceListCompleted(SQLBuilder kit, ActivityInstanceEntity runningNode,
            string previousActivityId)
        {
            ActivityInstanceEntity originalRunningNode = null;
            if (runningNode.BackSrcActivityInstanceId != null)
            {
                originalRunningNode = GetById(kit, runningNode.BackOrgActivityInstanceId.Value);
            }
            else
            {
                originalRunningNode = runningNode;
            }

            var clip = Sibling(kit).useClip();
            clip.from<ActivityInstanceEntity>(out var ai);
            clip.where(() => ai.ProcessInstanceId, runningNode.ProcessInstanceId);
            clip.where(() => ai.ActivityId, previousActivityId);
            clip.where(() => ai.ActivityState, (short)ActivityStateEnum.Completed);
            clip.orderByDesc(() => ai.Id);
            var instanceList = clip.select(ai).queryList().ToList();

            var withoutBackSrcInfoList = instanceList.Where(a => a.BackSrcActivityInstanceId == null).ToList();

            var firstMIChild = withoutBackSrcInfoList.First(a => a.EndedUserId == originalRunningNode.CreatedUserId);

            return GetMultipleInstanceListCompleted(kit, firstMIChild.MainActivityInstanceId.Value);
        }

        /// <summary>
        /// Determine whether the user is the assigned task user
        /// 判断用户是否是分配下来任务的用户
        /// </summary>
        private bool IsAssignedUserInActivityInstance(ActivityInstanceEntity entity,
            int userId)
        {
            var assignedUserIDs = entity.AssignedUserIds;
            var userList = assignedUserIDs.Split(',');
            var single = userList.FirstOrDefault<string>(a => a == userId.ToString());
            if (!string.IsNullOrEmpty(single))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Determine whether it is a signing together instance node
        /// 判断是否为会签实例节点 
        /// </summary>
        public Boolean IsMultipleInstanceChildren(ActivityInstanceEntity child)
        {
            return child.MainActivityInstanceId != null;
        }

        /// <summary>
        /// Filter activity instance list
        /// 过滤活动实例列表
        /// </summary>
        private List<ActivityInstanceEntity> FilteredActivityInstanceInTheSameBatch(List<ActivityInstanceEntity> instanceList)
        {
            var maxBackSrcActivityInstanceId = instanceList.Max<ActivityInstanceEntity>(a => a.BackSrcActivityInstanceId);
            if (maxBackSrcActivityInstanceId != null)
            {
                var childrenList = instanceList.Where<ActivityInstanceEntity>(a => a.BackSrcActivityInstanceId == maxBackSrcActivityInstanceId.Value).ToList();
                return childrenList;
            }
            else
            {
                return instanceList;
            }
        }

        /// <summary>
        /// Obtain multi instance nodes of signing together nodes based on activity status
        /// 根据活动状态获取会签节点的多实例节点
        /// </summary>
        public List<ActivityInstanceEntity> GetActivityMulitipleInstanceWithState(int mainActivityInstanceId,
            int processInstanceId,
            short? activityState = null)
        {
            var kit = Sail.UseRunSQL();
            return GetActivityMulitipleInstanceWithState(kit, mainActivityInstanceId, processInstanceId, activityState);
        }

        /// <summary>
        /// Retrieve the list of child nodes under the same batch of master nodes
        /// Including: filtering of rollback identification types that need to be filtered back and forth
        /// 获取同一批的主节点下的子节点列表记录
        /// 包括：需要过滤回退后的回退标识类型的过滤
        /// </summary>
        internal IList<ActivityInstanceEntity> GetActivityMulitipleInstanceWithStateBatch(SQLBuilder kit, int processInstanceId,
            int mainActivityInstanceId,
            short? activityState)
        {
            var instanceList = GetActivityMulitipleInstanceWithState(kit,
                mainActivityInstanceId,
                processInstanceId,
                activityState).ToList<ActivityInstanceEntity>();

            instanceList = FilteredActivityInstanceInTheSameBatch(instanceList);
            return instanceList;
        }

        /// <summary>
        /// Obtain multi instance nodes of signing together nodes based on activity status
        /// 根据活动状态获取会签节点的多实例节点
        /// </summary>
        internal List<ActivityInstanceEntity> GetActivityMulitipleInstanceWithState(SQLBuilder kit, int mainActivityInstanceId,
            int processInstanceId,
            short? activityState)
        {
            var clip = Sibling(kit).useClip();
            clip.from<ActivityInstanceEntity>(out var ai);
            clip.where(() => ai.MainActivityInstanceId, mainActivityInstanceId);
            clip.where(() => ai.ProcessInstanceId, processInstanceId);
            if (activityState.HasValue)
            {
                clip.where(() => ai.ActivityState, activityState.Value);
            }
            clip.orderBy(() => ai.CompleteOrder);
            return clip.select(ai).queryList().ToList();
        }

        /// <summary>
        /// Query the number of branch instances
        /// 查询分支实例的个数
        /// </summary>
        internal int GetGatewayInstanceCountByTransition(SQLBuilder kit, string splitActivityId,
            int splitActivityInstanceId,
            int processInstanceId)
        {
            var clip = Sibling(kit).useClip();
            clip.from<TransitionInstanceEntity>(out var ti);
            clip.where(() => ti.ProcessInstanceId, processInstanceId);
            clip.where(() => ti.FromActivityId, splitActivityId);
            clip.where(() => ti.FromActivityInstanceId, splitActivityInstanceId);
            var list = clip.select(ti).queryList().ToList();
            return list.Count();
        }

        /// <summary>
        /// Obtain the number of valid branches
        ///  获取有效的分支数目
        /// </summary>
        internal List<ActivityInstanceEntity> GetValidSplitedActivityInstanceList(SQLBuilder kit, int processInstanceId,
            int splitGatewayInstanceId)
        {
            var clip = Sibling(kit).useClip();
            clip.from<ActivityInstanceEntity>(out var ai);
            clip.InnerJoin<TransitionInstanceEntity>(out var ti).on(() => ai.Id == ti.ToActivityInstanceId);
            clip.where(() => ai.ActivityState == (short)ActivityStateEnum.Ready
                || ai.ActivityState == (short)ActivityStateEnum.Running
                || ai.ActivityState == (short)ActivityStateEnum.Completed
                || ai.ActivityState == (short)ActivityStateEnum.Suspended);
            clip.where(() => ti.FromActivityInstanceId, splitGatewayInstanceId);
            return clip.select(ai).queryList().ToList();
        }

        /// <summary>
        /// Obtain the number of valid branches
        /// 获取有效的子节点列表
        /// </summary>
        internal List<ActivityInstanceEntity> GetValidActivityInstanceListOfMI(int mainActivityInstanceId,
            int processInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetValidActivityInstanceListOfMI(kit, mainActivityInstanceId, processInstanceId);
        }

        /// <summary>
        /// Obtain the number of valid branches
        /// 获取有效的子节点列表
        /// </summary>
        internal List<ActivityInstanceEntity> GetValidActivityInstanceListOfMI(SQLBuilder kit, int mainActivityInstanceId,
            int processInstanceId)
        {
            var clip = Sibling(kit).useClip();
            clip.from<ActivityInstanceEntity>(out var ai);
            clip.where(() => ai.ActivityState == (short)ActivityStateEnum.Ready
                || ai.ActivityState == (short)ActivityStateEnum.Running
                || ai.ActivityState == (short)ActivityStateEnum.Completed
                || ai.ActivityState == (short)ActivityStateEnum.Suspended);
            clip.where(() => ai.MainActivityInstanceId, mainActivityInstanceId);
            clip.where(() => ai.ProcessInstanceId, processInstanceId);
            var list = clip.select(ai).queryList().ToList();

            var backList = new List<int>();
            foreach (var child in list)
            {
                if (child.BackSrcActivityInstanceId != null)
                {
                    if (backList.Any(a => a == child.BackSrcActivityInstanceId.Value) == false)
                        backList.Add(child.BackSrcActivityInstanceId.Value);
                }

                if (child.BackOrgActivityInstanceId != null)
                {
                    if (backList.Any(a => a == child.BackOrgActivityInstanceId.Value) == false)
                        backList.Add(child.BackOrgActivityInstanceId.Value);
                }
            }

            if (backList.Count > 0)
            {
                int[] backArray = backList.ToArray();
                var validList = list.Where(a => !backArray.Contains(a.Id.Value)).ToList();
                return validList;
            }

            return list;
        }
        #endregion

        #region Create activity instance 创建活动实例
        /// <summary>
        /// Create activity instance
        /// 创建活动实例的对象
        /// </summary>
        internal ActivityInstanceEntity CreateActivityInstanceObject(string appName,
            string appInstanceId,
            string appInstanceCode,
            string processId,
            int processInstanceId,
            Activity activity,
            WfAppRunner runner)
        {
            ActivityInstanceEntity instance = new ActivityInstanceEntity();
            instance.ActivityId = activity.ActivityId;
            instance.ActivityName = activity.ActivityName;
            instance.ActivityCode = activity.ActivityCode;
            instance.ActivityType = (short)activity.ActivityType;
            instance.WorkItemType = (short)activity.WorkItemType;
            instance.GatewayDirectionTypeId = activity.GatewayDetail != null ? (short)activity.GatewayDetail.DirectionType : null;
            instance.ProcessId = processId;
            instance.AppName = appName;
            instance.AppInstanceId = appInstanceId;
            instance.AppInstanceCode = appInstanceCode;
            instance.ProcessInstanceId = processInstanceId;
            instance.TokensRequired = 1;
            instance.TokensHad = 1;
            instance.CreatedUserId = runner.UserId;
            instance.CreatedUserName = runner.UserName;
            instance.CreatedDateTime = System.DateTime.UtcNow;
            instance.OverdueDateTime = CalculateActivityOverdueDateTime(activity.BoundaryList, instance.CreatedDateTime.Value);
            instance.ActivityState = (short)ActivityStateEnum.Ready;
            instance.CanNotRenewInstance = 0;

            return instance;
        }

        /// <summary>
        /// Calculate the expiration time of activity nodes
        /// 计算活动节点过期时间
        /// </summary>
        private Nullable<DateTime> CalculateActivityOverdueDateTime(IList<Boundary> boundaryList,
            DateTime createdDateTime)
        {
            Nullable<DateTime> overdueDateTime = null;
            if (boundaryList != null && boundaryList.Count() > 0)
            {
                foreach (var boundary in boundaryList)
                {
                    if (boundary.EventTriggerType == EventTriggerEnum.Timer
                        && !string.IsNullOrEmpty(boundary.Expression))
                    {
                        var timeSpan = System.Xml.XmlConvert.ToTimeSpan(boundary.Expression);
                        overdueDateTime = createdDateTime.Add(timeSpan);
                        break;
                    }
                }
            }
            return overdueDateTime;
        }

        /// <summary>
        /// Copy child nodes based on the master node
        /// 根据主节点复制子节点
        /// </summary>
        internal ActivityInstanceEntity CreateActivityInstanceObject(ActivityInstanceEntity main)
        {
            ActivityInstanceEntity instance = new ActivityInstanceEntity();
            instance.ActivityId = main.ActivityId;
            instance.ActivityName = main.ActivityName;
            instance.ActivityCode = main.ActivityCode;
            instance.ActivityType = main.ActivityType;
            instance.WorkItemType = main.WorkItemType;
            instance.GatewayDirectionTypeId = main.GatewayDirectionTypeId;
            instance.ProcessId = main.ProcessId;
            instance.AppName = main.AppName;
            instance.AppInstanceId = main.AppInstanceId;
            instance.AppInstanceCode = main.AppInstanceCode;
            instance.ProcessInstanceId = main.ProcessInstanceId;
            instance.TokensRequired = 1;
            instance.TokensHad = 1;
            instance.CreatedUserId = main.CreatedUserId;
            instance.CreatedUserName = main.CreatedUserName;
            instance.CreatedDateTime = System.DateTime.UtcNow;
            instance.ActivityState = (short)ActivityStateEnum.Ready;
            instance.CanNotRenewInstance = 0;

            return instance;
        }

        /// <summary>
        /// Create an object for the activity instance
        /// 创建活动实例的对象
        /// </summary>
        internal ActivityInstanceEntity CreateBackwardActivityInstanceObject(string appName,
            string appInstanceId,
            string appInstanceCode,
            int processInstanceId,
            string processId,
            Activity activity,
            BackwardTypeEnum backwardType,
            int backSrcActivityInstanceId,
            int backOrgActivityInstanceId,
            WfAppRunner runner)
        {
            ActivityInstanceEntity instance = new ActivityInstanceEntity();
            instance.ActivityId = activity.ActivityId;
            instance.ActivityName = activity.ActivityName;
            instance.ActivityCode = activity.ActivityCode;
            instance.ActivityType = (short)activity.ActivityType;
            instance.WorkItemType = (short)activity.WorkItemType;
            instance.GatewayDirectionTypeId = activity.GatewayDetail != null ? (short)activity.GatewayDetail.DirectionType : null;
            instance.ProcessId = processId;
            instance.AppName = appName;
            instance.AppInstanceId = appInstanceId;
            instance.AppInstanceCode = appInstanceCode;
            instance.ProcessInstanceId = processInstanceId;
            instance.BackwardType = (short)backwardType;
            instance.BackSrcActivityInstanceId = backSrcActivityInstanceId;
            instance.BackOrgActivityInstanceId = backOrgActivityInstanceId;
            instance.TokensRequired = 1;
            instance.TokensHad = 1;
            instance.CreatedUserId = runner.UserId;
            instance.CreatedUserName = runner.UserName;
            instance.CreatedDateTime = System.DateTime.UtcNow;
            instance.ActivityState = (short)ActivityStateEnum.Ready;
            instance.CanNotRenewInstance = 0;

            return instance;
        }

        /// <summary>
        /// Update the number of tokens for the activity node
        /// 更新活动节点的Token数目
        /// </summary>
        internal void IncreaseTokensHad(SQLBuilder kit, int activityInstanceId,
            WfAppRunner runner)
        {
            // CAS 自增：WHERE TokensHad=旧值，避免并行汇聚丢 token
            _ = runner;
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var activityInstance = GetById(kit, activityInstanceId);
                if (activityInstance == null)
                    return;

                var prev = activityInstance.TokensHad;
                var next = (prev ?? 0) + 1;
                var clip = Sibling(kit).useClip();
                clip.setTable<ActivityInstanceEntity>(out var ai);
                clip.set(() => ai.TokensHad, next);
                clip.set(() => ai.UpdatedDateTime, DateTime.UtcNow);
                clip.where(() => ai.Id, activityInstanceId);
                if (prev == null)
                    clip.where(() => ai.TokensHad == null);
                else
                    clip.where(() => ai.TokensHad, prev);

                if (clip.doUpdate() > 0)
                    return;
            }

            throw new WorkflowException(
                "IncreaseTokensHad CAS failed after retries. ActivityInstanceId=" + activityInstanceId);
        }
        #endregion

        #region Activity instance status setting 活动实例状态设置
        /// <summary>
        /// Activity instance read
        /// 活动实例被读取
        /// </summary>
        internal void Read(SQLBuilder kit, int activityInstanceId,
            WfAppRunner runner)
        {
            SetActivityState(kit, activityInstanceId, ActivityStateEnum.Running, runner);
        }

        /// <summary>
        /// Set activity instance status
        /// 设置活动实例状态
        /// </summary>
        private void SetActivityState(SQLBuilder kit, int activityInstanceId,
            ActivityStateEnum activityState,
            WfAppRunner runner)
        {
            var activityInstance = GetById(kit, activityInstanceId);
            activityInstance.ActivityState = (short)activityState;
            activityInstance.UpdatedUserId = runner.UserId;
            activityInstance.UpdatedUserName = runner.UserName;
            activityInstance.UpdatedDateTime = System.DateTime.UtcNow;
            Update(kit, activityInstance);
        }

        /// <summary>
        /// Withdraw activity instance
        /// 撤销活动实例
        /// </summary>
        internal void Withdraw(SQLBuilder kit, int activityInstanceId,
            WfAppRunner runner)
        {
            EndActivityState(kit, activityInstanceId, ActivityStateEnum.Withdrawed, runner);
        }

        /// <summary>
        /// Sendback activity instance
        /// 退回活动实例
        /// </summary>
        internal void SendBack(SQLBuilder kit, int activityInstanceId,
            WfAppRunner runner)
        {
            EndActivityState(kit, activityInstanceId, ActivityStateEnum.Sendbacked, runner);
        }

        /// <summary>
        /// Set the end status of the activity
        /// 设置活动结束状态
        /// </summary>
        private void EndActivityState(SQLBuilder kit, int activityInstanceId,
            ActivityStateEnum activityState,
            WfAppRunner runner)
        {
            var activityInstance = GetById(kit, activityInstanceId);
            activityInstance.ActivityState = (short)activityState;
            activityInstance.EndedUserId = runner.UserId;
            activityInstance.EndedUserName = runner.UserName;
            activityInstance.EndedDateTime = System.DateTime.UtcNow;

            Update(kit, activityInstance);
        }

        /// <summary>
        /// Complete activity instance
        /// 活动实例完成
        /// </summary>
        internal void Complete(SQLBuilder kit, int activityInstanceId,
            WfAppRunner runner)
        {
            EndActivityState(kit, activityInstanceId, ActivityStateEnum.Completed, runner);
        }

        internal void Complete(SQLBuilder kit, ActivityInstanceEntity activityInstance,
            WfAppRunner runner)
        {
            activityInstance.ActivityState = (short)ActivityStateEnum.Completed;
            activityInstance.EndedUserId = runner.UserId;
            activityInstance.EndedUserName = runner.UserName;
            activityInstance.EndedDateTime = System.DateTime.UtcNow;

            Update(kit, activityInstance);
        }

        /// <summary>
        /// Update approval status to agree
        /// 更新审批状态为同意
        /// </summary>
        internal void SetApprovalStatus(SQLBuilder kit, int activityInstanceId)
        {
            var activityInstance = GetById(kit, activityInstanceId);
            if (activityInstance.ApprovalStatus == (short)ApprovalStatusEnum.Null)
            {
                activityInstance.ApprovalStatus = (short)ApprovalStatusEnum.Agreed;
                Update(kit, activityInstance);
            }
        }

        /// <summary>
        /// Update the running nodes between branches and merges to block status
        /// 更新分支和合并之间的运行节点为阻止状态
        /// </summary>
        internal void UpdateActivityInstanceBlockedBetweenSplitJoin(SQLBuilder kit, Activity gatewayActivity,
            ActivityInstanceEntity gatewayInstance,
            IProcessModel processModel)
        {
            var joinCount = 0;
            var splitCount = 0;
            var splitActivity = processModel.GetBackwardGatewayActivity(gatewayActivity, ref joinCount, ref splitCount);
            var taskActivityList = processModel.GetAllTaskActivityList(splitActivity, gatewayActivity);

            UpdateActivityInstanceBlockedBetweenSplitJoin(kit, gatewayInstance.ProcessInstanceId.Value, taskActivityList);
        }

        /// <summary>
        /// Update the activities in the activity to block status
        /// 更新活动里面的活动为阻止状态
        /// </summary>
        private void UpdateActivityInstanceBlockedBetweenSplitJoin(SQLBuilder kit, int processInstanceId,
            IList<Activity> taskActivityList)
        {
            var idsin = taskActivityList.Select(a => a.ActivityId).ToList();
            var clip = Sibling(kit).useClip();
            clip.setTable<ActivityInstanceEntity>(out var ai);
            clip.set(() => ai.CanNotRenewInstance, (byte)1);
            clip.where(() => ai.ProcessInstanceId, processInstanceId);
            clip.whereIn(() => ai.ActivityState, new short?[]
            {
                (short)ActivityStateEnum.Ready,
                (short)ActivityStateEnum.Running,
                (short)ActivityStateEnum.Suspended
            });
            clip.whereIn(() => ai.ActivityId, idsin);
            clip.doUpdate();
        }
        #endregion

        #region Activity instance record maintenance 活动实例记录维护
        /// <summary>
        /// Insert activity instance
        /// 插入活动实例
        /// </summary>
        internal int Insert(ActivityInstanceEntity entity)
        {
            var kit = Sail.UseRunSQL();
            return Insert(kit, entity);
        }

        internal int Insert(SQLBuilder kit, ActivityInstanceEntity entity)
        {
            if (entity.ActivityType == (short)ActivityTypeEnum.GatewayNode
                && string.IsNullOrEmpty(entity.ActivityName))
            {
                entity.ActivityName = "GATEWAY";
            }

            int newId = Sibling(kit).useRepo<ActivityInstanceEntity, int>().InsertReturnId(entity);
            entity.Id = newId;

            return newId;
        }

        /// <summary>
        /// Update activity instance
        /// 更新活动实例
        /// </summary>
        internal void Update(ActivityInstanceEntity entity)
        {
            var kit = Sail.UseRunSQL();
            Update(kit, entity);
        }

        internal void Update(SQLBuilder kit, ActivityInstanceEntity entity)
            => Sibling(kit).useRepo<ActivityInstanceEntity>().Update(entity);

        /// <summary>
        /// Cancel activity instance
        /// 取消节点运行
        /// </summary>
        internal void Cancel(SQLBuilder kit, int activityInstanceId,
            WfAppRunner runner)
        {
            var clip = Sibling(kit).useClip();
            clip.setTable<ActivityInstanceEntity>(out var ai);
            clip.set(() => ai.ActivityState, (short)ActivityStateEnum.Cancelled);
            clip.set(() => ai.UpdatedUserId, runner.UserId);
            clip.set(() => ai.UpdatedUserName, runner.UserName);
            clip.set(() => ai.UpdatedDateTime, System.DateTime.UtcNow);
            clip.where(() => ai.Id, activityInstanceId);
            clip.where(() => ai.ActivityState == (short)ActivityStateEnum.Ready
                || ai.ActivityState == (short)ActivityStateEnum.Running
                || ai.ActivityState == (short)ActivityStateEnum.Suspended);
            clip.doUpdate();
        }

        /// <summary>
        /// Update the status of unfinished nodes in the countersignature node
        /// 更新会签节点中未办理完成的节点状态
        /// </summary>
        internal void CancelUnCompletedMultipleInstance(SQLBuilder kit, int mainActivityInstanceId, WfAppRunner runner)
        {
            var clip = Sibling(kit).useClip();
            clip.setTable<ActivityInstanceEntity>(out var ai);
            clip.set(() => ai.ActivityState, (short)ActivityStateEnum.Cancelled);
            clip.set(() => ai.UpdatedUserId, runner.UserId);
            clip.set(() => ai.UpdatedUserName, runner.UserName);
            clip.set(() => ai.UpdatedDateTime, System.DateTime.UtcNow);
            clip.where(() => ai.MainActivityInstanceId, mainActivityInstanceId);
            clip.where(() => ai.ActivityState == (short)ActivityStateEnum.Ready
                || ai.ActivityState == (short)ActivityStateEnum.Running
                || ai.ActivityState == (short)ActivityStateEnum.Suspended);
            clip.doUpdate();
        }

        /// <summary>
        /// Withdraw the master node and its multiple instance nodes below it
        /// 撤销主节点及其下面的多实例子节点
        /// </summary>
        internal void WithdrawMainIncludedChildNodes(SQLBuilder kit, int mainActivityInstanceId, WfAppRunner runner)
        {
            SetActivityState(kit, mainActivityInstanceId, ActivityStateEnum.Withdrawed, runner);
            WithdrawMultipleInstance(kit, mainActivityInstanceId, runner);
        }

        /// <summary>
        /// Update the node status of the counter signature processing node
        /// 更新会签办理节点的节点状态
        /// </summary>
        private void WithdrawMultipleInstance(SQLBuilder kit, int mainActivityInstanceId, WfAppRunner runner)
        {
            var clip = Sibling(kit).useClip();
            clip.setTable<ActivityInstanceEntity>(out var ai);
            clip.set(() => ai.ActivityState, (short)ActivityStateEnum.Withdrawed);
            clip.set(() => ai.UpdatedUserId, runner.UserId);
            clip.set(() => ai.UpdatedUserName, runner.UserName);
            clip.set(() => ai.UpdatedDateTime, System.DateTime.UtcNow);
            clip.where(() => ai.MainActivityInstanceId, mainActivityInstanceId);
            clip.where(() => ai.ActivityState == (short)ActivityStateEnum.Ready
                || ai.ActivityState == (short)ActivityStateEnum.Suspended);
            clip.doUpdate();
        }

        /// <summary>
        /// Put the node back in a suspended state
        /// 重新使节点处于挂起状态
        /// </summary>
        internal void Resuspend(SQLBuilder kit, int activityInstanceId, WfAppRunner runner)
        {
            SetActivityState(kit, activityInstanceId, ActivityStateEnum.Suspended, runner);
        }

        /// <summary>
        /// Put the node back in a running state
        /// 重新使节点处于运行状态
        /// </summary>
        internal void Rerun(SQLBuilder kit, int activityInstanceId, WfAppRunner runner)
        {
            SetActivityState(kit, activityInstanceId, ActivityStateEnum.Running, runner);
        }

        /// <summary>
        /// Delete activity instance
        /// 删除活动实例
        /// </summary>
        internal void Delete(int activityInstanceId)
        {
            var kit = Sail.UseRunSQL();
            Delete(kit, activityInstanceId);
        }

        internal void Delete(SQLBuilder kit, int activityInstanceId)
            => Sibling(kit).useRepo<ActivityInstanceEntity, int>().DeleteById(activityInstanceId);
        #endregion

        #region Activity Instance Approval Status 活动实例审批状态
        /// <summary>
        /// Agree
        /// 同意
        /// </summary>
        internal void Agree(int taskId)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    Agree(kit, taskId);
                    kit.commit();
                }
                catch
                {
                    throw;
                }
            }
        }

        internal void Agree(SQLBuilder kit, int taskId)
        {
            var activityInstance = GetByTask(kit, taskId);
            activityInstance.ApprovalStatus = (short)ApprovalStatusEnum.Agreed;
            Update(kit, activityInstance);
        }

        /// <summary>
        /// Refuse
        /// 拒绝
        /// </summary>
        internal void Refuse(int taskId)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    Refuse(kit, taskId);
                    kit.commit();
                }
                catch
                {
                    throw;
                }
            }
        }

        internal void Refuse(SQLBuilder kit, int taskId)
        {
            var activityInstance = GetByTask(kit, taskId);
            activityInstance.ApprovalStatus = (short)ApprovalStatusEnum.Refused;
            Update(kit, activityInstance);
        }

        /// <summary>
        /// Check the pass type of the current node
        ///  检查当前节点的通过类型
        /// </summary>
        internal NodePassedResult CheckActivityInstancePassedResult(SQLBuilder kit, Nullable<int> activityInstanceId)
        {
            var result = NodePassedResult.Create(NodePassedTypeEnum.Default);
            if (activityInstanceId != null)
            {
                var activityInstance = GetById(kit, activityInstanceId.Value);
                if (IsMultipleInstanceChildren(activityInstance) == true)
                {
                    var mainActivityInstance = GetById(kit, activityInstance.MainActivityInstanceId.Value);
                    var nodePassedType = CheckMIPassRateInfo(kit, activityInstance, mainActivityInstance);
                    result = NodePassedResult.Create(nodePassedType);
                }
                else
                {
                    result = NodePassedResult.CreateByApprovalStatus(activityInstance.ApprovalStatus.Value);
                }
            }
            return result;
        }

        /// <summary>
        /// Check the satisfaction of the pass rate of the co signing nodes
        /// 检查会签节点通过率的满足情况
        /// </summary>
        internal NodePassedTypeEnum CheckMIPassRateInfo(SQLBuilder kit, ActivityInstanceEntity currentActivityInstance,
            ActivityInstanceEntity mainActivityInstance)
        {
            var childActivityInstanceList = GetValidActivityInstanceListOfMI(kit, mainActivityInstance.Id.Value, mainActivityInstance.ProcessInstanceId.Value);

            // R2：已挂管道时只走 LeaveJudge（含 Legacy 会签→Collaborate 推断）；禁止回落旧通过率旁路
            var pipeline = Sail.AssigneePipeline;
            string leaveProcessId = mainActivityInstance.ProcessId;
            string leaveVersion = null;
            try
            {
                var pi = Sail.ProcessInstances.GetById(
                    kit, mainActivityInstance.ProcessInstanceId.Value);
                leaveProcessId = pi?.ProcessId ?? leaveProcessId;
                leaveVersion = pi?.Version;
            }
            catch
            {
                /* 读版本失败时仍尝试 Leave；Pipeline 可再自查 */
            }

            if (pipeline != null)
            {
                if (pipeline.TryCanLeave(
                        new LeaveInvokeRequest
                        {
                            ProcessInstanceId = mainActivityInstance.ProcessInstanceId.Value,
                            ActivityInstanceId = currentActivityInstance.Id.Value,
                            ProcessId = leaveProcessId,
                            Version = leaveVersion,
                            CurrentActivityInstance = currentActivityInstance,
                            MainActivityInstance = mainActivityInstance,
                            ChildActivityInstances = childActivityInstanceList
                        },
                        out var leave)
                    && leave != null)
                {
                    if (string.Equals(leave.Decision, LeaveDecisions.Passed, StringComparison.OrdinalIgnoreCase))
                        return NodePassedTypeEnum.Passed;
                    if (string.Equals(leave.Decision, LeaveDecisions.NotPassed, StringComparison.OrdinalIgnoreCase))
                        return NodePassedTypeEnum.NotPassed;
                    if (string.Equals(leave.Decision, LeaveDecisions.Waiting, StringComparison.OrdinalIgnoreCase))
                        return NodePassedTypeEnum.NeedToBeMoreApproved;
                    return NodePassedTypeEnum.Default;
                }

                throw new WfAssigneeException(
                    AssigneeReasonCodes.LeaveConfigMissing,
                    "LeaveJudge missing for multi-instance activity '"
                    + (mainActivityInstance.ActivityId ?? mainActivityInstance.Id?.ToString())
                    + "'. Configure LeaveJudge in designer (R6). ReasonCode="
                    + AssigneeReasonCodes.LeaveConfigMissing);
            }

            // R2：未挂管道不再回落旧 MI 通过率旁路（与 Resolve 对齐）
            _ = Sail.GetAssigneePipelineRequired();
            // GetAssigneePipelineRequired 在未挂管道时抛 InvalidOperationException；不可达
            return NodePassedTypeEnum.Default;
        }

        /// <summary>
        /// Obtain the approval rate type of the countersignature node
        /// 获取会签节点的审批通过率类型
        /// </summary>
        internal Boolean GetMiApprovalThresholdStatus(SQLBuilder kit, ActivityInstanceEntity currentActivityInstance,
            ActivityInstanceEntity mainActivityInstance)
        {
            var finalPassed = false;
            var passedType = CheckMIPassRateInfo(kit, currentActivityInstance, mainActivityInstance);
            if (passedType == NodePassedTypeEnum.Passed || passedType == NodePassedTypeEnum.NotPassed)
            {
                finalPassed = true;
            }
            return finalPassed;
        }
        #endregion
    }
}
