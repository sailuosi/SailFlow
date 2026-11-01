using System;
using System.Collections.Generic;
using System.Linq;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.engine.Client;
using mooSQL.data;
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
            var clip = Sail.UseRunSQL().useClip();
            clip.from<TaskViewEntity>(out var t);
            clip.where(() => t.ProcessState, (short)2);
            clip.where(() => t.ActivityType == 4 || t.WorkItemType == 1);
            clip.where(() => t.ActivityState, (short)1);
            clip.where(() => t.TaskState, (short)32, "<>");
            clip.orderByDesc(() => t.Id);
            clip.top(10);
            return clip.select(t).queryList().ToList();
        }

        /// <summary>已办 Top10（仅展示）。</summary>
        public List<TaskViewEntity> GetTaskDoneListTop()
        {
            var clip = Sail.UseRunSQL().useClip();
            clip.from<TaskViewEntity>(out var t);
            clip.where(() => t.ProcessState, (short)2);
            clip.where(() => t.ActivityType == 4 || t.WorkItemType == 1);
            clip.where(() => t.ActivityState, (short)4);
            clip.where(() => t.TaskState, (short)32, "<>");
            clip.orderByDesc(() => t.Id);
            clip.top(10);
            return clip.select(t).queryList().ToList();
        }

        public IList<TaskViewEntity> GetRunningTasks(TaskQuery query)
            => QueryTasksPaged(query, 2).Items;

        public IList<TaskViewEntity> GetReadyTasks(TaskQuery query)
            => QueryTasksPaged(query, 1).Items;

        public IList<TaskViewEntity> GetCompletedTasks(TaskQuery query)
            => QueryTasksPaged(query, 4).Items;

        /// <summary>分页待办（ActivityState=Ready）。</summary>
        public (IList<TaskViewEntity> Items, int Total) QueryReadyTasksPaged(TaskQuery query)
            => QueryTasksPaged(query, 1);

        /// <summary>分页已办（TaskState=Completed）。</summary>
        public (IList<TaskViewEntity> Items, int Total) QueryCompletedTasksPaged(TaskQuery query)
            => QueryTasksPaged(query, 4);

        /// <summary>分页运行中任务（ActivityState=Running）。</summary>
        public (IList<TaskViewEntity> Items, int Total) QueryRunningTasksPaged(TaskQuery query)
            => QueryTasksPaged(query, 2);

        public TaskViewEntity? GetFirstRunningTask(int activityInstanceId)
        {
            var clip = Sail.UseRunSQL().useClip();
            clip.from<TaskViewEntity>(out var t);
            clip.where(() => t.ProcessState, (short)2);
            clip.where(() => t.ActivityInstanceId, activityInstanceId);
            clip.where(() => t.ActivityType == 4 || t.WorkItemType == 1);
            clip.where(() => t.ActivityState == 1 || t.ActivityState == 2);
            clip.where(() => t.TaskState == 1 || t.TaskState == 2);
            clip.orderByDesc(() => t.TaskState);
            var list = clip.select(t).queryList().ToList();

            return list.Count > 0 ? list[0] : null;
        }

        public IList<TaskViewEntity> GetTaskListEMailUnSent()
        {
            var clip = Sail.UseRunSQL().useClip();
            clip.from<TaskViewEntity>(out var t);
            clip.where(() => t.ProcessState, (short)2);
            clip.where(() => t.ActivityType == 4 || t.WorkItemType == 1);
            clip.where(() => t.ActivityState, (short)1);
            clip.where(() => t.IsEMailSent, (byte?)0);
            clip.where(() => t.TaskState, (short)32, "<>");
            return clip.select(t).queryList().ToList();
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
            var clip = Sail.UseRunSQL().useClip();
            clip.from<TaskViewEntity>(out var t);
            clip.where(() => t.AppInstanceId, runner.AppInstanceId);
            clip.where(() => t.ProcessId, runner.ProcessId);
            clip.where(() => t.ProcessState, (short)2);
            clip.where(() => t.ActivityType == 4 || t.WorkItemType == 1);
            clip.where(() => t.ActivityState, (short)1);
            clip.orderByDesc(() => t.Id);
            return clip.select(t).queryList();
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
            var activityInstance = aim.GetById(task.ActivityInstanceId ?? throw new InvalidOperationException("Task ActivityInstanceId is required."));

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
                            mainActivityInstance.Id.Value,
                            mainActivityInstance.ProcessInstanceId.Value,
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
                            mainActivityInstance.Id.Value,
                            mainActivityInstance.ProcessInstanceId.Value,
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
                            mainActivityInstance.Id.Value,
                            mainActivityInstance.ProcessInstanceId.Value,
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
                            mainActivityInstance.Id.Value,
                            mainActivityInstance.ProcessInstanceId.Value,
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

        /// <summary>
        /// 按活动/任务状态分页查询任务视图。activityState: 1 Ready / 2 Running / 4 Completed(task)。
        /// </summary>
        public (IList<TaskViewEntity> Items, int Total) QueryTasksPaged(TaskQuery query, int activityState)
        {
            query ??= new TaskQuery();
            var pageSize = query.PageSize > 0 ? query.PageSize : 20;
            var pageIndex = query.PageIndex > 0 ? query.PageIndex : 1;

            var clip = Sail.UseRunSQL().useClip();
            clip.from<TaskViewEntity>(out var t);
            clip.where(() => t.ProcessState, (short)2);
            clip.where(() => t.ActivityType == 4 || t.WorkItemType == 1);

            if (activityState == (short)ActivityStateEnum.Ready
                || activityState == (short)ActivityStateEnum.Running)
            {
                clip.where(() => t.ActivityState, (short)activityState);
                clip.where(() => t.TaskState, (short)32, "<>");
            }
            else if (activityState == (short)ActivityStateEnum.Completed)
            {
                clip.where(() => t.TaskState, (short)4);
            }

            // 非会签任务 main_activity_state 为 NULL；SQL 中 NULL<>4 为未知，会把整行滤掉
            if (activityState == 1)
            {
                clip.sinkOR();
                clip.whereIsNull(() => t.MainActivityState);
                clip.where(() => t.MainActivityState, (short)4, "<>");
                clip.rise();
            }

            clip.whereIf(!string.IsNullOrEmpty(query.AppInstanceId), () => t.AppInstanceId, query.AppInstanceId);
            clip.whereIf(!string.IsNullOrEmpty(query.ProcessId), () => t.ProcessId, query.ProcessId);
            clip.whereIf(!string.IsNullOrEmpty(query.Version), () => t.Version, query.Version);
            clip.whereIf(!string.IsNullOrEmpty(query.UserId), () => t.AssignedUserId, query.UserId);
            clip.whereIf(!string.IsNullOrEmpty(query.EndedUserId), () => t.EndedUserId, query.EndedUserId);
            if (!string.IsNullOrEmpty(query.AppName))
                clip.whereLike(() => t.AppName, query.AppName);

            clip.orderByDesc(() => t.Id);
            // 库端分页（禁止全量 queryList 再内存切页）
            var page = clip.select(t).setPage(pageSize, pageIndex).queryPage();
            var items = (IList<TaskViewEntity>)(page.Items?.ToList() ?? new List<TaskViewEntity>());
            return (items, page.Total);
        }

        /// <summary>我的/管理端流程实例分页。</summary>
        public (IList<ProcessInstanceEntity> Items, int Total) QueryProcessInstancesPaged(ProcessInstanceQuery query)
        {
            query ??= new ProcessInstanceQuery();
            var pageSize = query.PageSize > 0 ? query.PageSize : 20;
            var pageIndex = query.PageIndex > 0 ? query.PageIndex : 1;

            var clip = Sail.UseRunSQL().useClip();
            clip.from<ProcessInstanceEntity>(out var pi);
            clip.whereIf(!string.IsNullOrEmpty(query.ProcessId), () => pi.ProcessId, query.ProcessId);
            clip.whereIf(!string.IsNullOrEmpty(query.ApplicationInstanceId), () => pi.AppInstanceId, query.ApplicationInstanceId);
            if (!string.IsNullOrEmpty(query.AppName))
                clip.whereLike(() => pi.AppName, query.AppName);
            clip.whereIf(!string.IsNullOrEmpty(query.CreatedUserId), () => pi.CreatedUserId, query.CreatedUserId);
            if (query.ProcessState.HasValue)
                clip.where(() => pi.ProcessState, query.ProcessState.Value);

            clip.orderByDesc(() => pi.Id);
            var page = clip.select(pi).setPage(pageSize, pageIndex).queryPage();
            var items = (IList<ProcessInstanceEntity>)(page.Items?.ToList() ?? new List<ProcessInstanceEntity>());
            return (items, page.Total);
        }

        #endregion

        #region Process catalog / monitor display

        public IList<ProcessEntity> GetProcessListSimple()
        {
            var clip = Sail.UseRunSQL().useClip();
            clip.from<ProcessEntity>(out var p);
            clip.orderByDesc(() => p.UpdatedDateTime);
            return clip.select(p).queryList().ToList();
        }

        public int GetProcessInstanceCount(string processId, string version)
        {
            var clip = Sail.UseRunSQL().useClip();
            clip.from<ProcessInstanceEntity>(out var pi);
            clip.where(() => pi.ProcessId, processId);
            clip.where(() => pi.Version, version);
            return clip.count();
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
                    Id = a.Id ?? 0,
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
                    Id = t.Id ?? 0,
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
