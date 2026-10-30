
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using mooSQL.data;
using saiflow.data;
using saiflow.engine.Config;
using saiflow.hub.Localize;
using saiflow.hub.Resource;
using saiflow.engine.Common;
using saiflow.engine.Utility;
using saiflow.engine.Business.Entity;
using saiflow.engine.Xpdl.Common;
using saiflow.engine.Business.Result;
using saiflow.engine.Business.SqlProvider;
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

        internal TaskEntity GetTask(SQLBuilder kit, int taskId)
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
            return Sibling(kit)
                .select("*")
                .from(SailSqlDialect.TaskTable)
                .where("activity_instance_id", activityInstanceId)
                .where("process_instance_id", processInstanceId)
                .query<TaskEntity>()
                .ToList();
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
            var list = Sibling(kit)
                .select("*")
                .from(SailSqlDialect.TaskView)
                .where(SailSqlDialect.MapField("ActivityInstanceId"), activityInstanceId)
                .where(SailSqlDialect.MapField("ProcessInstanceId"), processInstanceId)
                .query<TaskViewEntity>()
                .ToList();
            return list.Count > 0 ? list[0] : null;
        }

        /// <summary>
        /// Determine whether the task is the last task of the current node
        /// Single Node: Return True
        /// Multi instance nodes: judged based on the number of instances
        /// 判断任务是否是当前节点最后一个任务
        /// 单一节点：返回True
        /// 多实例节点：根据实例个数判断
        /// </summary>
        public Boolean IsLastTask(int taskId)
        {
            var isLast = false;
            var task = GetTask(taskId);
            var aim = new ActivityInstanceManager(Sail);
            var activityInstance = aim.GetById(task.ActivityInstanceId);

            if (aim.IsMultipleInstanceChildren(activityInstance) == true)
            {
                //多实例会签和加签处理
                //取出会签主节点实例数据
                //Multiple practical meetings for signing and adding signatures
                //Retrieve the primary node instance data for countersignature
                var mainActivityInstance = aim.GetById(activityInstance.MainActivityInstanceId.Value);
                var complexType = EnumHelper.ParseEnum<ComplexTypeEnum>(mainActivityInstance.ComplexType.Value.ToString());
                

                if (complexType == ComplexTypeEnum.SignTogether)        //Sign together
                {
					var mergeType = EnumHelper.ParseEnum<MergeTypeEnum>(mainActivityInstance.MergeType.Value.ToString());
                    if (mergeType == MergeTypeEnum.Sequence)        //Sequence
                    {
                        //取出处于多实例挂起的节点列表
                        //Retrieve the list of nodes that are suspended in multiple instances
                        var sqList = aim.GetActivityMulitipleInstanceWithState(
                            mainActivityInstance.Id,
                            mainActivityInstance.ProcessInstanceId,
                            (short)ActivityStateEnum.Suspended).ToList<ActivityInstanceEntity>();
                        short allNum = (short)mainActivityInstance.AssignedUserIds.Split(',').Length;
                        short maxOrder = 0;

                        if (sqList != null && sqList.Count > 0)
                        {
                            //取出最大执行节点
                            //Retrieve the maximum execution node
                            maxOrder = (short)sqList.Max<ActivityInstanceEntity>(t => t.CompleteOrder.Value);
                        }
                        else if (mainActivityInstance.CompleteOrder <= allNum)
                        {
                            //最后一个执行节点
                            //The last execution node
                            maxOrder = (short)mainActivityInstance.CompleteOrder.Value;
                        }
                        else
                        {
                            maxOrder = allNum;
                        }
                        if (mainActivityInstance.CompareType == null || EnumHelper.ParseEnum<CompareTypeEnum>(mainActivityInstance.CompareType.Value.ToString()) == CompareTypeEnum.Count)
                        {
                            //串行会签通过率（按人数判断）
                            //Serial signature pass rate (judged by number of people)
                            if (mainActivityInstance.CompleteOrder != null && mainActivityInstance.CompleteOrder <= maxOrder)
                            {
                                maxOrder = (short)mainActivityInstance.CompleteOrder;
                            }
                            if (activityInstance.CompleteOrder < maxOrder)
                            {
                                isLast = false;
                            }
                            else if (activityInstance.CompleteOrder == maxOrder)
                            {
                                isLast = true;
                            }
                        }
                        else
                        {
                            //串行会签未设置通过率的判断
                            //Judgment of no set pass rate for serial countersignature
                            if (mainActivityInstance.CompleteOrder == null || mainActivityInstance.CompleteOrder > 1)
                                mainActivityInstance.CompleteOrder = 1;

                            if ((activityInstance.CompleteOrder * 0.01) / (allNum * 0.01) >= mainActivityInstance.CompleteOrder)
                            {
                                isLast = true;
                            }
                            else
                            {
                                isLast = false;
                            }
                        }
                    }
                    else if (mergeType == MergeTypeEnum.Parallel)        //Parallel
                    {
                        //取出处于多实例节点列表
                        //Retrieve the list of nodes in multiple instances
                        var sqList = aim.GetActivityMulitipleInstanceWithState(
                            mainActivityInstance.Id,
                            mainActivityInstance.ProcessInstanceId,
                            null).ToList<ActivityInstanceEntity>();
                        var allCount = sqList.Where(x => x.ActivityState != (short)ActivityStateEnum.Withdrawed).ToList().Count();
                        var completedCount = sqList.Where<ActivityInstanceEntity>(w => w.ActivityState == (short)ActivityStateEnum.Completed || w.AssignedUserIds == task.AssignedUserId)
                            .ToList<ActivityInstanceEntity>()
                            .Count();
                        if (mainActivityInstance.CompareType == null || (EnumHelper.ParseEnum<CompareTypeEnum>(mainActivityInstance.CompareType.Value.ToString()) == CompareTypeEnum.Percentage))
                        {
                            //并行会签未设置通过率的判断
                            //Judgment of no set pass rate for parallel signature
                            if (mainActivityInstance.CompleteOrder == null || mainActivityInstance.CompleteOrder > 1)
                                mainActivityInstance.CompleteOrder = 1;

                            if ((completedCount * 0.01) / (allCount * 0.01) >= mainActivityInstance.CompleteOrder)
                            {
                                isLast = true;
                            }
                            else
                            {
                                isLast = false;
                            }
                        }
                        else
                        {
                            if (mainActivityInstance.CompleteOrder == null || mainActivityInstance.CompleteOrder > allCount)
                            {
                                mainActivityInstance.CompleteOrder = allCount;
                            }
                            if (mainActivityInstance.CompleteOrder > completedCount)
                            {
                                isLast = false;
                            }
                            else if (mainActivityInstance.CompleteOrder == completedCount)
                            {
                                isLast = true;
                            }
                        }
                    }
                }
                else if (complexType == ComplexTypeEnum.SignForward)     //Sign Forward
                {
                    //判断加签是否全部完成，如果是，则流转到下一步，否则不能流转
                    //Check if all signatures have been added. If so, proceed to the next step. Otherwise, do not proceed
                    var signforwardType = EnumHelper.ParseEnum<SignForwardTypeEnum>(activityInstance.SignForwardType.Value.ToString());

                    if (signforwardType == SignForwardTypeEnum.SignForwardBehind
                        || signforwardType == SignForwardTypeEnum.SignForwardBefore)        //Signforward befor, behind
                    {
                        //取出处于多实例节点列表
                        //Retrieve the list of nodes in multiple instances
                        var sqList = aim.GetActivityMulitipleInstanceWithState(
                            mainActivityInstance.Id,
                            mainActivityInstance.ProcessInstanceId,
                            (short)ActivityStateEnum.Suspended).ToList<ActivityInstanceEntity>();

                        short maxOrder = 0;
                        if (sqList != null && sqList.Count > 0)
                        {
                            //取出最大执行节点
                            //Retrieve the maximum execution node
                            maxOrder = (short)sqList.Max<ActivityInstanceEntity>(t => t.CompleteOrder.Value);
                        }
                        else
                        {
                            //最后一个执行节点
                            //The last execution node
                            maxOrder = (short)activityInstance.CompleteOrder;
                        }
                        if (mainActivityInstance.CompareType == null || EnumHelper.ParseEnum<CompareTypeEnum>(mainActivityInstance.CompareType.Value.ToString()) == CompareTypeEnum.Count)
                        {
                            //加签通过率
                            //Approval rate of additional signatures
                            if (mainActivityInstance.CompleteOrder != null && mainActivityInstance.CompleteOrder <= maxOrder)
                            {
                                maxOrder = (short)mainActivityInstance.CompleteOrder;
                            }

                            if (activityInstance.CompleteOrder == sqList.Count)
                            {
                                isLast = true;
                            }
                            else if (activityInstance.CompleteOrder < maxOrder)
                            {
                                isLast = false;
                            }
                            else if (activityInstance.CompleteOrder == maxOrder)
                            {
                                //最后一个节点执行完，主节点进入完成状态，整个流程向下执行
                                //After the last node completes its execution, the main node enters the completion state, and the entire process proceeds downwards
                                isLast = true;
                            }
                        }
                        else
                        {
                            if (mainActivityInstance.CompleteOrder == null || mainActivityInstance.CompleteOrder > 1)//串行加签未设置通过率的判断
                                mainActivityInstance.CompleteOrder = 1;
                            if ((activityInstance.CompleteOrder * 0.01) / (maxOrder * 0.01) >= mainActivityInstance.CompleteOrder)
                            {
                                isLast = true;
                            }
                            else
                            {
                                isLast = false;
                            }
                        }
                    }
                    else if (signforwardType == SignForwardTypeEnum.SignForwardParallel)        //Sign forward parallel
                    {
                        //取出处于多实例节点列表
                        //Retrieve the list of nodes in multiple instances
                        var sqList = aim.GetActivityMulitipleInstanceWithState(
                            mainActivityInstance.Id,
                            mainActivityInstance.ProcessInstanceId,
                            null).ToList<ActivityInstanceEntity>();

                        //并行加签，按照通过率来决定是否标识当前节点完成
                        //Parallel signing, determining whether to mark the current node completion based on the pass rate
                        var allCount = sqList.Where(x => x.ActivityState != (short)ActivityStateEnum.Withdrawed).ToList().Count();
                        var completedCount = sqList.Where<ActivityInstanceEntity>(w => w.ActivityState == (short)ActivityStateEnum.Completed || w.AssignedUserIds == task.AssignedUserId)
                            .ToList<ActivityInstanceEntity>()
                            .Count();
                        if (mainActivityInstance.CompareType == null || EnumHelper.ParseEnum<CompareTypeEnum>(mainActivityInstance.CompareType.Value.ToString()) == CompareTypeEnum.Percentage)
                        {
                            //并行加签通过率的判断
                            //Determination of parallel endorsement pass rate
                            if (mainActivityInstance.CompleteOrder > 1)
                                mainActivityInstance.CompleteOrder = 1;

                            if ((completedCount * 0.01) / (allCount * 0.01) >= mainActivityInstance.CompleteOrder)
                            {
                                isLast = true;
                            }
                            else
                            {
                                isLast = false;
                            }
                        }
                        else
                        {
                            if (mainActivityInstance.CompleteOrder == null || mainActivityInstance.CompleteOrder > allCount)
                            {
                                mainActivityInstance.CompleteOrder = allCount;
                            }
                            if (mainActivityInstance.CompleteOrder > completedCount)
                            {
                                isLast = false;
                            }
                            else if (mainActivityInstance.CompleteOrder == completedCount)
                            {
                                isLast = true;
                            }
                        }
                    }
                }
            }
            else
            {
                //单一节点类型
                //Single node type
                isLast = true;
            }
            return isLast;
        }
        #endregion

        #region Get the current user's processing tasks 获取当前用户的办理任务
        /// <summary>
        /// Retrieve the tasks currently running by the user
        /// 获取当前用户运行中的任务
        /// </summary>
        internal IEnumerable<TaskViewEntity> GetRunningTasks(TaskQuery query)
        {
            return GetTasksPaged(query, 2);
        }

        /// <summary>
        /// Retrieve the tasks currently in ready by the user
        /// 获取当前用户待办的任务
        /// </summary>
        internal IEnumerable<TaskViewEntity> GetReadyTasks(TaskQuery query)
        {
            return GetTasksPaged(query, 1);
        }

        /// <summary>
        /// Retrieve the tasks currently running by activity instance id
        /// 获取正在运行的任务
        /// </summary>
        internal TaskViewEntity GetFirstRunningTask(int activityInstanceId)
        {
            var list = Sail.UseRunSQL()
                .select("*")
                .from(SailSqlDialect.TaskView)
                .where(SailSqlDialect.MapField("ProcessState"), 2)
                .where(SailSqlDialect.MapField("ActivityInstanceId"), activityInstanceId)
                .where($"({SailSqlDialect.MapField("ActivityType")}=4 OR {SailSqlDialect.MapField("WorkItemType")}=1)")
                .where($"({SailSqlDialect.MapField("ActivityState")}=1 OR {SailSqlDialect.MapField("ActivityState")}=2)")
                .where($"({SailSqlDialect.MapField("TaskState")}=1 OR {SailSqlDialect.MapField("TaskState")}=2)")
                .orderBy($"{SailSqlDialect.MapField("TaskState")} DESC")
                .query<TaskViewEntity>()
                .ToList();

            if (list.Count() > 0)
            {
                return list[0];
            }
            return null;
        }

        /// <summary>
        /// Retrieve completed tasks
        /// 获取已经完成任务
        /// </summary>
        internal IEnumerable<TaskViewEntity> GetCompletedTasks(TaskQuery query)
        {
            return GetTasksPaged(query, 4);
        }

        /// <summary>
        /// Get task (pagination)
        /// 获取任务（分页）
        /// </summary>
        private IEnumerable<TaskViewEntity> GetTasksPaged(TaskQuery query, int activityState)
        {
            var kit = Sail.UseRunSQL()
                .select("*")
                .from(SailSqlDialect.TaskView)
                .where(SailSqlDialect.MapField("ProcessState"), 2)
                .where($"({SailSqlDialect.MapField("ActivityType")}=4 OR {SailSqlDialect.MapField("WorkItemType")}=1)");

            if (activityState == (short)ActivityStateEnum.Ready
                || activityState == (short)ActivityStateEnum.Running)
            {
                kit.where(SailSqlDialect.MapField("ActivityState"), activityState)
                    .where(SailSqlDialect.MapField("TaskState"), 32, "<>");
            }
            else if (activityState == (short)ActivityStateEnum.Completed)
            {
                kit.where(SailSqlDialect.MapField("TaskState"), 4);
            }

            if (activityState == 1)
            {
                kit.where(SailSqlDialect.MapField("MainActivityState"), 4, "<>");
            }

            if (!string.IsNullOrEmpty(query.AppInstanceId))
                kit.where(SailSqlDialect.MapField("AppInstanceId"), query.AppInstanceId);

            if (!string.IsNullOrEmpty(query.ProcessId))
                kit.where(SailSqlDialect.MapField("ProcessId"), query.ProcessId);

            if (!string.IsNullOrEmpty(query.Version))
                kit.where(SailSqlDialect.MapField("Version"), query.Version);

            if (!string.IsNullOrEmpty(query.UserId))
                kit.where(SailSqlDialect.MapField("AssignedUserId"), query.UserId);

            if (!string.IsNullOrEmpty(query.EndedUserId))
                kit.where(SailSqlDialect.MapField("EndedUserId"), query.EndedUserId);

            if (!string.IsNullOrEmpty(query.AppName))
                kit.whereLike(SailSqlDialect.MapField("AppName"), query.AppName);

            var pageSize = query.PageSize;
            if (pageSize == 0) pageSize = 100;          //Default number of records per page for pagination

            return kit.orderBy($"{SailSqlDialect.TaskIdColumn} DESC")
                .query<TaskViewEntity>()
                .Take(pageSize)
                .ToList();
        }

        /// <summary>
        /// Get Top 10 task todo list
        /// 仅供用于数据展示使用
        /// </summary>
        public List<TaskViewEntity> GetTaskToDoListTop()
        {
            return Sail.UseRunSQL()
                .select("*")
                .from(SailSqlDialect.TaskView)
                .where(SailSqlDialect.MapField("ProcessState"), 2)
                .where($"({SailSqlDialect.MapField("ActivityType")}=4 OR {SailSqlDialect.MapField("WorkItemType")}=1)")
                .where(SailSqlDialect.MapField("ActivityState"), 1)
                .where(SailSqlDialect.MapField("TaskState"), 32, "<>")
                .orderBy($"{SailSqlDialect.TaskIdColumn} DESC")
                .top(10)
                .query<TaskViewEntity>()
                .ToList();
        }

        /// <summary>
        /// Get Top 10 task done list
        /// 仅供用于数据展示使用
        /// </summary>
        public List<TaskViewEntity> GetTaskDoneListTop()
        {
            return Sail.UseRunSQL()
                .select("*")
                .from(SailSqlDialect.TaskView)
                .where(SailSqlDialect.MapField("ProcessState"), 2)
                .where($"({SailSqlDialect.MapField("ActivityType")}=4 OR {SailSqlDialect.MapField("WorkItemType")}=1)")
                .where(SailSqlDialect.MapField("ActivityState"), 4)
                .where(SailSqlDialect.MapField("TaskState"), 32, "<>")
                .orderBy($"{SailSqlDialect.TaskIdColumn} DESC")
                .top(10)
                .query<TaskViewEntity>()
                .ToList();
        }

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
            var list = Sibling(kit)
                .select("*")
                .from(SailSqlDialect.TaskView)
                .where(SailSqlDialect.MapField("ActivityInstanceId"), activityInstanceId)
                .where(SailSqlDialect.MapField("AssignedUserId"), userId)
                .where(SailSqlDialect.MapField("ProcessState"), 2)
                .where($"({SailSqlDialect.MapField("ActivityType")}=4 OR {SailSqlDialect.MapField("ActivityType")}=5 OR {SailSqlDialect.MapField("ActivityType")}=6)")
                .where($"({SailSqlDialect.MapField("ActivityState")}=1 OR {SailSqlDialect.MapField("ActivityState")}=2)")
                .orderBy($"{SailSqlDialect.TaskIdColumn} DESC")
                .top(1)
                .query<TaskViewEntity>()
                .ToList();

            return ResolveTaskOfMineByActivityInstance(list, activityInstanceId);
        }

        private static TaskViewEntity ResolveTaskOfMineByActivityInstance(
            List<TaskViewEntity> list, int activityInstanceId)
        {
            if (list.Count == 0)
            {
                var message = LocalizeHelper.GetEngineMessage("taskmanager.gettaskofmine.error");
                throw new WorkflowException(
                    string.Format("{0}，ActivityInstanceId: {1}", message, activityInstanceId.ToString())
                );
            }
            if (list.Count > 1)
            {
                throw new WorkflowException(LocalizeHelper.GetEngineMessage("taskmanager.gettaskofmine.toomoretasks.error"));
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
            var list = Sibling(kit)
                .select("*")
                .from(SailSqlDialect.TaskView)
                .where(SailSqlDialect.MapField("AppInstanceId"), appInstanceId)
                .where(SailSqlDialect.MapField("ProcessId"), processId)
                .where(SailSqlDialect.MapField("AssignedUserId"), userId)
                .where(SailSqlDialect.MapField("ProcessState"), 2)
                .where($"({SailSqlDialect.MapField("ActivityType")}=4 OR {SailSqlDialect.MapField("ActivityType")}=5 OR {SailSqlDialect.MapField("ActivityType")}=6 OR {SailSqlDialect.MapField("WorkItemType")}=1)")
                .where($"({SailSqlDialect.MapField("ActivityState")}=1 OR {SailSqlDialect.MapField("ActivityState")}=2)")
                .orderBy($"{SailSqlDialect.TaskIdColumn} DESC")
                .top(1)
                .query<TaskViewEntity>()
                .ToList();

            if (list.Count == 0)
            {
                var message = LocalizeHelper.GetEngineMessage("taskmanager.gettaskofmine.error");
                throw new WorkflowException(
                    string.Format("{0}，AppInstanceId: {1}", message, appInstanceId.ToString())
                );
            }
            if (list.Count > 1)
            {
                throw new WorkflowException(LocalizeHelper.GetEngineMessage("taskmanager.gettaskofmine.toomoretasks.error"));
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

        /// <summary>
        /// Retrieve pending tasks (business instances)
        /// 获取待办任务(业务实例)
        /// </summary>
        internal IEnumerable<TaskViewEntity> GetReadyTaskOfApp(WfAppRunner runner)
        {
            return Sail.UseRunSQL()
                .select("*")
                .from(SailSqlDialect.TaskView)
                .where(SailSqlDialect.MapField("AppInstanceId"), runner.AppInstanceId)
                .where(SailSqlDialect.MapField("ProcessId"), runner.ProcessId)
                .where(SailSqlDialect.MapField("ProcessState"), 2)
                .where($"({SailSqlDialect.MapField("ActivityType")}=4 OR {SailSqlDialect.MapField("WorkItemType")}=1)")
                .where(SailSqlDialect.MapField("ActivityState"), 1)
                .orderBy($"{SailSqlDialect.TaskIdColumn} DESC")
                .query<TaskViewEntity>();
        }

        /// <summary>
        /// Get the to-do list of unsent email notifications
        /// 获取未发送邮件通知的待办任务列表
        /// </summary>
        internal IList<TaskViewEntity> GetTaskListEMailUnSent()
        {
            return Sail.UseRunSQL()
                .select("*")
                .from(SailSqlDialect.TaskView)
                .where(SailSqlDialect.MapField("ProcessState"), 2)
                .where($"({SailSqlDialect.MapField("ActivityType")}=4 OR {SailSqlDialect.MapField("WorkItemType")}=1)")
                .where(SailSqlDialect.MapField("ActivityState"), 1)
                .where(SailSqlDialect.MapField("IsEMailSent"), 0)
                .where(SailSqlDialect.MapField("TaskState"), 32, "<>")
                .query<TaskViewEntity>()
                .ToList();
        }
        #endregion

        #region TaskManager 任务数据基本操作
        private int Insert(SQLBuilder kit, TaskEntity entity)
        {
            int newTaskId = Sibling(kit).useRepo<TaskEntity, int>().InsertWithPK(entity);
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
                runner.UserId, runner.UserName);
        }

        private int Insert(SQLBuilder kit, ActivityInstanceEntity activityInstance,
            string performerId,
            string performerName,
            string runnerId,
            string runnerName,
            int? entrustedTaskId = null)
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
            SendTaskEMails(entity);
            return taskId;
        }

        /// <summary>
        /// Send email notification messages
        /// email template can be modified by customers
        /// the customers can use their own email html template, this is only a demo
        /// internal static readonly string EMailSendUtility_SendEMailTitle = "新待办事项提醒邮件！";
        /// internal static readonly string EMailSendUtility_SendEMailContent = @" < h1 >新待办任务提醒</h1>" +
        ///        "<p>您有一条新的待办任务： " +
        ///        "<a href='http://localhost/sfmvc/order/{0}'>请在此处填写具体的业务单据名称和单据序号</a>  " +
        ///        "请您及时登录业务系统处理！谢谢！</p>";
        /// 发送任务邮件
        /// </summary>
        private void SendTaskEMails(TaskEntity entity)
        {
            //判断是否需要发送待办邮件
            //Determine whether to send a to-do email
            if (JobAdminConfig.EMailSendUtility_SendEMailFlag == true)
            {
                var userId = entity.AssignedUserId;
                var rm = ResourceServiceLocator.Resolve();
                var user = rm.GetUserById(userId);

                if (user != null && !string.IsNullOrEmpty(user.EMail))
                {
                    var title = LocalizeHelper.GetEngineMessage("taskmanager.emailtemplate.title");
                    var body = LocalizeHelper.GetEngineMessage("taskmanager.emailtemplate.body");
                    body = string.Format(body, JobAdminConfig.EMailSendUtility_LocalHostWebApp, entity.AppName, entity.AppInstanceId);
                    var emailSendUtility = new EMailSendUtility(Sail, entity.Id);
                    emailSendUtility.SendEMailAsync(title, body, user.EMail);
                }
            }
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

        internal void Update(TaskEntity entity)
        {
            var kit = Sail.UseRunSQL();
            Update(kit, entity);
        }

        internal void Update(SQLBuilder kit, TaskEntity entity)
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
                    new ActivityInstanceManager(Sail).Read(kit, task.ActivityInstanceId, taskRunner);
                    kit.commit();
                }
                catch (System.Exception e)
                {
                    throw new WorkflowException(LocalizeHelper.GetEngineMessage("taskmanager.settaskread.error", e.Message), e);
                }
            }
        }

        internal void SetTaskEMailSent(int taskId)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var task = GetTask(kit, taskId);
                    task.IsEMailSent = (byte)TaskEMailSentStatusEnum.Sent;
                    Update(kit, task);
                    kit.commit();
                }
                catch (System.Exception e)
                {
                    throw new WorkflowException(LocalizeHelper.GetEngineMessage("taskmanager.settaskemailsent.error", e.Message), e);
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
            => EndTaskState(kit, taskId, TaskStateEnum.Completed, runner);

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
                    throw new WorkflowException(LocalizeHelper.GetEngineMessage("taskmanager.entrust.error"), e);
                }
            }
        }

        internal WfDataManagedResult Entrust(SQLBuilder kit, TaskEntrustedEntity entity, bool cancelOriginalTask = true)
        {
            var am = new ActivityInstanceManager(Sail);
            var activityInstance = am.GetByTask(kit, entity.TaskId);

            if (activityInstance.ActivityState != (short)ActivityStateEnum.Ready
                && activityInstance.ActivityState != (short)ActivityStateEnum.Running)
            {
                return WfDataManagedResult.Exception(LocalizeHelper.GetEngineMessage("taskmanager.entrust.warn"));
            }

            if (cancelOriginalTask)
            {
                var task = GetTask(kit, entity.TaskId);
                task.TaskState = (short)TaskStateEnum.Closed;
                Update(kit, task);
            }

            var taskList = GetTaskListByActivity(kit, activityInstance.ProcessInstanceId, activityInstance.Id);
            if (taskList.Where(t => t.AssignedUserId == entity.EntrustToUserId).Any())
            {
                return WfDataManagedResult.Exception(LocalizeHelper.GetEngineMessage("taskmanager.entrust.notagain.warn"));
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
                    throw new WorkflowException(LocalizeHelper.GetEngineMessage("taskmanager.entrust.error"), e);
                }
            }
        }

        internal void CancelEntrustedTask(SQLBuilder kit, int taskId)
        {
            var list = Sibling(kit)
                .select("*")
                .from(SailSqlDialect.TaskTable)
                .where("entrusted_task_id", taskId)
                .query<TaskEntity>();
            var entrustedTaskEntity = list.FirstOrDefault();
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
                    throw new WorkflowException(LocalizeHelper.GetEngineMessage("taskmanager.replace.error"), e);
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
            Sibling(kit)
                .setTable(SailSqlDialect.TaskTable)
                .set("task_state", 48)
                .set("record_status_invalid", 1)
                .where("activity_instance_id", activityInstanceId)
                .doUpdate();
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
