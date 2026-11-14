using System;
using System.Collections.Generic;
using System.Linq;
using sailflow.engine.Entity;
using sailflow.engine.Manager;
using sailflow.engine.Client;
using mooSQL.data;
using sailflow.engine.Common;
using sailflow.data.Hosting;
using sailflow.engine.Xpdl;
using sailflow.hub.Resource;
using sailflow.WebUtility;

namespace sailflow.biz.query
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
            clip.from<TaskViewEntity>(out var t)
                .where(() => t.ProcessState, (short)2)
                .where(() => t.ActivityType == 4 || t.WorkItemType == 1)
                .where(() => t.ActivityState, (short)1)
                .where(() => t.TaskState, (short)32, "<>")
                .orderByDesc(() => t.ActivatedDateTime)
                .top(10);
            return clip.select(t).queryList().ToList();
        }

        /// <summary>已办 Top10（仅展示）。</summary>
        public List<TaskViewEntity> GetTaskDoneListTop()
        {
            var clip = Sail.UseRunSQL().useClip();
            clip.from<TaskViewEntity>(out var t)
                .where(() => t.ProcessState, (short)2)
                .where(() => t.ActivityType == 4 || t.WorkItemType == 1)
                .where(() => t.ActivityState, (short)4)
                .where(() => t.TaskState, (short)32, "<>")
                .orderByDesc(() => t.EndedDateTime)
                .top(10);
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

        public TaskViewEntity? GetFirstRunningTask(string activityInstanceId)
        {
            var clip = Sail.UseRunSQL().useClip();
            clip.from<TaskViewEntity>(out var t)
                .where(() => t.ProcessState, (short)2)
                .where(() => t.ActivityInstanceId, activityInstanceId)
                .where(() => t.ActivityType == 4 || t.WorkItemType == 1)
                .whereIn(() => t.ActivityState, new short?[] { (short)1, (short)2 })
                .whereIn(() => t.TaskState, new short?[] { (short)1, (short)2 })
                .orderByDesc(() => t.TaskState);
            var list = clip.select(t).queryList().ToList();

            return list.Count > 0 ? list[0] : null;
        }

        public IList<TaskViewEntity> GetTaskListEMailUnSent()
        {
            var clip = Sail.UseRunSQL().useClip();
            clip.from<TaskViewEntity>(out var t)
                .where(() => t.ProcessState, (short)2)
                .where(() => t.ActivityType == 4 || t.WorkItemType == 1)
                .where(() => t.ActivityState, (short)1)
                .where(() => t.IsEMailSent, (byte?)TaskEMailSentStatusEnum.UnSent)
                .where(() => t.TaskState, (short)32, "<>");
            return clip.select(t).queryList().ToList();
        }

        public bool SetTaskEMailSent(string taskId)
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
                        "更新任务邮件发送状态发生错误: " + e.Message, e);
                }
            }
        }

        public IList<Performer> GetTaskPerformers(FlowCommandContext runner)
        {
            var tasks = GetReadyTaskOfApp(runner).ToList();
            IList<Performer> performerList = new List<Performer>();
            foreach (var task in tasks)
            {
                performerList.Add(new Performer(task.AssignedUserId, task.AssignedUserName));
            }
            return performerList;
        }

        public IEnumerable<TaskViewEntity> GetReadyTaskOfApp(FlowCommandContext runner)
        {
            var clip = Sail.UseRunSQL().useClip();
            clip.from<TaskViewEntity>(out var t)
                .where(() => t.AppInstanceId, runner.AppInstanceId)
                .where(() => t.ProcessId, runner.ProcessId)
                .where(() => t.ProcessState, (short)2)
                .where(() => t.ActivityType == 4 || t.WorkItemType == 1)
                .where(() => t.ActivityState, (short)1)
                .orderByDesc(() => t.ActivatedDateTime);
            return clip.select(t).queryList();
        }

        /// <summary>
        /// 判断当前任务是否是最后一个任务（会签/加签/单节点）。
        /// </summary>
        public bool IsLastTask(string taskId)
        {
            var isLast = false;
            var tm = new TaskManager(Sail);
            var task = tm.GetTask(taskId);
            var aim = new ActivityInstanceManager(Sail);
            var activityInstance = aim.GetById(task.ActivityInstanceId ?? throw new InvalidOperationException("Task ActivityInstanceId is required."));

            if (aim.IsMultipleInstanceChildren(activityInstance) == true)
            {
                var mainActivityInstance = aim.GetById(activityInstance.MainActivityInstanceId);
                var complexType = EnumHelper.ParseEnum<ComplexTypeEnum>(mainActivityInstance.ComplexType.Value.ToString());

                if (complexType == ComplexTypeEnum.SignTogether)
                {
                    var mergeType = EnumHelper.ParseEnum<MergeTypeEnum>(mainActivityInstance.MergeType.Value.ToString());
                    if (mergeType == MergeTypeEnum.Sequence)
                    {
                        var sqList = aim.GetActivityMulitipleInstanceWithState(
                            mainActivityInstance.ActivityInstanceId,
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
                            mainActivityInstance.ActivityInstanceId,
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
                            mainActivityInstance.ActivityInstanceId,
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
                            mainActivityInstance.ActivityInstanceId,
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

        /// <summary>
        /// 按活动/任务状态分页查询任务视图。activityState: 1 Ready / 2 Running / 4 Completed(task)。
        /// </summary>
        public (IList<TaskViewEntity> Items, int Total) QueryTasksPaged(TaskQuery query, int activityState)
        {
            query ??= new TaskQuery();
            var pageSize = query.PageSize > 0 ? query.PageSize : 20;
            var pageIndex = query.PageIndex > 0 ? query.PageIndex : 1;

            var clip = Sail.UseRunSQL().useClip();
            clip.from<TaskViewEntity>(out var t)
                .where(() => t.ProcessState, (short)2)
                .where(() => t.ActivityType == 4 || t.WorkItemType == 1);

            if (activityState == (short)ActivityStateEnum.Ready
                || activityState == (short)ActivityStateEnum.Running)
            {
                clip.where(() => t.ActivityState, (short)activityState)
                    .where(() => t.TaskState, (short)32, "<>");
            }
            else if (activityState == (short)ActivityStateEnum.Completed)
            {
                clip.where(() => t.TaskState, (short)4);
            }

            // 非会签任务 ai_mn_activity_state 为 NULL；SQL 中 NULL<>4 为未知，会把整行滤掉
            if (activityState == 1)
            {
                clip.sinkOR()
                    .whereIsNull(() => t.MainActivityState)
                    .where(() => t.MainActivityState, (short)4, "<>")
                    .rise();
            }

            clip.whereIf(!string.IsNullOrEmpty(query.AppInstanceId), () => t.AppInstanceId, query.AppInstanceId)
                .whereIf(!string.IsNullOrEmpty(query.ProcessId), () => t.ProcessId, query.ProcessId)
                .whereIf(!string.IsNullOrEmpty(query.Version), () => t.Version, query.Version)
                .whereIf(!string.IsNullOrEmpty(query.UserId), () => t.AssignedUserId, query.UserId)
                .whereIf(!string.IsNullOrEmpty(query.EndedUserId), () => t.EndedUserId, query.EndedUserId);
            if (!string.IsNullOrEmpty(query.AppName))
                clip.whereLike(() => t.AppName, query.AppName);

            if (activityState == (short)ActivityStateEnum.Completed)
                clip.orderByDesc(() => t.EndedDateTime);
            else
                clip.orderByDesc(() => t.ActivatedDateTime);
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
            clip.from<ProcessInstanceEntity>(out var pi)
                .whereIf(!string.IsNullOrEmpty(query.ProcessId), () => pi.ProcessId, query.ProcessId)
                .whereIf(!string.IsNullOrEmpty(query.ApplicationInstanceId), () => pi.AppInstanceId, query.ApplicationInstanceId);
            if (!string.IsNullOrEmpty(query.AppName))
                clip.whereLike(() => pi.AppName, query.AppName);
            clip.whereIf(!string.IsNullOrEmpty(query.CreatedUserId), () => pi.CreatedUserId, query.CreatedUserId);
            if (query.ProcessState.HasValue)
                clip.where(() => pi.ProcessState, query.ProcessState.Value);

            clip.orderByDesc(() => pi.CreatedDateTime);
            var page = clip.select(pi).setPage(pageSize, pageIndex).queryPage();
            var items = (IList<ProcessInstanceEntity>)(page.Items?.ToList() ?? new List<ProcessInstanceEntity>());
            return (items, page.Total);
        }

        /// <summary>热表优先；未命中则按 endedYear 或候选年查冷表。</summary>
        public ProcessInstanceEntity GetProcessInstanceHotOrArchive(
            string processInstanceId,
            int? endedYear = null,
            IEnumerable<int> candidateYears = null)
        {
            var clip = Sail.UseRunSQL().useClip();
            clip.from<ProcessInstanceEntity>(out var pi)
                .where(() => pi.ProcessInstanceId, processInstanceId);
            var hot = clip.select(pi).queryList().FirstOrDefault();
            if (hot != null)
                return hot;

            var archive = Sail.CreateArchiveQuery();
            if (endedYear.HasValue)
                return archive.GetProcessInstance(processInstanceId, endedYear.Value);

            var years = candidateYears?.ToList();
            if (years == null || years.Count == 0)
            {
                var y = DateTime.UtcNow.Year;
                years = new List<int> { y, y - 1, y - 2 };
            }

            return archive.FindProcessInstance(processInstanceId, years);
        }

        /// <summary>Host 定时唤起：按 DataLifecycleOptions 保留窗迁出一批终态实例。</summary>
        public int RunProcessArchiveBatch(int? batchSize = null) =>
            Sail.RunProcessArchiveBatch(batchSize);

        #endregion

        #region Process catalog / monitor display

        public IList<ProcessEntity> GetProcessListSimple()
        {
            var clip = Sail.UseRunSQL().useClip();
            clip.from<ProcessEntity>(out var p)
                .orderByDesc(() => p.UpdatedDateTime);
            return clip.select(p).queryList().ToList();
        }

        public int GetProcessInstanceCount(string processId, string version)
        {
            var clip = Sail.UseRunSQL().useClip();
            clip.from<ProcessInstanceEntity>(out var pi)
                .where(() => pi.ProcessId, processId)
                .where(() => pi.Version, version);
            return clip.count();
        }

        public IList<NodeImage> GetActivityInstanceCompleted(string taskId)
        {
            var tm = new TaskManager(Sail);
            var task = tm.GetTaskView(taskId);
            return GetActivityInstanceCompleted(task.AppInstanceId, task.ProcessId);
        }

        public IList<NodeImage> GetActivityInstanceCompleted(FlowCommandContext runner)
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
                    Id = a.ActivityInstanceId,
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
                    Id = t.TransitionInstanceId,
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
