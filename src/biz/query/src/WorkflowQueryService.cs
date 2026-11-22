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
                .whereIn(() => t.TaskState, new short?[]
                {
                    (short)TaskStateEnum.Waiting,
                    (short)TaskStateEnum.Reading
                })
                .orderByDesc(() => t.ActiveAt)
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
                .orderByDesc(() => t.CompletedAt)
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
                performerList.Add(new Performer(task.SignId, task.SignName));
            }
            return performerList;
        }

        public IEnumerable<TaskViewEntity> GetReadyTaskOfApp(FlowCommandContext runner)
        {
            var clip = Sail.UseRunSQL().useClip();
            clip.from<TaskViewEntity>(out var t)
                .where(() => t.BizPK, runner.BizPK)
                .where(() => t.ProcessId, runner.ProcessId)
                .where(() => t.ProcessState, (short)2)
                .where(() => t.ActivityType == 4 || t.WorkItemType == 1)
                .where(() => t.ActivityState, (short)1)
                .orderByDesc(() => t.ActiveAt);
            return clip.select(t).queryList();
        }

        /// <summary>
        /// 判断当前任务是否是最后一个任务（会签/加签/单节点）。
        /// 加签：容器末席且外层仍 Waiting → false；仅外层亦将 Passed 时 true。
        /// </summary>
        public bool IsLastTask(string taskId)
        {
            var isLast = false;
            var tm = new TaskManager(Sail);
            var task = tm.GetTask(taskId);
            var aim = new ActivityInstanceManager(Sail);
            var activityInstance = aim.GetById(task.ActivityInstanceId ?? throw new InvalidOperationException("Task ActivityInstanceId is required."));

            // 发起人直属 Task 挂在加签容器上
            if (ActivityInstanceManager.IsSignForwardContainer(activityInstance))
                return IsLastTaskSignForwardSeat(aim, activityInstance, activityInstance);

            if (aim.IsMultipleInstanceChildren(activityInstance) == true)
            {
                var mainActivityInstance = aim.GetById(activityInstance.MainActInstanceId);
                if (mainActivityInstance?.ComplexType == null)
                    return false;

                // 叶子挂在加签容器下
                if (ActivityInstanceManager.IsSignForwardContainer(mainActivityInstance))
                    return IsLastTaskSignForwardSeat(aim, activityInstance, mainActivityInstance);

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
                        // 总人数按主 AI 下子实例数（勿依赖可能滞后的 SignIds 汇总）
                        short allNum = (short)aim.GetActivityMulitipleInstanceWithState(
                            mainActivityInstance.ActivityInstanceId,
                            mainActivityInstance.ProcessInstanceId,
                            null).Count;
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
                        // 与 Mediator 一致：只认 Completed；当前子 AI 计入「若我现在通过是否达阈」
                        var completedCount = sqList.Count(w =>
                            w.ActivityState == (short)ActivityStateEnum.Completed
                            || w.ActivityInstanceId == activityInstance.ActivityInstanceId);
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
                            isLast = completedCount >= mainActivityInstance.CompleteOrder;
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
        /// 加签席：先看容器内是否末席；末席再看外层是否仍 Waiting（外层 Waiting → 非 Last）。
        /// </summary>
        private bool IsLastTaskSignForwardSeat(
            ActivityInstanceManager aim,
            ActivityInstanceEntity current,
            ActivityInstanceEntity container)
        {
            var children = aim.GetActivityMulitipleInstanceWithState(
                container.ActivityInstanceId,
                container.ProcessInstanceId,
                null).ToList();

            var unfinishedChildren = children.Where(c =>
                c.ActivityState == (short)ActivityStateEnum.Ready
                || c.ActivityState == (short)ActivityStateEnum.Running
                || c.ActivityState == (short)ActivityStateEnum.Suspended).ToList();

            var onContainer = string.Equals(
                current.ActivityInstanceId, container.ActivityInstanceId, StringComparison.OrdinalIgnoreCase);

            var mergeType = container.MergeType != null
                ? EnumHelper.ParseEnum<MergeTypeEnum>(container.MergeType.Value.ToString())
                : MergeTypeEnum.Parallel;

            // 容器直属 Task：仍有未完成加签子 → 非末
            if (onContainer)
            {
                if (unfinishedChildren.Count > 0)
                    return false;
            }
            else
            {
                // 叶子：仍有其他未完成子 → 非末
                if (unfinishedChildren.Any(c =>
                    !string.Equals(c.ActivityInstanceId, current.ActivityInstanceId, StringComparison.OrdinalIgnoreCase)))
                    return false;

                if (mergeType == MergeTypeEnum.Sequence)
                {
                    var ordered = children.Where(x => x.CompleteOrder != null && x.CompleteOrder > 0).ToList();
                    float maxOrder = ordered.Count > 0 ? ordered.Max(t => t.CompleteOrder.Value) : 0;
                    // 前加签：末序后还要发起人直属 Task（容器未 Completed）
                    if (container.SignForwardType == (short)SignForwardTypeEnum.SignForwardBefore
                        && (current.CompleteOrder == null || current.CompleteOrder >= maxOrder)
                        && container.ActivityState != (short)ActivityStateEnum.Completed)
                        return false;
                    if (current.CompleteOrder == null || current.CompleteOrder < maxOrder)
                        return false;
                }
                else
                {
                    // 并行：容器上发起人直属打开待办仍在 → 非末（同级席未齐）
                    if (HasOpenInitiatorTaskOnContainer(container))
                        return false;
                }
            }

            // 容器将达阈 → 外层 Race 可 Last；否则有未完成兄弟席则非 Last
            if (string.IsNullOrEmpty(container.MainActInstanceId))
                return true;

            var parent = aim.GetById(container.MainActInstanceId);
            if (parent == null)
                return true;

            if (string.Equals(parent.LeaveJudgeType, "Race", StringComparison.OrdinalIgnoreCase))
                return true;

            var siblingChildren = aim.GetActivityMulitipleInstanceWithState(
                parent.ActivityInstanceId, parent.ProcessInstanceId, null)
                .Where(c => c.ActivityState == (short)ActivityStateEnum.Ready
                    || c.ActivityState == (short)ActivityStateEnum.Running
                    || c.ActivityState == (short)ActivityStateEnum.Suspended)
                .Where(c => !string.Equals(c.ActivityInstanceId, container.ActivityInstanceId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            // 保守：外层非 Race 且存在其他活动席/兄弟 → 非 Last（扁平旁人打开待办由工作台另判）
            return siblingChildren.Count == 0;
        }

        /// <summary>并加签同级：容器上发起人（SignIds）仍有 Waiting/Reading 审批待办。</summary>
        private bool HasOpenInitiatorTaskOnContainer(ActivityInstanceEntity container)
        {
            if (container == null
                || string.IsNullOrEmpty(container.SignIds)
                || string.IsNullOrEmpty(container.ActivityInstanceId)
                || string.IsNullOrEmpty(container.ProcessInstanceId))
                return false;

            var clip = Sail.UseRunSQL().useClip();
            clip.from<TaskEntity>(out var t)
                .where(() => t.ProcessInstanceId, container.ProcessInstanceId)
                .where(() => t.ActivityInstanceId, container.ActivityInstanceId)
                .where(() => t.SignId, container.SignIds)
                .whereIn(() => t.TaskState, new short?[]
                {
                    (short)TaskStateEnum.Waiting,
                    (short)TaskStateEnum.Reading
                });
            return clip.select(t).queryList().Any();
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
                // 个人待办开闭看 TaskState：只认 Waiting/Reading，勿仅靠 AI Ready（多人未结时他人已办仍会进列表）
                clip.where(() => t.ActivityState, (short)activityState)
                    .whereIn(() => t.TaskState, new short?[]
                    {
                        (short)TaskStateEnum.Waiting,
                        (short)TaskStateEnum.Reading
                    });
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

            clip.whereIf(!string.IsNullOrEmpty(query.BizPK), () => t.BizPK, query.BizPK)
                .whereIf(!string.IsNullOrEmpty(query.ProcessId), () => t.ProcessId, query.ProcessId)
                .whereIf(!string.IsNullOrEmpty(query.Version), () => t.Version, query.Version)
                .whereIf(!string.IsNullOrEmpty(query.UserId), () => t.SignId, query.UserId)
                .whereIf(!string.IsNullOrEmpty(query.EndedUserId), () => t.EndedUserId, query.EndedUserId);
            if (!string.IsNullOrEmpty(query.BizChannel))
                clip.whereLike(() => t.BizChannel, query.BizChannel);

            if (activityState == (short)ActivityStateEnum.Completed)
                clip.orderByDesc(() => t.CompletedAt);
            else
                clip.orderByDesc(() => t.ActiveAt);
            // 库端分页（禁止全量 queryList 再内存切页）
            var page = clip.select(t).setPage(pageSize, pageIndex).queryPage();
            var items = (IList<TaskViewEntity>)(page.Items?.ToList() ?? new List<TaskViewEntity>());
            return (items, page.Total);
        }

        /// <summary>
        /// 按 BizChannel + BizPK 批量查进度摘要（上限 1000）。权限过滤归宿主。
        /// </summary>
        public IList<BizProgressSummary> QueryProgressByBizPKs(
            string bizChannel,
            IReadOnlyList<string> bizPKs,
            int maxCount = 1000)
        {
            if (string.IsNullOrWhiteSpace(bizChannel))
                throw new ArgumentException("BizChannel required", nameof(bizChannel));
            if (bizPKs == null || bizPKs.Count == 0)
                return Array.Empty<BizProgressSummary>();

            var limit = maxCount <= 0 ? 1000 : Math.Min(maxCount, 1000);
            var keys = bizPKs
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(limit)
                .ToArray();
            if (keys.Length == 0)
                return Array.Empty<BizProgressSummary>();

            var channel = bizChannel.Trim();
            var clip = Sail.UseRunSQL().useClip();
            clip.from<ProcessInstanceEntity>(out var pi)
                .where(() => pi.BizChannel, channel)
                .whereIn(() => pi.BizPK, keys)
                .orderByDesc(() => pi.CreatedDateTime);
            var rows = clip.select(pi).queryList().ToList();

            // 同一 BizPK 多实例时取最新一条
            var map = new Dictionary<string, ProcessInstanceEntity>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.BizPK)) continue;
                if (!map.ContainsKey(row.BizPK))
                    map[row.BizPK] = row;
            }

            var list = new List<BizProgressSummary>(map.Count);
            foreach (var kv in map)
            {
                var e = kv.Value;
                list.Add(new BizProgressSummary
                {
                    BizChannel = e.BizChannel ?? channel,
                    BizPK = e.BizPK,
                    ProcessInstanceId = e.ProcessInstanceId,
                    ProcessId = e.ProcessId,
                    Version = e.Version,
                    Title = e.Title,
                    ProcessState = e.ProcessState,
                    CreatedUserId = e.CreatedUserId,
                    CreatedDateTime = e.CreatedDateTime,
                    EndedDateTime = e.EndedDateTime
                });
            }
            return list;
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
                .whereIf(!string.IsNullOrEmpty(query.BizPK), () => pi.BizPK, query.BizPK);
            if (!string.IsNullOrEmpty(query.BizChannel))
                clip.whereLike(() => pi.BizChannel, query.BizChannel);
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
            return GetActivityInstanceCompleted(task.BizPK, task.ProcessId);
        }

        public IList<NodeImage> GetActivityInstanceCompleted(FlowCommandContext runner)
            => GetActivityInstanceCompleted(runner.BizPK, runner.ProcessId);

        public IList<NodeImage> GetActivityInstanceCompleted(string bizPK, string processId)
        {
            IList<NodeImage> imageList = new List<NodeImage>();
            var am = new ActivityInstanceManager(Sail);
            var list = am.GetActivityInstanceListCompleted(bizPK, processId);

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
            var list = tm.GetTransitionInstanceList(query.BizPK, query.ProcessId, query.Version).ToList();

            foreach (TrackEntity t in list)
            {
                imageList.Add(new TransitionImage
                {
                    Id = t.TrackId,
                    TransitionId = t.TransitionId
                });
            }
            return imageList;
        }

        public IList<ActivityInstanceEntity> GetRunningActivityInstance(TaskQuery query)
        {
            var aim = new ActivityInstanceManager(Sail);
            return aim.GetRunningActivityInstanceList(query.BizPK, query.ProcessId, query.Version).ToList();
        }

        #endregion
    }
}
