using System;
using System.Collections.Generic;
using System.Linq;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.engine.Business.SqlProvider;
using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.engine.Xpdl.Common;
using saiflow.hub.Resource;
using saiflow.WebUtility;

namespace saiflow.biz.query
{
    /// <summary>
    /// 业务侧流程查询（Inbox / 目录 / 监控展示），不参与引擎运转。
    /// </summary>
    public class WorkflowQueryService
    {
        public WorkflowQueryService(SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        #region Task inbox / display

        /// <summary>待办 Top10（仅展示）。</summary>
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

        /// <summary>已办 Top10（仅展示）。</summary>
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

        public IList<TaskViewEntity> GetRunningTasks(TaskQuery query)
            => GetTasksPaged(query, 2);

        public IList<TaskViewEntity> GetReadyTasks(TaskQuery query)
            => GetTasksPaged(query, 1);

        public IList<TaskViewEntity> GetCompletedTasks(TaskQuery query)
            => GetTasksPaged(query, 4);

        public TaskViewEntity? GetFirstRunningTask(int activityInstanceId)
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

            return list.Count > 0 ? list[0] : null;
        }

        public IList<TaskViewEntity> GetTaskListEMailUnSent()
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

        public bool SetTaskEMailSent(int taskId)
        {
            var tm = new TaskManager(Sail);
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var task = tm.GetTask(kit, taskId);
                    task.IsEMailSent = (byte)TaskEMailSentStatusEnum.Sent;
                    tm.Update(kit, task);
                    kit.commit();
                    return true;
                }
                catch (Exception e)
                {
                    throw new WorkflowException(
                        Sail.Messages.GetEngineMessage("taskmanager.settaskemailsent.error", e.Message), e);
                }
            }
        }

        public IList<Performer> GetTaskPerformers(WfAppRunner runner)
        {
            var tasks = GetReadyTaskOfApp(runner).ToList();
            IList<Performer> performerList = new List<Performer>();
            foreach (var task in tasks)
            {
                performerList.Add(new Performer(task.AssignedUserId, task.AssignedUserName));
            }
            return performerList;
        }

        public IEnumerable<TaskViewEntity> GetReadyTaskOfApp(WfAppRunner runner)
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
        /// 判断当前任务是否是最后一个任务（会签/加签/单节点）。
        /// </summary>
        public bool IsLastTask(int taskId)
        {
            var isLast = false;
            var tm = new TaskManager(Sail);
            var task = tm.GetTask(taskId);
            var aim = new ActivityInstanceManager(Sail);
            var activityInstance = aim.GetById(task.ActivityInstanceId);

            if (aim.IsMultipleInstanceChildren(activityInstance) == true)
            {
                var mainActivityInstance = aim.GetById(activityInstance.MainActivityInstanceId.Value);
                var complexType = EnumHelper.ParseEnum<ComplexTypeEnum>(mainActivityInstance.ComplexType.Value.ToString());

                if (complexType == ComplexTypeEnum.SignTogether)
                {
                    var mergeType = EnumHelper.ParseEnum<MergeTypeEnum>(mainActivityInstance.MergeType.Value.ToString());
                    if (mergeType == MergeTypeEnum.Sequence)
                    {
                        var sqList = aim.GetActivityMulitipleInstanceWithState(
                            mainActivityInstance.Id,
                            mainActivityInstance.ProcessInstanceId,
                            (short)ActivityStateEnum.Suspended).ToList();
                        short allNum = (short)mainActivityInstance.AssignedUserIds.Split(',').Length;
                        short maxOrder = 0;

                        if (sqList != null && sqList.Count > 0)
                            maxOrder = (short)sqList.Max(t => t.CompleteOrder.Value);
                        else if (mainActivityInstance.CompleteOrder <= allNum)
                            maxOrder = (short)mainActivityInstance.CompleteOrder.Value;
                        else
                            maxOrder = allNum;

                        if (mainActivityInstance.CompareType == null
                            || EnumHelper.ParseEnum<CompareTypeEnum>(mainActivityInstance.CompareType.Value.ToString()) == CompareTypeEnum.Count)
                        {
                            if (mainActivityInstance.CompleteOrder != null && mainActivityInstance.CompleteOrder <= maxOrder)
                                maxOrder = (short)mainActivityInstance.CompleteOrder;
                            if (activityInstance.CompleteOrder < maxOrder)
                                isLast = false;
                            else if (activityInstance.CompleteOrder == maxOrder)
                                isLast = true;
                        }
                        else
                        {
                            if (mainActivityInstance.CompleteOrder == null || mainActivityInstance.CompleteOrder > 1)
                                mainActivityInstance.CompleteOrder = 1;

                            if ((activityInstance.CompleteOrder * 0.01) / (allNum * 0.01) >= mainActivityInstance.CompleteOrder)
                                isLast = true;
                            else
                                isLast = false;
                        }
                    }
                    else if (mergeType == MergeTypeEnum.Parallel)
                    {
                        var sqList = aim.GetActivityMulitipleInstanceWithState(
                            mainActivityInstance.Id,
                            mainActivityInstance.ProcessInstanceId,
                            null).ToList();
                        var allCount = sqList.Where(x => x.ActivityState != (short)ActivityStateEnum.Withdrawed).ToList().Count();
                        var completedCount = sqList
                            .Where(w => w.ActivityState == (short)ActivityStateEnum.Completed || w.AssignedUserIds == task.AssignedUserId)
                            .ToList()
                            .Count();
                        if (mainActivityInstance.CompareType == null
                            || EnumHelper.ParseEnum<CompareTypeEnum>(mainActivityInstance.CompareType.Value.ToString()) == CompareTypeEnum.Percentage)
                        {
                            if (mainActivityInstance.CompleteOrder == null || mainActivityInstance.CompleteOrder > 1)
                                mainActivityInstance.CompleteOrder = 1;

                            if ((completedCount * 0.01) / (allCount * 0.01) >= mainActivityInstance.CompleteOrder)
                                isLast = true;
                            else
                                isLast = false;
                        }
                        else
                        {
                            if (mainActivityInstance.CompleteOrder == null || mainActivityInstance.CompleteOrder > allCount)
                                mainActivityInstance.CompleteOrder = allCount;
                            if (mainActivityInstance.CompleteOrder > completedCount)
                                isLast = false;
                            else if (mainActivityInstance.CompleteOrder == completedCount)
                                isLast = true;
                        }
                    }
                }
                else if (complexType == ComplexTypeEnum.SignForward)
                {
                    var signforwardType = EnumHelper.ParseEnum<SignForwardTypeEnum>(activityInstance.SignForwardType.Value.ToString());

                    if (signforwardType == SignForwardTypeEnum.SignForwardBehind
                        || signforwardType == SignForwardTypeEnum.SignForwardBefore)
                    {
                        var sqList = aim.GetActivityMulitipleInstanceWithState(
                            mainActivityInstance.Id,
                            mainActivityInstance.ProcessInstanceId,
                            (short)ActivityStateEnum.Suspended).ToList();

                        short maxOrder = 0;
                        if (sqList != null && sqList.Count > 0)
                            maxOrder = (short)sqList.Max(t => t.CompleteOrder.Value);
                        else
                            maxOrder = (short)activityInstance.CompleteOrder;

                        if (mainActivityInstance.CompareType == null
                            || EnumHelper.ParseEnum<CompareTypeEnum>(mainActivityInstance.CompareType.Value.ToString()) == CompareTypeEnum.Count)
                        {
                            if (mainActivityInstance.CompleteOrder != null && mainActivityInstance.CompleteOrder <= maxOrder)
                                maxOrder = (short)mainActivityInstance.CompleteOrder;

                            if (activityInstance.CompleteOrder == sqList.Count)
                                isLast = true;
                            else if (activityInstance.CompleteOrder < maxOrder)
                                isLast = false;
                            else if (activityInstance.CompleteOrder == maxOrder)
                                isLast = true;
                        }
                        else
                        {
                            if (mainActivityInstance.CompleteOrder == null || mainActivityInstance.CompleteOrder > 1)
                                mainActivityInstance.CompleteOrder = 1;
                            if ((activityInstance.CompleteOrder * 0.01) / (maxOrder * 0.01) >= mainActivityInstance.CompleteOrder)
                                isLast = true;
                            else
                                isLast = false;
                        }
                    }
                    else if (signforwardType == SignForwardTypeEnum.SignForwardParallel)
                    {
                        var sqList = aim.GetActivityMulitipleInstanceWithState(
                            mainActivityInstance.Id,
                            mainActivityInstance.ProcessInstanceId,
                            null).ToList();

                        var allCount = sqList.Where(x => x.ActivityState != (short)ActivityStateEnum.Withdrawed).ToList().Count();
                        var completedCount = sqList
                            .Where(w => w.ActivityState == (short)ActivityStateEnum.Completed || w.AssignedUserIds == task.AssignedUserId)
                            .ToList()
                            .Count();
                        if (mainActivityInstance.CompareType == null
                            || EnumHelper.ParseEnum<CompareTypeEnum>(mainActivityInstance.CompareType.Value.ToString()) == CompareTypeEnum.Percentage)
                        {
                            if (mainActivityInstance.CompleteOrder > 1)
                                mainActivityInstance.CompleteOrder = 1;

                            if ((completedCount * 0.01) / (allCount * 0.01) >= mainActivityInstance.CompleteOrder)
                                isLast = true;
                            else
                                isLast = false;
                        }
                        else
                        {
                            if (mainActivityInstance.CompleteOrder == null || mainActivityInstance.CompleteOrder > allCount)
                                mainActivityInstance.CompleteOrder = allCount;
                            if (mainActivityInstance.CompleteOrder > completedCount)
                                isLast = false;
                            else if (mainActivityInstance.CompleteOrder == completedCount)
                                isLast = true;
                        }
                    }
                }
            }
            else
            {
                isLast = true;
            }
            return isLast;
        }

        private IList<TaskViewEntity> GetTasksPaged(TaskQuery query, int activityState)
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
                kit.where(SailSqlDialect.MapField("MainActivityState"), 4, "<>");

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
            if (pageSize == 0) pageSize = 100;

            return kit.orderBy($"{SailSqlDialect.TaskIdColumn} DESC")
                .query<TaskViewEntity>()
                .Take(pageSize)
                .ToList();
        }

        #endregion

        #region Process catalog / monitor display

        public IList<ProcessEntity> GetProcessListSimple()
        {
            var kit = Sail.UseRunSQL().from(SailSqlDialect.ProcessEntityTable);
            if (SailSqlDialect.IsSqlServer)
            {
                kit.select(@"Id, ProcessId, ProcessName, ProcessCode, Version, Status, AppType, PackageType, PackageId,
                    PageUrl, Icon, StartType, StartExpression, EndType, EndExpression, CreatedDateTime, UpdatedDateTime")
                    .orderBy("UpdatedDateTime DESC");
            }
            else
            {
                kit.select(@"id AS Id, process_id AS ProcessId, process_name AS ProcessName, process_code AS ProcessCode,
                    version AS Version, status AS Status, app_type AS AppType, package_type AS PackageType, package_id AS PackageId,
                    page_url AS PageUrl, icon AS Icon, start_type AS StartType, start_expression AS StartExpression,
                    end_type AS EndType, end_expression AS EndExpression, created_datetime AS CreatedDateTime,
                    updated_datetime AS UpdatedDateTime")
                    .orderBy("COALESCE(updated_datetime, '1970-01-01') DESC");
            }
            return kit.query<ProcessEntity>().ToList();
        }

        public int GetProcessInstanceCount(string processId, string version)
        {
            return Sail.UseRunSQL()
                .from(SailSqlDialect.ProcessInstanceTable)
                .where("process_id", processId)
                .where("version", version)
                .count();
        }

        public IList<NodeImage> GetActivityInstanceCompleted(int taskId)
        {
            var tm = new TaskManager(Sail);
            var task = tm.GetTaskView(taskId);
            return GetActivityInstanceCompleted(task.AppInstanceId, task.ProcessId);
        }

        public IList<NodeImage> GetActivityInstanceCompleted(WfAppRunner runner)
            => GetActivityInstanceCompleted(runner.AppInstanceId, runner.ProcessId);

        public IList<NodeImage> GetActivityInstanceCompleted(string appInstanceId, string processId)
        {
            IList<NodeImage> imageList = new List<NodeImage>();
            var am = new ActivityInstanceManager(Sail);
            var list = am.GetActivityInstanceListCompleted(appInstanceId, processId);

            foreach (ActivityInstanceEntity a in list)
            {
                imageList.Add(new NodeImage
                {
                    Id = a.Id,
                    ActivityName = a.ActivityName
                });
            }
            return imageList;
        }

        public IList<TransitionImage> GetTransitionInstanceList(TransitionInstanceQuery query)
        {
            IList<TransitionImage> imageList = new List<TransitionImage>();
            var tm = new TransitionInstanceManager(Sail);
            var list = tm.GetTransitionInstanceList(query.AppInstanceId, query.ProcessId, query.Version).ToList();

            foreach (TransitionInstanceEntity t in list)
            {
                imageList.Add(new TransitionImage
                {
                    Id = t.Id,
                    TransitionId = t.TransitionId
                });
            }
            return imageList;
        }

        public IList<ActivityInstanceEntity> GetRunningActivityInstance(TaskQuery query)
        {
            var aim = new ActivityInstanceManager(Sail);
            return aim.GetRunningActivityInstanceList(query.AppInstanceId, query.ProcessId, query.Version).ToList();
        }

        #endregion
    }
}
