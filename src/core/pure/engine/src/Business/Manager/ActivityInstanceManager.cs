
using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using saiflow.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.SqlProvider;
using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.engine.Xpdl;
using saiflow.engine.Xpdl.Common;
using saiflow.engine.Xpdl.Entity;
using saiflow.hub.Localize;

namespace saiflow.engine.Business.Manager
{
    /// <summary>
    /// Activity Instance Manager
    /// 活动实例管理类
    /// </summary>
    internal class ActivityInstanceManager
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
        internal ActivityInstanceEntity GetById(int activityInstanceId)
        {
            try
            {
                return Sail.UseRunSQL().useRepo<ActivityInstanceEntity, int>().GetById(activityInstanceId);
            }
            catch (System.Exception e)
            {
                throw new ApplicationException(LocalizeHelper.GetEngineMessage("activityinstancemanager.getbyid.error", e.Message),
                    e);
            }
        }

        internal ActivityInstanceEntity GetById(SQLBuilder kit, int activityInstanceId)
            => Sibling(kit).useRepo<ActivityInstanceEntity, int>().GetById(activityInstanceId);

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

            var tm = new TaskManager(Sail);
            ActivityInstanceEntity runningNode = null;
            if (taskId != null && taskId.Value > 0)
            {
                taskView = tm.GetTaskView(kit, taskId.Value);
                runningNode = GetById(kit, taskView.ActivityInstanceId);
                return runningNode;
            }

            var activityInstanceList = GetRunningActivityInstanceList(kit, runner.AppInstanceId, runner.ProcessId, runner.Version).ToList();
            if (activityInstanceList == null || activityInstanceList.Count == 0)
            {
                var message = LocalizeHelper.GetEngineMessage("activityinstancemanager.getrunningnode.error");
                var e = new WorkflowException(message);
                LogManager.RecordLog(Sail, message, LogEventType.Exception, LogPriority.Normal, null, e);
                throw e;
            }

            if (activityInstanceList.Count == 1)
            {
                runningNode = activityInstanceList[0];
                taskView = tm.GetTaskOfMineByActivityInstance(kit, runningNode.Id, runner.UserId);
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
                    taskView = tm.GetTaskOfMineByActivityInstance(kit, runningNode.Id, runner.UserId);
                }
                else
                {
                    var msgException = LocalizeHelper.GetEngineMessage("activityinstancemanager.getrunningnode.unique.error");
                    var e = new WorkflowException(msgException);
                    LogManager.RecordLog(Sail, LocalizeHelper.GetEngineMessage("activityinstancemanager.getrunningnode.error"),
                        LogEventType.Exception, LogPriority.Normal, null, e);
                    throw e;
                }
            }
            return runningNode;
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
            return Sibling(kit)
                .select("*")
                .from(SailSqlDialect.ActivityInstanceTable)
                .where("process_instance_id", processInstanceId)
                .orderBy("id")
                .query<ActivityInstanceEntity>()
                .ToList();
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
            ActivityInstanceEntity activityInstance = null;
            var list = Sibling(kit)
                .select("*")
                .from(SailSqlDialect.ActivityInstanceTable)
                .where("process_instance_id", processInstanceId)
                .where("activity_id", activityId)
                .orderBy("id DESC")
                .query<ActivityInstanceEntity>()
                .ToList();

            if (list.Count > 0)
                activityInstance = list[0];

            return activityInstance;
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
            var list = Sibling(kit)
                .select("AI.*")
                .from($"{SailSqlDialect.ActivityInstanceTable} AI")
                .innerJoin($"{SailSqlDialect.ProcessInstanceTable} PI ON AI.process_instance_id = PI.id")
                .where("PI.process_state", 2)
                .where("AI.app_instance_id", appInstanceId)
                .where("AI.process_id", processId)
                .where("AI.activity_id", activityId)
                .query<ActivityInstanceEntity>()
                .ToList();

            if (list.Count > 0)
            {
                var matchedActivityInstance = list[0];
                return GetById(kit, matchedActivityInstance.Id);
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
            return Sibling(kit)
                .select("AI.*")
                .from($"{SailSqlDialect.ActivityInstanceTable} AI")
                .innerJoin($"{SailSqlDialect.ProcessInstanceTable} PI ON AI.process_instance_id = PI.id")
                .where("PI.process_state", 2)
                .where("PI.process_id", processId)
                .where("PI.version", version)
                .where("AI.activity_id", activityId)
                .where("(AI.activity_state=1 OR AI.activity_state=2)")
                .query<ActivityInstanceEntity>()
                .ToList();
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
            var list = Sibling(kit)
                .select("*")
                .from(SailSqlDialect.ActivityInstanceTable)
                .where("process_instance_id", processInstanceId)
                .where("activity_id", activityId)
                .where("activity_state", (short)activityState)
                .orderBy("id DESC")
                .query<ActivityInstanceEntity>()
                .ToList();
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
        internal List<ActivityInstanceEntity> GetActivityInstanceListCompleted(string appInstanceId,
            string processId)
        {
            var kit = Sail.UseRunSQL();
            return Sibling(kit)
                .select("AI.*")
                .from($"{SailSqlDialect.ActivityInstanceTable} AI")
                .innerJoin($"{SailSqlDialect.ProcessInstanceTable} PI ON AI.process_instance_id = PI.id")
                .where("PI.process_state", 2)
                .where("AI.app_instance_id", appInstanceId)
                .where("AI.process_id", processId)
                .where("AI.activity_state", 4)
                .query<ActivityInstanceEntity>()
                .ToList();
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

            var instanceList = GetActivityInstanceListCompletedSimple(kit, processInstanceId, previousActivityId);

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
            return Sibling(kit)
                .select("*")
                .from(SailSqlDialect.ActivityInstanceTable)
                .where("process_instance_id", processInstanceId)
                .where("activity_id", activityId)
                .where("activity_state", (short)ActivityStateEnum.Completed)
                .orderBy("id DESC")
                .query<ActivityInstanceEntity>()
                .ToList();
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
            var list = Sibling(kit)
                .select("AI.*")
                .from($"{SailSqlDialect.ActivityInstanceTable} AI")
                .innerJoin($"{SailSqlDialect.TaskTable} T ON AI.id = T.activity_instance_id")
                .where("T.id", taskId)
                .query<ActivityInstanceEntity>()
                .ToList();
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
        internal IEnumerable<ActivityInstanceEntity> GetRunningActivityInstanceList(string appInstanceId,
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
            return Sibling(kit)
                .select("AI.*")
                .from($"{SailSqlDialect.ActivityInstanceTable} AI")
                .innerJoin($"{SailSqlDialect.ProcessInstanceTable} PI ON AI.process_instance_id = PI.id")
                .where("(AI.activity_state=1 OR AI.activity_state=2)")
                .where("PI.process_state", 2)
                .where("PI.version", version)
                .where("AI.app_instance_id", appInstanceId)
                .where("AI.process_id", processId)
                .query<ActivityInstanceEntity>();
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
            return Sibling(kit)
                .select("AI.*")
                .from($"{SailSqlDialect.ActivityInstanceTable} AI")
                .innerJoin($"{SailSqlDialect.ProcessInstanceTable} PI ON AI.process_instance_id = PI.id")
                .where("PI.process_state", 2)
                .where("AI.main_activity_instance_id", mainActivityInstanceId)
                .where("AI.activity_state", 4)
                .query<ActivityInstanceEntity>()
                .ToList();
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

            var instanceList = Sibling(kit)
                .select("*")
                .from(SailSqlDialect.ActivityInstanceTable)
                .where("process_instance_id", runningNode.ProcessInstanceId)
                .where("activity_id", previousActivityId)
                .where("activity_state", 4)
                .orderBy("id DESC")
                .query<ActivityInstanceEntity>()
                .ToList();

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
        internal Boolean IsMultipleInstanceChildren(ActivityInstanceEntity child)
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
        internal List<ActivityInstanceEntity> GetActivityMulitipleInstanceWithState(int mainActivityInstanceId,
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
            var query = Sibling(kit)
                .select("*")
                .from(SailSqlDialect.ActivityInstanceTable)
                .where("main_activity_instance_id", mainActivityInstanceId)
                .where("process_instance_id", processInstanceId);
            if (activityState.HasValue)
            {
                query.where("activity_state", activityState.Value);
            }
            return query.orderBy("complete_order")
                .query<ActivityInstanceEntity>()
                .ToList();
        }

        /// <summary>
        /// Query the number of branch instances
        /// 查询分支实例的个数
        /// </summary>
        internal int GetGatewayInstanceCountByTransition(SQLBuilder kit, string splitActivityId,
            int splitActivityInstanceId,
            int processInstanceId)
        {
            var list = Sibling(kit)
                .select("*")
                .from(SailSqlDialect.TransitionInstanceTable)
                .where("process_instance_id", processInstanceId)
                .where("from_activity_id", splitActivityId)
                .where("from_activity_instance_id", splitActivityInstanceId)
                .query<TransitionInstanceEntity>()
                .ToList();
            return list.Count();
        }

        /// <summary>
        /// Obtain the number of valid branches
        ///  获取有效的分支数目
        /// </summary>
        internal List<ActivityInstanceEntity> GetValidSplitedActivityInstanceList(SQLBuilder kit, int processInstanceId,
            int splitGatewayInstanceId)
        {
            return Sibling(kit)
                .select("A.*")
                .from($"{SailSqlDialect.ActivityInstanceTable} A")
                .innerJoin($"{SailSqlDialect.TransitionInstanceTable} T ON A.id = T.to_activity_instance_id")
                .where("(A.activity_state=1 OR A.activity_state=2 OR A.activity_state=4 OR A.activity_state=5)")
                .where("T.from_activity_instance_id", splitGatewayInstanceId)
                .query<ActivityInstanceEntity>()
                .ToList();
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
            var list = Sibling(kit)
                .select("*")
                .from(SailSqlDialect.ActivityInstanceTable)
                .where("(activity_state=1 OR activity_state = 2 OR activity_state = 4 OR activity_state = 5)")
                .where("main_activity_instance_id", mainActivityInstanceId)
                .where("process_instance_id", processInstanceId)
                .query<ActivityInstanceEntity>()
                .ToList();

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
                var validList = list.Where(a => !backArray.Contains(a.Id)).ToList();
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
            instance.OverdueDateTime = CalculateActivityOverdueDateTime(activity.BoundaryList, instance.CreatedDateTime);
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
            var activityInstance = GetById(kit, activityInstanceId);
            activityInstance.TokensHad += 1;
            Update(kit, activityInstance);
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

            UpdateActivityInstanceBlockedBetweenSplitJoin(kit, gatewayInstance.ProcessInstanceId, taskActivityList);
        }

        /// <summary>
        /// Update the activities in the activity to block status
        /// 更新活动里面的活动为阻止状态
        /// </summary>
        private void UpdateActivityInstanceBlockedBetweenSplitJoin(SQLBuilder kit, int processInstanceId,
            IList<Activity> taskActivityList)
        {
            var idsin = taskActivityList.Select(a => a.ActivityId).ToList();
            Sibling(kit)
                .setTable(SailSqlDialect.ActivityInstanceTable)
                .set("cannot_renew_instance", 1)
                .where("process_instance_id", processInstanceId)
                .whereIn("activity_state", 1, 2, 5)
                .whereIn("activity_id", idsin)
                .doUpdate();
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

            int newId = Sibling(kit).useRepo<ActivityInstanceEntity, int>().InsertWithPK(entity);
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
            Sibling(kit)
                .setTable(SailSqlDialect.ActivityInstanceTable)
                .set("activity_state", (short)ActivityStateEnum.Cancelled)
                .set("updated_user_id", runner.UserId)
                .set("updated_user_name", runner.UserName)
                .set("updated_datetime", System.DateTime.UtcNow)
                .where("id", activityInstanceId)
                .where("(activity_state=1 OR activity_state=2 OR activity_state=5)")
                .doUpdate();
        }

        /// <summary>
        /// Update the status of unfinished nodes in the countersignature node
        /// 更新会签节点中未办理完成的节点状态
        /// </summary>
        internal void CancelUnCompletedMultipleInstance(SQLBuilder kit, int mainActivityInstanceId, WfAppRunner runner)
        {
            Sibling(kit)
                .setTable(SailSqlDialect.ActivityInstanceTable)
                .set("activity_state", (short)ActivityStateEnum.Cancelled)
                .set("updated_user_id", runner.UserId)
                .set("updated_user_name", runner.UserName)
                .set("updated_datetime", System.DateTime.UtcNow)
                .where("main_activity_instance_id", mainActivityInstanceId)
                .where("(activity_state=1 OR activity_state=2 OR activity_state=5)")
                .doUpdate();
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
            Sibling(kit)
                .setTable(SailSqlDialect.ActivityInstanceTable)
                .set("activity_state", (short)ActivityStateEnum.Withdrawed)
                .set("updated_user_id", runner.UserId)
                .set("updated_user_name", runner.UserName)
                .set("updated_datetime", System.DateTime.UtcNow)
                .where("main_activity_instance_id", mainActivityInstanceId)
                .where("(activity_state=1 OR activity_state=5)")
                .doUpdate();
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
                    result = NodePassedResult.CreateByApprovalStatus(activityInstance.ApprovalStatus);
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
            var passedType = NodePassedTypeEnum.Default;
            var childActivityInstanceList = GetValidActivityInstanceListOfMI(kit, mainActivityInstance.Id, mainActivityInstance.ProcessInstanceId);

            var agreedCount = childActivityInstanceList.Where(a => a.ApprovalStatus == (short)ApprovalStatusEnum.Agreed).Count();
            var refusedCount = childActivityInstanceList.Where(a => a.ApprovalStatus == (short)ApprovalStatusEnum.Refused).Count();

            if (mainActivityInstance.MergeType == (short)MergeTypeEnum.Sequence)
            {
                if (mainActivityInstance.CompareType.Value == (short)CompareTypeEnum.Count)
                {
                    var thresholdCount = mainActivityInstance.CompleteOrder.Value;
                    var allCount = childActivityInstanceList.Max(a => a.CompleteOrder).Value;
                    var negativeCount = allCount - thresholdCount;

                    if (refusedCount > negativeCount)
                    {
                        passedType = NodePassedTypeEnum.NotPassed;
                    }
                    else if (agreedCount >= thresholdCount)
                    {
                        passedType = NodePassedTypeEnum.Passed;
                    }
                    else
                    {
                        passedType = NodePassedTypeEnum.NeedToBeMoreApproved;
                    }
                    return passedType;
                }
                else if (mainActivityInstance.CompareType.Value == (short)CompareTypeEnum.Percentage)
                {
                    var thresholdPercentage = mainActivityInstance.CompleteOrder.Value;
                    var allCount = childActivityInstanceList.Count;
                    var negativePercentage = 1 - thresholdPercentage;

                    if ((refusedCount * 0.01) / (allCount * 0.01) > negativePercentage)
                    {
                        passedType = NodePassedTypeEnum.NotPassed;
                    }
                    else if ((agreedCount * 0.01) / (allCount * 0.01) >= thresholdPercentage)
                    {
                        passedType = NodePassedTypeEnum.Passed;
                    }
                    else
                    {
                        passedType = NodePassedTypeEnum.NeedToBeMoreApproved;
                    }
                    return passedType;
                }
                else
                {
                    var msgException = LocalizeHelper.GetEngineMessage("activityinstancemanager.CheckPassRateMatchedInfo.UnknownCompareTypeValue");
                    throw new WorkflowException(msgException);
                }
            }
            else if (mainActivityInstance.MergeType == (short)MergeTypeEnum.Parallel)
            {
                if (mainActivityInstance.CompareType.Value == (short)CompareTypeEnum.Count)
                {
                    var thresholdCount = mainActivityInstance.CompleteOrder.Value;
                    var allCount = childActivityInstanceList.Count;
                    var negativeCount = allCount - thresholdCount;

                    if (refusedCount > negativeCount)
                    {
                        passedType = NodePassedTypeEnum.NotPassed;
                    }
                    else if (agreedCount >= thresholdCount)
                    {
                        passedType = NodePassedTypeEnum.Passed;
                    }
                    else
                    {
                        passedType = NodePassedTypeEnum.NeedToBeMoreApproved;
                    }
                    return passedType;
                }
                else if (mainActivityInstance.CompareType.Value == (short)CompareTypeEnum.Percentage)
                {
                    var thresholdPercentage = mainActivityInstance.CompleteOrder.Value;
                    var allCount = childActivityInstanceList.Count;
                    var negativePercentage = 1 - thresholdPercentage;

                    if ((refusedCount * 0.01) / (allCount * 0.01) > negativePercentage)
                    {
                        passedType = NodePassedTypeEnum.NotPassed;
                    }
                    else if ((agreedCount * 0.01) / (allCount * 0.01) >= thresholdPercentage)
                    {
                        passedType = NodePassedTypeEnum.Passed;
                    }
                    else
                    {
                        passedType = NodePassedTypeEnum.NeedToBeMoreApproved;
                    }
                    return passedType;
                }
                else
                {
                    var msgException = LocalizeHelper.GetEngineMessage("activityinstancemanager.CheckPassRateMatchedInfo.UnknownCompareTypeValue");
                    throw new WorkflowException(msgException);
                }
            }
            else
            {
                var msgException = LocalizeHelper.GetEngineMessage("activityinstancemanager.CheckPassRateMatchedInfo.UnknownMergeTypeValue");
                throw new WorkflowException(msgException);
            }
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
