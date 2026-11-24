
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using sailflow.data;
using sailflow.assignee;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.log;
using sailflow.run;
using sailflow.xml;
using sailflow.hub.Localize;

namespace sailflow.data
{
    /// <summary>
    /// Activity Instance Manager
    /// 活动实例管理类
    /// </summary>
    public class ActivityInstanceManager : IActivityInstanceManager
    {
        public ActivityInstanceManager(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        Exception FailThrow(Exception ex, string op, string detail = null)
        {
            SailFailLog.Exception(Sail, SailLogCategories.Engine, "ActivityInstance." + op, ex, detail);
            return ex;
        }

        #region Activity instance data acquisition 活动实例数据获取
        /// <summary>
        /// Retrieve activity instances based on Id
        /// 根据ID获取活动实例
        /// </summary>
        public ActivityInstanceEntity GetById(string activityInstanceId)
        {
            try
            {
                return Sail.UseRunSQL().findRowById<ActivityInstanceEntity>(activityInstanceId);
            }
            catch (System.Exception e)
            {
                throw FailThrow(new ApplicationException(
                    Sail.Messages.GetEngineMessage("activityinstancemanager.getbyid.error", e.Message), e), "GetById");
            }
        }

        public ActivityInstanceEntity GetById(SQLBuilder kit, string activityInstanceId)
            => kit.findRowById<ActivityInstanceEntity>(activityInstanceId);

        /// <summary>
        /// 异步按 Id 取活动实例（Clip take(1)，可取消）。与同步 Repo.GetById 双轨。
        /// </summary>
        public async Task<ActivityInstanceEntity> GetByIdAsync(
            SQLBuilder kit,
            string activityInstanceId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var clip = kit.useClip();
            clip.from<ActivityInstanceEntity>(out var ai)
                .where(() => ai.ActivityInstanceId, activityInstanceId);
            var list = await clip.select(ai).take(1).queryListAsync(cancellationToken).ConfigureAwait(false);
            return list?.FirstOrDefault();
        }

        /// <summary>
        /// Obtain the current running node information of the process
        /// 获取流程当前运行节点信息
        /// </summary>
        public ActivityInstanceEntity GetRunningNode(FlowCommandContext runner)
        {
            var kit = Sail.UseRunSQL();
            return GetRunningNode(kit, runner, out _);
        }

        /// <summary>
        /// Obtain the current running node information of the process
        /// 获取流程当前运行节点信息
        /// </summary>
        public ActivityInstanceEntity GetRunningNode(FlowCommandContext runner,
            out TaskViewEntity taskView)
        {
            var kit = Sail.UseRunSQL();
            return GetRunningNode(kit, runner, out taskView);
        }

        /// <summary>
        /// Obtain the current running node information of the process
        /// 获取流程当前运行节点信息
        /// </summary>
        public ActivityInstanceEntity GetRunningNode(SQLBuilder kit,
            FlowCommandContext runner,
            out TaskViewEntity taskView)
        {
            var taskId = runner.TaskId;
            taskView = null;

            var tm = Sail.ManagerFactory.Tasks;
            ActivityInstanceEntity runningNode = null;
            if (!string.IsNullOrWhiteSpace(taskId))
            {
                taskView = tm.GetTaskView(kit, taskId);
                runningNode = GetById(kit, taskView.ActivityInstanceId);
                return runningNode;
            }

            var activityInstanceList = GetRunningActivityInstanceList(kit, runner.BizPK, runner.ProcessId, runner.Version).ToList();
            if (activityInstanceList == null || activityInstanceList.Count == 0)
            {
                var message = Sail.Messages.GetEngineMessage("activityinstancemanager.getrunningnode.error");
                var e = new WorkflowException(message);
                ILogManager.RecordLog(Sail, message, LogEventType.Exception, LogPriority.Normal, null, e);
                throw e;
            }

            if (activityInstanceList.Count == 1)
            {
                runningNode = activityInstanceList[0];
                taskView = tm.GetTaskOfMineByActivityInstance(kit, runningNode.ActivityInstanceId, runner.UserId);
            }
            else if (activityInstanceList.Count > 1)
            {
                // 以本人打开待办挂靠的 AI 为准（勿用 SignIds 汇总选型）
                foreach (var ai in activityInstanceList)
                {
                    var mine = tm.GetTaskOfMineByActivityInstance(kit, ai.ActivityInstanceId, runner.UserId);
                    if (mine == null)
                        continue;
                    runningNode = ai;
                    taskView = mine;
                    break;
                }

                if (runningNode == null)
                {
                    var msgException = Sail.Messages.GetEngineMessage("activityinstancemanager.getrunningnode.unique.error");
                    var e = new WorkflowException(msgException);
                    ILogManager.RecordLog(Sail, Sail.Messages.GetEngineMessage("activityinstancemanager.getrunningnode.error"),
                        LogEventType.Exception, LogPriority.Normal, null, e);
                    throw e;
                }
            }
            return runningNode;
        }

        /// <summary>
        /// 异步获取当前运行节点（可取消）。返回 (Node, TaskView)；语义对齐同步 <see cref="GetRunningNode(SQLBuilder, FlowCommandContext, out TaskViewEntity)"/>。
        /// </summary>
        public async Task<(ActivityInstanceEntity Node, TaskViewEntity TaskView)> GetRunningNodeAsync(
            SQLBuilder kit,
            FlowCommandContext runner,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tm = Sail.ManagerFactory.Tasks;
            TaskViewEntity taskView = null;
            ActivityInstanceEntity runningNode = null;

            var taskId = runner.TaskId;
            if (!string.IsNullOrWhiteSpace(taskId))
            {
                taskView = await tm.GetTaskViewAsync(kit, taskId, cancellationToken).ConfigureAwait(false);
                runningNode = await GetByIdAsync(kit, taskView.ActivityInstanceId, cancellationToken)
                    .ConfigureAwait(false);
                return (runningNode, taskView);
            }

            var activityInstanceList = (await GetRunningActivityInstanceListAsync(
                    kit, runner.BizPK, runner.ProcessId, runner.Version, cancellationToken)
                .ConfigureAwait(false)).ToList();

            if (activityInstanceList == null || activityInstanceList.Count == 0)
            {
                var message = Sail.Messages.GetEngineMessage("activityinstancemanager.getrunningnode.error");
                var e = new WorkflowException(message);
                ILogManager.RecordLog(Sail, message, LogEventType.Exception, LogPriority.Normal, null, e);
                throw e;
            }

            if (activityInstanceList.Count == 1)
            {
                runningNode = activityInstanceList[0];
                taskView = await tm.GetTaskOfMineByActivityInstanceAsync(
                        kit, runningNode.ActivityInstanceId, runner.UserId, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                // 以本人打开待办挂靠的 AI 为准（勿用 SignIds 汇总选型）
                foreach (var ai in activityInstanceList)
                {
                    var mine = await tm.GetTaskOfMineByActivityInstanceAsync(
                            kit, ai.ActivityInstanceId, runner.UserId, cancellationToken)
                        .ConfigureAwait(false);
                    if (mine == null)
                        continue;
                    runningNode = ai;
                    taskView = mine;
                    break;
                }

                if (runningNode == null)
                {
                    var msgException = Sail.Messages.GetEngineMessage("activityinstancemanager.getrunningnode.unique.error");
                    var e = new WorkflowException(msgException);
                    ILogManager.RecordLog(Sail, Sail.Messages.GetEngineMessage("activityinstancemanager.getrunningnode.error"),
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
        public ActivityInstanceEntity GetBackSrcActivityInstance(string currentActivityInstanceId)
        {
            var currentActivityInstance = GetById(currentActivityInstanceId);
            var backSrcActivityInstance = GetById(currentActivityInstance.BackSrcActInstanceId);

            return backSrcActivityInstance;
        }

        /// <summary>
        /// Determine whether it is a processing task for a certain user
        /// 判断是否是某个用户的办理任务（以打开中的 Task.SignId 为准，不用活动 SignIds 汇总）
        /// </summary>
        public bool IsMineTask(ActivityInstanceEntity entity,
            string userId)
        {
            if (entity == null || string.IsNullOrWhiteSpace(userId))
                return false;
            var mine = Sail.ManagerFactory.Tasks.GetTaskOfMineByActivityInstance(
                entity.ActivityInstanceId, userId);
            return mine != null;
        }

        /// <summary>
        /// Determine whether the node is in a running state
        /// 判断节点是否处于运行状态
        /// </summary>
        public Boolean IncludeRunningState(ActivityInstanceEntity activityInstance)
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
        public IList<ActivityInstanceEntity> GetActivityInstanceList(SQLBuilder kit, string processInstanceId)
        {
            var clip = kit.useClip();
            clip.from<ActivityInstanceEntity>(out var ai)
                .where(() => ai.ProcessInstanceId, processInstanceId)
                .orderBy(() => ai.CreatedDateTime);
            return clip.select(ai).queryList().ToList();
        }

        /// <summary>
        /// Get Activity Instance Latest
        /// 获取活动节点实例
        /// </summary>
        public ActivityInstanceEntity GetActivityInstanceLatest(string processInstanceId,
            string activityId)
        {
            var kit = Sail.UseRunSQL();
            return GetActivityInstanceLatest(kit, processInstanceId, activityId);
        }

        /// <summary>
        /// Get Activity Instance Latest
        /// 获取最近的节点实例
        /// </summary>
        public ActivityInstanceEntity GetActivityInstanceLatest(SQLBuilder kit, string processInstanceId,
            string activityId)
        {
            var clip = kit.useClip();
            clip.from<ActivityInstanceEntity>(out var ai)
                .where(() => ai.ProcessInstanceId, processInstanceId)
                .where(() => ai.ActivityId, activityId)
                .orderByDesc(() => ai.CreatedDateTime);
            return clip.select(ai).take(1).queryList()?.FirstOrDefault();
        }

        /// <summary>
        /// Get Activity Instance Latest
        /// 获取最近的节点实例
        /// </summary>
        public ActivityInstanceEntity GetActivityInstanceLatest(string bizPK,
            string processId,
            string activityId)
        {
            var kit = Sail.UseRunSQL();
            return GetActivityInstanceLatest(kit, bizPK, processId, activityId);
        }

        public ActivityInstanceEntity GetActivityInstanceLatest(SQLBuilder kit, string bizPK,
            string processId,
            string activityId)
        {
            var clip = kit.useClip();
            clip.from<ActivityInstanceEntity>(out var ai)
                .InnerJoin<ProcessInstanceEntity>(out var pi).on(() => ai.ProcessInstanceId == pi.ProcessInstanceId)
                .where(() => pi.ProcessState, (short)ProcessStateEnum.Running)
                .where(() => ai.BizPK, bizPK)
                .where(() => ai.ProcessId, processId)
                .where(() => ai.ActivityId, activityId);
            var list = clip.select(ai).queryList().ToList();

            if (list.Count > 0)
            {
                var matchedActivityInstance = list[0];
                return GetById(kit, matchedActivityInstance.ActivityInstanceId);
            }

            return null;
        }

        /// <summary>
        /// Get Activity Instance List Latest
        /// 获取最近的节点实例
        /// </summary>
        public List<ActivityInstanceEntity> GetActivityInstanceList(string processId,
            string version,
            string activityId)
        {
            var kit = Sail.UseRunSQL();
            var clip = kit.useClip();
            clip.from<ActivityInstanceEntity>(out var ai)
                .InnerJoin<ProcessInstanceEntity>(out var pi).on(() => ai.ProcessInstanceId == pi.ProcessInstanceId)
                .where(() => pi.ProcessState, (short)ProcessStateEnum.Running)
                .where(() => pi.ProcessId, processId)
                .where(() => pi.Version, version)
                .where(() => ai.ActivityId, activityId)
                .whereIn(() => ai.ActivityState, new short?[]
                {
                    (short)ActivityStateEnum.Ready,
                    (short)ActivityStateEnum.Running
                });
            return clip.select(ai).queryList().ToList();
        }

        /// <summary>
        /// Get activity instances with running status
        /// 获取运行状态的活动实例
        /// </summary>
        public ActivityInstanceEntity GetActivityRunning(SQLBuilder kit, string processInstanceId,
            string activityId)
        {
            return GetActivityByState(kit, processInstanceId, activityId, ActivityStateEnum.Running);
        }

        /// <summary>
        /// Obtain activity instances based on states
        /// 根据状态获取活动实例
        /// </summary>
        public ActivityInstanceEntity GetActivityByState(SQLBuilder kit, string processInstanceId,
            string activityId,
            ActivityStateEnum activityState)
        {
            ActivityInstanceEntity entity = null;
            var clip = kit.useClip();
            clip.from<ActivityInstanceEntity>(out var ai)
                .where(() => ai.ProcessInstanceId, processInstanceId)
                .where(() => ai.ActivityId, activityId)
                .where(() => ai.ActivityState, (short)activityState)
                .orderByDesc(() => ai.CreatedDateTime);
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
        public List<ActivityInstanceEntity> GetActivityInstanceListCompleted(string bizPK,
            string processId)
        {
            var kit = Sail.UseRunSQL();
            var clip = kit.useClip();
            clip.from<ActivityInstanceEntity>(out var ai)
                .InnerJoin<ProcessInstanceEntity>(out var pi).on(() => ai.ProcessInstanceId == pi.ProcessInstanceId)
                .where(() => pi.ProcessState, (short)ProcessStateEnum.Running)
                .where(() => ai.BizPK, bizPK)
                .where(() => ai.ProcessId, processId)
                .where(() => ai.ActivityState, (short)ActivityStateEnum.Completed);
            return clip.select(ai).queryList().ToList();
        }

        /// <summary>
        /// Obtain instances of activities that have already been completed in the previous step
        /// 获取上一步已经完成活动的实例
        /// </summary>
        public ActivityInstanceEntity GetPreviousActivityInstanceSimple(ActivityInstanceEntity runningNode,
            string previousActivityId)
        {
            var kit = Sail.UseRunSQL();
            return GetPreviousActivityInstanceSimple(kit, runningNode, previousActivityId);
        }

        /// <summary>
        /// Obtain instances of activities that have already been completed in the previous step
        /// 获取上一步已经完成活动的实例
        /// </summary>
        public ActivityInstanceEntity GetPreviousActivityInstanceSimple(SQLBuilder kit, ActivityInstanceEntity runningNode,
            string previousActivityId)
        {
            ActivityInstanceEntity originalRunningNode = null;
            var processInstanceId = runningNode.ProcessInstanceId;

            if (runningNode.BackSrcActInstanceId != null)
            {
                originalRunningNode = GetById(kit, runningNode.BackOrgActInstanceId);
            }
            else
            {
                originalRunningNode = runningNode;
            }

            var instanceList = GetActivityInstanceListCompletedSimple(kit, processInstanceId, previousActivityId);

            var withoutBackSrcInfoList = instanceList.Where(a => a.BackSrcActInstanceId == null);

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
        public IList<ActivityInstanceEntity> GetActivityInstanceListCompletedSimple(string processInstanceId,
            string activityId)
        {
            var kit = Sail.UseRunSQL();
            return GetActivityInstanceListCompletedSimple(kit, processInstanceId, activityId);
        }

        /// <summary>
        /// Obtain activity instances with completion status
        /// 获取完成状态的活动实例
        /// </summary>
        public IList<ActivityInstanceEntity> GetActivityInstanceListCompletedSimple(SQLBuilder kit, string processInstanceId,
            string activityId)
        {
            var clip = kit.useClip();
            clip.from<ActivityInstanceEntity>(out var ai)
                .where(() => ai.ProcessInstanceId, processInstanceId)
                .where(() => ai.ActivityId, activityId)
                .where(() => ai.ActivityState, (short)ActivityStateEnum.Completed)
                .orderByDesc(() => ai.EndedDateTime);
            return clip.select(ai).queryList().ToList();
        }

        /// <summary>
        /// Retrieve activity instance information from task Id
        /// 由任务ID获取活动实例信息
        /// </summary>
        public ActivityInstanceEntity GetByTask(string taskId)
        {
            var kit = Sail.UseRunSQL();
            return GetByTask(kit, taskId);
        }

        /// <summary>
        /// Retrieve activity instance information from task Id
        /// 由任务ID获取活动实例信息
        /// </summary>
        public ActivityInstanceEntity GetByTask(SQLBuilder kit, string taskId)
        {
            ActivityInstanceEntity entity = null;
            var clip = kit.useClip();
            clip.from<ActivityInstanceEntity>(out var ai)
                .InnerJoin<TaskEntity>(out var t).on(() => ai.ActivityInstanceId == t.ActivityInstanceId)
                .where(() => t.TaskId, taskId);
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
        public IEnumerable<ActivityInstanceEntity> GetRunningActivityInstanceList(string bizPK,
            string processId,
            string version = null)
        {
            var kit = Sail.UseRunSQL();
            return GetRunningActivityInstanceList(kit, bizPK, processId, version);
        }

        /// <summary>
        /// Get the activity nodes running in the process instance
        /// 获取流程实例中运行的活动节点
        /// </summary>
        public IEnumerable<ActivityInstanceEntity> GetRunningActivityInstanceList(SQLBuilder kit, string bizPK,
            string processId,
            string version)
        {
            if (string.IsNullOrEmpty(version)) version = "1";
            var clip = kit.useClip();
            clip.from<ActivityInstanceEntity>(out var ai)
                .InnerJoin<ProcessInstanceEntity>(out var pi).on(() => ai.ProcessInstanceId == pi.ProcessInstanceId)
                .whereIn(() => ai.ActivityState, new short?[]
                {
                    (short)ActivityStateEnum.Ready,
                    (short)ActivityStateEnum.Running
                })
                .where(() => pi.ProcessState, (short)ProcessStateEnum.Running)
                .where(() => pi.Version, version)
                .where(() => ai.BizPK, bizPK)
                .where(() => ai.ProcessId, processId);
            return clip.select(ai).queryList();
        }

        /// <summary>
        /// 异步获取运行中活动实例列表（可取消）。
        /// </summary>
        public async Task<IEnumerable<ActivityInstanceEntity>> GetRunningActivityInstanceListAsync(
            SQLBuilder kit,
            string bizPK,
            string processId,
            string version,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(version)) version = "1";
            var clip = kit.useClip();
            clip.from<ActivityInstanceEntity>(out var ai)
                .InnerJoin<ProcessInstanceEntity>(out var pi).on(() => ai.ProcessInstanceId == pi.ProcessInstanceId)
                .whereIn(() => ai.ActivityState, new short?[]
                {
                    (short)ActivityStateEnum.Ready,
                    (short)ActivityStateEnum.Running
                })
                .where(() => pi.ProcessState, (short)ProcessStateEnum.Running)
                .where(() => pi.Version, version)
                .where(() => ai.BizPK, bizPK)
                .where(() => ai.ProcessId, processId);
            return await clip.select(ai).queryListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieve the list of completed child nodes under the main node
        /// 获取主节点下已经完成的子节点列表
        /// </summary>
        public List<ActivityInstanceEntity> GetMultipleInstanceListCompleted(string mainActivityInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetMultipleInstanceListCompleted(kit, mainActivityInstanceId);
        }

        /// <summary>
        /// Retrieve the list of completed child nodes under the main node
        /// 获取主节点下已经完成的子节点列表
        /// </summary>
        public List<ActivityInstanceEntity> GetMultipleInstanceListCompleted(SQLBuilder kit, string mainActivityInstanceId)
        {
            var clip = kit.useClip();
            clip.from<ActivityInstanceEntity>(out var ai)
                .InnerJoin<ProcessInstanceEntity>(out var pi).on(() => ai.ProcessInstanceId == pi.ProcessInstanceId)
                .where(() => pi.ProcessState, (short)ProcessStateEnum.Running)
                .where(() => ai.MainActInstanceId, mainActivityInstanceId)
                .where(() => ai.ActivityState, (short)ActivityStateEnum.Completed);
            return clip.select(ai).queryList().ToList();
        }

        /// <summary>
        /// Retrieve completed multi instance nodes
        /// 获取已经完成的多实例子节点
        /// </summary>
        public List<ActivityInstanceEntity> GetPreviousParallelMultipleInstanceListCompleted(ActivityInstanceEntity runningNode,
            string previousActivityId)
        {
            var kit = Sail.UseRunSQL();
            return GetPreviousParallelMultipleInstanceListCompleted(kit, runningNode, previousActivityId);
        }

        /// <summary>
        /// Retrieve completed multi instance nodes
        /// 获取已经完成的多实例子节点
        /// </summary>
        public List<ActivityInstanceEntity> GetPreviousParallelMultipleInstanceListCompleted(SQLBuilder kit, ActivityInstanceEntity runningNode,
            string previousActivityId)
        {
            ActivityInstanceEntity originalRunningNode = null;
            if (runningNode.BackSrcActInstanceId != null)
            {
                originalRunningNode = GetById(kit, runningNode.BackOrgActInstanceId);
            }
            else
            {
                originalRunningNode = runningNode;
            }

            var clip = kit.useClip();
            clip.from<ActivityInstanceEntity>(out var ai)
                .where(() => ai.ProcessInstanceId, runningNode.ProcessInstanceId)
                .where(() => ai.ActivityId, previousActivityId)
                .where(() => ai.ActivityState, (short)ActivityStateEnum.Completed)
                .orderByDesc(() => ai.EndedDateTime);
            var instanceList = clip.select(ai).queryList().ToList();

            var withoutBackSrcInfoList = instanceList.Where(a => a.BackSrcActInstanceId == null).ToList();

            var firstMIChild = withoutBackSrcInfoList.First(a => a.EndedUserId == originalRunningNode.CreatedUserId);

            return GetMultipleInstanceListCompleted(kit, firstMIChild.MainActInstanceId);
        }

        /// <summary>
        /// Determine whether it is a signing together instance node
        /// 判断是否为会签实例节点 
        /// </summary>
        public Boolean IsMultipleInstanceChildren(ActivityInstanceEntity child)
        {
            return child.MainActInstanceId != null;
        }

        /// <summary>
        /// Filter activity instance list
        /// 过滤活动实例列表
        /// </summary>
        private List<ActivityInstanceEntity> FilteredActivityInstanceInTheSameBatch(List<ActivityInstanceEntity> instanceList)
        {
            var maxBackSrcActivityInstanceId = instanceList
                .Select(a => a.BackSrcActInstanceId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .OrderByDescending(id => id, StringComparer.Ordinal)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(maxBackSrcActivityInstanceId))
            {
                var childrenList = instanceList.Where<ActivityInstanceEntity>(a => a.BackSrcActInstanceId == maxBackSrcActivityInstanceId).ToList();
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
        public List<ActivityInstanceEntity> GetActivityMulitipleInstanceWithState(string mainActivityInstanceId,
            string processInstanceId,
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
        public IList<ActivityInstanceEntity> GetActivityMulitipleInstanceWithStateBatch(SQLBuilder kit, string processInstanceId,
            string mainActivityInstanceId,
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
        public List<ActivityInstanceEntity> GetActivityMulitipleInstanceWithState(SQLBuilder kit, string mainActivityInstanceId,
            string processInstanceId,
            short? activityState)
        {
            var clip = kit.useClip();
            clip.from<ActivityInstanceEntity>(out var ai)
                .where(() => ai.MainActInstanceId, mainActivityInstanceId)
                .where(() => ai.ProcessInstanceId, processInstanceId);
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
        public int GetGatewayInstanceCountByTransition(SQLBuilder kit, string splitActivityId,
            string splitActivityInstanceId,
            string processInstanceId)
        {
            var clip = kit.useClip();
            clip.from<TrackEntity>(out var ti)
                .where(() => ti.ProcessInstanceId, processInstanceId)
                .where(() => ti.FromActId, splitActivityId)
                .where(() => ti.FromActInstanceId, splitActivityInstanceId);
            var list = clip.select(ti).queryList().ToList();
            return list.Count();
        }

        /// <summary>
        /// Obtain the number of valid branches
        ///  获取有效的分支数目
        /// </summary>
        public List<ActivityInstanceEntity> GetValidSplitedActivityInstanceList(SQLBuilder kit, string processInstanceId,
            string splitGatewayInstanceId)
        {
            var clip = kit.useClip();
            clip.from<ActivityInstanceEntity>(out var ai)
                .InnerJoin<TrackEntity>(out var ti).on(() => ai.ActivityInstanceId == ti.ToActInstanceId)
                .whereIn(() => ai.ActivityState, new short?[]
                {
                    (short)ActivityStateEnum.Ready,
                    (short)ActivityStateEnum.Running,
                    (short)ActivityStateEnum.Completed,
                    (short)ActivityStateEnum.Suspended
                })
                .where(() => ti.FromActInstanceId, splitGatewayInstanceId);
            return clip.select(ai).queryList().ToList();
        }

        /// <summary>
        /// Obtain the number of valid branches
        /// 获取有效的子节点列表
        /// </summary>
        public List<ActivityInstanceEntity> GetValidActivityInstanceListOfMI(string mainActivityInstanceId,
            string processInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetValidActivityInstanceListOfMI(kit, mainActivityInstanceId, processInstanceId);
        }

        /// <summary>
        /// Obtain the number of valid branches
        /// 获取有效的子节点列表
        /// </summary>
        public List<ActivityInstanceEntity> GetValidActivityInstanceListOfMI(SQLBuilder kit, string mainActivityInstanceId,
            string processInstanceId)
        {
            var clip = kit.useClip();
            clip.from<ActivityInstanceEntity>(out var ai)
                .whereIn(() => ai.ActivityState, new short?[]
                {
                    (short)ActivityStateEnum.Ready,
                    (short)ActivityStateEnum.Running,
                    (short)ActivityStateEnum.Completed,
                    (short)ActivityStateEnum.Suspended
                })
                .where(() => ai.MainActInstanceId, mainActivityInstanceId)
                .where(() => ai.ProcessInstanceId, processInstanceId);
            var list = clip.select(ai).queryList().ToList();

            var backList = new List<string>();
            foreach (var child in list)
            {
                if (!string.IsNullOrWhiteSpace(child.BackSrcActInstanceId))
                {
                    if (backList.Any(a => a == child.BackSrcActInstanceId) == false)
                        backList.Add(child.BackSrcActInstanceId);
                }

                if (!string.IsNullOrWhiteSpace(child.BackOrgActInstanceId))
                {
                    if (backList.Any(a => a == child.BackOrgActInstanceId) == false)
                        backList.Add(child.BackOrgActInstanceId);
                }
            }

            if (backList.Count > 0)
            {
                string[] backArray = backList.ToArray();
                var validList = list.Where(a => !backArray.Contains(a.ActivityInstanceId)).ToList();
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
        public ActivityInstanceEntity CreateActivityInstanceObject(string bizChannel,
            string bizPK,
            string bizCode,
            string processId,
            string processInstanceId,
            Activity activity,
            FlowCommandContext runner)
        {
            ActivityInstanceEntity instance = new ActivityInstanceEntity();
            instance.ActivityId = activity.ActivityId;
            instance.ActivityName = activity.ActivityName;
            instance.ActivityCode = activity.ActivityCode;
            instance.ActivityType = (short)activity.ActivityType;
            instance.WorkItemType = (short)activity.WorkItemType;
            instance.GateWayTypeId = activity.GatewayDetail != null ? (short)activity.GatewayDetail.DirectionType : null;
            instance.ProcessId = processId;
            instance.BizChannel = bizChannel;
            instance.BizPK = bizPK;
            instance.BizCode = bizCode;
            instance.ProcessInstanceId = processInstanceId;
            instance.TokensRequired = 1;
            instance.TokensHad = 1;
            instance.CreatedUserId = runner.UserId;
            instance.CreatedUserName = runner.UserName;
            instance.CreatedDateTime = System.DateTime.UtcNow;
            instance.DueAt = CalculateActivityOverdueDateTime(activity.BoundaryList, instance.CreatedDateTime.Value);
            instance.ActivityState = (short)ActivityStateEnum.Ready;
            instance.CanNotRenewInstance = 0;
            ApplyLeaveSnapshotFromActivity(instance, activity);

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
        public ActivityInstanceEntity CreateActivityInstanceObject(ActivityInstanceEntity main)
        {
            ActivityInstanceEntity instance = new ActivityInstanceEntity();
            instance.ActivityId = main.ActivityId;
            instance.ActivityName = main.ActivityName;
            instance.ActivityCode = main.ActivityCode;
            instance.ActivityType = main.ActivityType;
            instance.WorkItemType = main.WorkItemType;
            instance.GateWayTypeId = main.GateWayTypeId;
            instance.ProcessId = main.ProcessId;
            instance.BizChannel = main.BizChannel;
            instance.BizPK = main.BizPK;
            instance.BizCode = main.BizCode;
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
        public ActivityInstanceEntity CreateBackwardActivityInstanceObject(string bizChannel,
            string bizPK,
            string bizCode,
            string processInstanceId,
            string processId,
            Activity activity,
            BackwardTypeEnum backwardType,
            string backSrcActivityInstanceId,
            string backOrgActivityInstanceId,
            FlowCommandContext runner)
        {
            ActivityInstanceEntity instance = new ActivityInstanceEntity();
            instance.ActivityId = activity.ActivityId;
            instance.ActivityName = activity.ActivityName;
            instance.ActivityCode = activity.ActivityCode;
            instance.ActivityType = (short)activity.ActivityType;
            instance.WorkItemType = (short)activity.WorkItemType;
            instance.GateWayTypeId = activity.GatewayDetail != null ? (short)activity.GatewayDetail.DirectionType : null;
            instance.ProcessId = processId;
            instance.BizChannel = bizChannel;
            instance.BizPK = bizPK;
            instance.BizCode = bizCode;
            instance.ProcessInstanceId = processInstanceId;
            instance.BackwardType = (short)backwardType;
            instance.BackSrcActInstanceId = backSrcActivityInstanceId;
            instance.BackOrgActInstanceId = backOrgActivityInstanceId;
            instance.TokensRequired = 1;
            instance.TokensHad = 1;
            instance.CreatedUserId = runner.UserId;
            instance.CreatedUserName = runner.UserName;
            instance.CreatedDateTime = System.DateTime.UtcNow;
            instance.ActivityState = (short)ActivityStateEnum.Ready;
            instance.CanNotRenewInstance = 0;
            ApplyLeaveSnapshotFromActivity(instance, activity);

            return instance;
        }

        /// <summary>
        /// 从设计 Activity 拷贝 Leave 快照到 AI（加签只改发起人容器 Leave，不改节点快照）。
        /// </summary>
        public static void ApplyLeaveSnapshotFromActivity(ActivityInstanceEntity instance, Activity activity)
        {
            if (instance == null || activity == null)
                return;
            instance.LeaveJudgeType = activity.LeaveJudgeType;
            instance.LeaveJudgeParams = activity.LeaveJudgeParams;
        }

        /// <summary>
        /// Update the number of tokens for the activity node
        /// 更新活动节点的Token数目
        /// </summary>
        public void IncreaseTokensHad(SQLBuilder kit, string activityInstanceId,
            FlowCommandContext runner)
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
                var clip = kit.useClip();
                clip.setTable<ActivityInstanceEntity>(out var ai)
                    .set(() => ai.TokensHad, next)
                    .set(() => ai.UpdatedDateTime, DateTime.UtcNow)
                    .where(() => ai.ActivityInstanceId, activityInstanceId);
                if (prev == null)
                    clip.where(() => ai.TokensHad == null);
                else
                    clip.where(() => ai.TokensHad, prev);

                if (clip.doUpdate() > 0)
                    return;
            }

            throw FailThrow(new WorkflowException(
                "IncreaseTokensHad CAS failed after retries. ActivityInstanceId=" + activityInstanceId), "IncreaseTokensHad");
        }
        #endregion

        #region Activity instance status setting 活动实例状态设置
        /// <summary>
        /// Activity instance read
        /// 活动实例被读取
        /// </summary>
        public void Read(SQLBuilder kit, string activityInstanceId,
            FlowCommandContext runner)
        {
            SetActivityState(kit, activityInstanceId, ActivityStateEnum.Running, runner);
        }

        /// <summary>
        /// Set activity instance status
        /// 设置活动实例状态
        /// </summary>
        private void SetActivityState(SQLBuilder kit, string activityInstanceId,
            ActivityStateEnum activityState,
            FlowCommandContext runner)
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
        public void Withdraw(SQLBuilder kit, string activityInstanceId,
            FlowCommandContext runner)
        {
            EndActivityState(kit, activityInstanceId, ActivityStateEnum.Withdrawed, runner);
        }

        /// <summary>
        /// Sendback activity instance
        /// 退回活动实例
        /// </summary>
        public void SendBack(SQLBuilder kit, string activityInstanceId,
            FlowCommandContext runner)
        {
            EndActivityState(kit, activityInstanceId, ActivityStateEnum.Sendbacked, runner);
        }

        /// <summary>
        /// Set the end status of the activity
        /// 设置活动结束状态
        /// </summary>
        private void EndActivityState(SQLBuilder kit, string activityInstanceId,
            ActivityStateEnum activityState,
            FlowCommandContext runner)
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
        public void Complete(SQLBuilder kit, string activityInstanceId,
            FlowCommandContext runner)
        {
            EndActivityState(kit, activityInstanceId, ActivityStateEnum.Completed, runner);
        }

        public void Complete(SQLBuilder kit, ActivityInstanceEntity activityInstance,
            FlowCommandContext runner)
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
        public void SetApprovalStatus(SQLBuilder kit, string activityInstanceId)
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
        public void UpdateActivityInstanceBlockedBetweenSplitJoin(SQLBuilder kit, Activity gatewayActivity,
            ActivityInstanceEntity gatewayInstance,
            IProcessModel processModel)
        {
            var joinCount = 0;
            var splitCount = 0;
            var splitActivity = processModel.GetBackwardGatewayActivity(gatewayActivity, ref joinCount, ref splitCount);
            var taskActivityList = processModel.GetAllTaskActivityList(splitActivity, gatewayActivity);

            UpdateActivityInstanceBlockedBetweenSplitJoin(kit, gatewayInstance.ProcessInstanceId, taskActivityList);
        }

        /// <summary>
        /// Update the activities in the activity to block status
        /// 更新活动里面的活动为阻止状态
        /// </summary>
        private void UpdateActivityInstanceBlockedBetweenSplitJoin(SQLBuilder kit, string processInstanceId,
            IList<Activity> taskActivityList)
        {
            var idsin = taskActivityList.Select(a => a.ActivityId).ToList();
            var clip = kit.useClip();
            clip.setTable<ActivityInstanceEntity>(out var ai)
                .set(() => ai.CanNotRenewInstance, (byte)1)
                .where(() => ai.ProcessInstanceId, processInstanceId)
                .whereIn(() => ai.ActivityState, new short?[]
                {
                    (short)ActivityStateEnum.Ready,
                    (short)ActivityStateEnum.Running,
                    (short)ActivityStateEnum.Suspended
                })
                .whereIn(() => ai.ActivityId, idsin)
                .doUpdate();
        }
        #endregion

        #region Activity instance record maintenance 活动实例记录维护
        /// <summary>
        /// Insert activity instance
        /// 插入活动实例
        /// </summary>
        public string Insert(ActivityInstanceEntity entity)
        {
            var kit = Sail.UseRunSQL();
            return Insert(kit, entity);
        }

        public string Insert(SQLBuilder kit, ActivityInstanceEntity entity)
        {
            if (entity.ActivityType == (short)ActivityTypeEnum.GatewayNode
                && string.IsNullOrEmpty(entity.ActivityName))
            {
                entity.ActivityName = "GATEWAY";
            }

            string newId = kit.useRepo<ActivityInstanceEntity, string>().InsertReturnId(entity);
            entity.ActivityInstanceId = newId;

            return newId;
        }

        /// <summary>
        /// Update activity instance
        /// 更新活动实例
        /// </summary>
        public void Update(ActivityInstanceEntity entity)
        {
            var kit = Sail.UseRunSQL();
            Update(kit, entity);
        }

        public void Update(SQLBuilder kit, ActivityInstanceEntity entity)
            => kit.useRepo<ActivityInstanceEntity>().Update(entity);

        /// <summary>
        /// Cancel activity instance
        /// 取消节点运行
        /// </summary>
        public void Cancel(SQLBuilder kit, string activityInstanceId,
            FlowCommandContext runner)
        {
            var clip = kit.useClip();
            clip.setTable<ActivityInstanceEntity>(out var ai)
                .set(() => ai.ActivityState, (short)ActivityStateEnum.Cancelled)
                .set(() => ai.UpdatedUserId, runner.UserId)
                .set(() => ai.UpdatedUserName, runner.UserName)
                .set(() => ai.UpdatedDateTime, System.DateTime.UtcNow)
                .where(() => ai.ActivityInstanceId, activityInstanceId)
                .whereIn(() => ai.ActivityState, new short?[]
                {
                    (short)ActivityStateEnum.Ready,
                    (short)ActivityStateEnum.Running,
                    (short)ActivityStateEnum.Suspended
                })
                .doUpdate();
        }

        /// <summary>
        /// 取消主下未完成子 AI，并<strong>递归</strong>取消其后代（加签嵌套子树）。
        /// </summary>
        public void CancelUnCompletedMultipleInstance(SQLBuilder kit, string mainActivityInstanceId, FlowCommandContext runner)
        {
            CancelUnCompletedMultipleInstanceRecursive(kit, mainActivityInstanceId, runner, depth: 0);
        }

        /// <summary>
        /// 淹没节点下全部未完成 SignForward 容器及其子树（Race 旁关时调用）。
        /// </summary>
        public void CancelSignForwardSubtreeUnder(SQLBuilder kit, string rootActivityInstanceId, FlowCommandContext runner)
        {
            if (string.IsNullOrWhiteSpace(rootActivityInstanceId))
                return;
            var root = GetById(kit, rootActivityInstanceId);
            if (root == null || string.IsNullOrWhiteSpace(root.ProcessInstanceId))
                return;

            var direct = GetActivityMulitipleInstanceWithState(
                    kit, rootActivityInstanceId, root.ProcessInstanceId, null)
                .Where(c => c != null
                    && (c.ActivityState == (short)ActivityStateEnum.Ready
                        || c.ActivityState == (short)ActivityStateEnum.Running
                        || c.ActivityState == (short)ActivityStateEnum.Suspended))
                .ToList();

            foreach (var child in direct)
            {
                var isSfContainer = child.ComplexType == (short)ComplexTypeEnum.SignForward
                    && !string.IsNullOrEmpty(child.LeaveJudgeType);
                if (isSfContainer || child.ComplexType == (short)ComplexTypeEnum.SignForward)
                {
                    CancelUnCompletedMultipleInstanceRecursive(kit, child.ActivityInstanceId, runner, 0);
                    if (child.ActivityState == (short)ActivityStateEnum.Ready
                        || child.ActivityState == (short)ActivityStateEnum.Running
                        || child.ActivityState == (short)ActivityStateEnum.Suspended)
                    {
                        child.ActivityState = (short)ActivityStateEnum.Cancelled;
                        child.UpdatedUserId = runner?.UserId;
                        child.UpdatedUserName = runner?.UserName;
                        child.UpdatedDateTime = System.DateTime.UtcNow;
                        Update(kit, child);
                        Sail.ManagerFactory.Tasks.CloseOpenApproverTasksOnActivityIds(
                            kit,
                            root.ProcessInstanceId,
                            new[] { child.ActivityInstanceId },
                            runner,
                            TaskEndTypes.RaceTaken);
                    }
                }
            }
        }

        private void CancelUnCompletedMultipleInstanceRecursive(
            SQLBuilder kit,
            string mainActivityInstanceId,
            FlowCommandContext runner,
            int depth)
        {
            if (string.IsNullOrWhiteSpace(mainActivityInstanceId) || depth > 32)
                return;

            var main = GetById(kit, mainActivityInstanceId);
            var processInstanceId = main?.ProcessInstanceId;

            var openChildren = string.IsNullOrWhiteSpace(processInstanceId)
                ? new List<ActivityInstanceEntity>()
                : GetActivityMulitipleInstanceWithStateBatch(
                        kit,
                        processInstanceId,
                        mainActivityInstanceId,
                        null)
                    .Where(c => c != null
                        && (c.ActivityState == (short)ActivityStateEnum.Ready
                            || c.ActivityState == (short)ActivityStateEnum.Running
                            || c.ActivityState == (short)ActivityStateEnum.Suspended))
                    .ToList();

            foreach (var child in openChildren)
                CancelUnCompletedMultipleInstanceRecursive(kit, child.ActivityInstanceId, runner, depth + 1);

            var clip = kit.useClip();
            clip.setTable<ActivityInstanceEntity>(out var ai)
                .set(() => ai.ActivityState, (short)ActivityStateEnum.Cancelled)
                .set(() => ai.UpdatedUserId, runner.UserId)
                .set(() => ai.UpdatedUserName, runner.UserName)
                .set(() => ai.UpdatedDateTime, System.DateTime.UtcNow)
                .where(() => ai.MainActInstanceId, mainActivityInstanceId)
                .whereIn(() => ai.ActivityState, new short?[]
                {
                    (short)ActivityStateEnum.Ready,
                    (short)ActivityStateEnum.Running,
                    (short)ActivityStateEnum.Suspended
                })
                .doUpdate();

            if (!string.IsNullOrWhiteSpace(processInstanceId) && openChildren.Count > 0)
            {
                Sail.ManagerFactory.Tasks.CloseOpenApproverTasksOnActivityIds(
                    kit,
                    processInstanceId,
                    openChildren.Select(c => c.ActivityInstanceId),
                    runner,
                    TaskEndTypes.RaceTaken);
            }
        }

        /// <summary>
        /// Withdraw the master node and its multiple instance nodes below it
        /// 撤销主节点及其下面的多实例子节点
        /// </summary>
        public void WithdrawMainIncludedChildNodes(SQLBuilder kit, string mainActivityInstanceId, FlowCommandContext runner)
        {
            SetActivityState(kit, mainActivityInstanceId, ActivityStateEnum.Withdrawed, runner);
            WithdrawMultipleInstance(kit, mainActivityInstanceId, runner);
        }

        /// <summary>
        /// Update the node status of the counter signature processing node
        /// 更新会签办理节点的节点状态
        /// </summary>
        private void WithdrawMultipleInstance(SQLBuilder kit, string mainActivityInstanceId, FlowCommandContext runner)
        {
            var clip = kit.useClip();
            clip.setTable<ActivityInstanceEntity>(out var ai)
                .set(() => ai.ActivityState, (short)ActivityStateEnum.Withdrawed)
                .set(() => ai.UpdatedUserId, runner.UserId)
                .set(() => ai.UpdatedUserName, runner.UserName)
                .set(() => ai.UpdatedDateTime, System.DateTime.UtcNow)
                .where(() => ai.MainActInstanceId, mainActivityInstanceId)
                .whereIn(() => ai.ActivityState, new short?[]
                {
                    (short)ActivityStateEnum.Ready,
                    (short)ActivityStateEnum.Suspended
                })
                .doUpdate();
        }

        /// <summary>
        /// Put the node back in a suspended state
        /// 重新使节点处于挂起状态
        /// </summary>
        public void Resuspend(SQLBuilder kit, string activityInstanceId, FlowCommandContext runner)
        {
            SetActivityState(kit, activityInstanceId, ActivityStateEnum.Suspended, runner);
        }

        /// <summary>
        /// Put the node back in a running state
        /// 重新使节点处于运行状态
        /// </summary>
        public void Rerun(SQLBuilder kit, string activityInstanceId, FlowCommandContext runner)
        {
            SetActivityState(kit, activityInstanceId, ActivityStateEnum.Running, runner);
        }

        /// <summary>
        /// Delete activity instance
        /// 删除活动实例
        /// </summary>
        public void Delete(string activityInstanceId)
        {
            var kit = Sail.UseRunSQL();
            Delete(kit, activityInstanceId);
        }

        public void Delete(SQLBuilder kit, string activityInstanceId)
            => kit.useRepo<ActivityInstanceEntity, string>().DeleteById(activityInstanceId);
        #endregion

        #region Activity Instance Approval Status 活动实例审批状态
        /// <summary>
        /// Agree
        /// 同意
        /// </summary>
        public void Agree(string taskId)
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

        public void Agree(SQLBuilder kit, string taskId)
        {
            var activityInstance = GetByTask(kit, taskId);
            activityInstance.ApprovalStatus = (short)ApprovalStatusEnum.Agreed;
            Update(kit, activityInstance);
        }

        /// <summary>
        /// Refuse
        /// 拒绝
        /// </summary>
        public void Refuse(string taskId)
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

        public void Refuse(SQLBuilder kit, string taskId)
        {
            var activityInstance = GetByTask(kit, taskId);
            activityInstance.ApprovalStatus = (short)ApprovalStatusEnum.Refused;
            Update(kit, activityInstance);
        }

        /// <summary>
        /// Check the pass type of the current node
        ///  检查当前节点的通过类型
        /// </summary>
        public NodePassedResult CheckActivityInstancePassedResult(SQLBuilder kit, string activityInstanceId)
        {
            var result = NodePassedResult.Create(NodePassedTypeEnum.Default);
            if (activityInstanceId != null)
            {
                var activityInstance = GetById(kit, activityInstanceId);
                if (IsMultipleInstanceChildren(activityInstance) == true)
                {
                    var mainActivityInstance = GetById(kit, activityInstance.MainActInstanceId);
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
        public NodePassedTypeEnum CheckMIPassRateInfo(SQLBuilder kit, ActivityInstanceEntity currentActivityInstance,
            ActivityInstanceEntity mainActivityInstance)
        {
            return EvaluateLeaveOnActivity(kit, currentActivityInstance, mainActivityInstance);
        }

        /// <summary>
        /// 对主体 AI 做 Leave：seats = 直属 Task ∪ 直属子 AI 折合（同级）。
        /// </summary>
        public NodePassedTypeEnum EvaluateLeaveOnActivity(
            SQLBuilder kit,
            ActivityInstanceEntity currentActivityInstance,
            ActivityInstanceEntity subjectActivityInstance)
        {
            if (subjectActivityInstance == null)
                return NodePassedTypeEnum.Default;

            var childActivityInstanceList = GetValidActivityInstanceListOfMI(
                kit,
                subjectActivityInstance.ActivityInstanceId,
                subjectActivityInstance.ProcessInstanceId);

            var pipeline = Sail.AssigneePipeline;
            string leaveProcessId = subjectActivityInstance.ProcessId;
            string leaveVersion = null;
            try
            {
                var pi = Sail.ProcessInstances.GetById(
                    kit, subjectActivityInstance.ProcessInstanceId);
                leaveProcessId = pi?.ProcessId ?? leaveProcessId;
                leaveVersion = pi?.Version;
            }
            catch
            {
                /* 读版本失败时仍尝试 Leave */
            }

            if (pipeline != null)
            {
                var childIds = childActivityInstanceList?
                    .Select(c => c?.ActivityInstanceId)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .ToList() ?? new List<string>();
                if (!string.IsNullOrWhiteSpace(currentActivityInstance?.ActivityInstanceId)
                    && !childIds.Contains(currentActivityInstance.ActivityInstanceId)
                    && !string.Equals(
                        currentActivityInstance.ActivityInstanceId,
                        subjectActivityInstance.ActivityInstanceId,
                        StringComparison.OrdinalIgnoreCase))
                    childIds.Add(currentActivityInstance.ActivityInstanceId);

                var directTasks = Sail.ManagerFactory.Tasks.GetTaskListByActivity(
                    kit,
                    subjectActivityInstance.ProcessInstanceId,
                    subjectActivityInstance.ActivityInstanceId);
                var childTasks = Sail.ManagerFactory.Tasks.GetTaskListByActivityIds(
                    kit,
                    subjectActivityInstance.ProcessInstanceId,
                    childIds);
                var seats = LeaveSeatProjector.BuildSeatsForActivity(
                    subjectActivityInstance,
                    directTasks,
                    childActivityInstanceList,
                    childTasks);

                if (pipeline.TryCanLeave(
                        new LeaveInvokeRequest
                        {
                            ProcessInstanceId = subjectActivityInstance.ProcessInstanceId,
                            ActivityInstanceId = currentActivityInstance?.ActivityInstanceId
                                ?? subjectActivityInstance.ActivityInstanceId,
                            ProcessId = leaveProcessId,
                            Version = leaveVersion,
                            CurrentActivityInstance = currentActivityInstance ?? subjectActivityInstance,
                            MainActivityInstance = subjectActivityInstance,
                            ChildActivityInstances = childActivityInstanceList,
                            Seats = seats,
                            Kit = kit
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

                throw FailThrow(new WfAssigneeException(
                    AssigneeReasonCodes.LeaveConfigMissing,
                    "LeaveJudge missing for activity '"
                    + (subjectActivityInstance.ActivityId ?? subjectActivityInstance.ActivityInstanceId?.ToString())
                    + "'. Configure LeaveJudge in designer (R6). ReasonCode="
                    + AssigneeReasonCodes.LeaveConfigMissing), "LeaveJudge");
            }

            _ = Sail.GetAssigneePipelineRequired();
            return NodePassedTypeEnum.Default;
        }

        /// <summary>是否为加签席位容器（发起人 AI，承载子规则 Leave）。</summary>
        public static bool IsSignForwardContainer(ActivityInstanceEntity ai)
            => ai != null
               && ai.ComplexType == (short)ComplexTypeEnum.SignForward
               && !string.IsNullOrEmpty(ai.LeaveJudgeType);

        /// <summary>
        /// Obtain the approval rate type of the countersignature node
        /// 获取会签节点的审批通过率类型
        /// </summary>
        public Boolean GetMiApprovalThresholdStatus(SQLBuilder kit, ActivityInstanceEntity currentActivityInstance,
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
