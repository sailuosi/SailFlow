using System.Collections.Generic;
using System.Linq;
using sailflow.engine.Common;
using sailflow.data;
using sailflow.engine.Xpdl;

namespace sailflow.assignee
{
    /// <summary>将 Task / 子 AI 投影为 <see cref="LeaveSeatSnapshot"/>（只读窄字段）。</summary>
    public static class LeaveSeatProjector
    {
        public static List<LeaveSeatSnapshot> FromTasks(
            IEnumerable<TaskEntity> tasks,
            IReadOnlyDictionary<string, ActivityInstanceEntity> activityById = null)
        {
            var list = new List<LeaveSeatSnapshot>();
            if (tasks == null)
                return list;

            foreach (var t in tasks)
            {
                if (t == null)
                    continue;
                if (AssigneeRoleHelper.IsParticipant(t.AssigneeRole))
                    continue;

                ActivityInstanceEntity ai = null;
                if (activityById != null
                    && !string.IsNullOrEmpty(t.ActivityInstanceId))
                    activityById.TryGetValue(t.ActivityInstanceId, out ai);

                var state = t.TaskState ?? 0;
                var closed = state == (short)TaskStateEnum.Closed
                             || state == (short)TaskStateEnum.Canceled;
                var completed = state == (short)TaskStateEnum.Completed;

                var agreed = false;
                var refused = false;
                if (completed)
                {
                    if (ai != null && ai.ApprovalStatus == (short)ApprovalStatusEnum.Refused)
                        refused = true;
                    else
                        agreed = true;
                }

                list.Add(new LeaveSeatSnapshot
                {
                    SignId = t.SignId,
                    SignName = t.SignName,
                    Weight = t.Weight.HasValue && t.Weight.Value > 0 ? t.Weight.Value : 1m,
                    ProxySignId = t.ProxySignId,
                    Agreed = agreed,
                    Refused = refused,
                    ClosedOrCancelled = closed,
                    TaskId = t.TaskId,
                    ActivityInstanceId = t.ActivityInstanceId,
                    GroupId = t.GroupId
                });
            }

            return list;
        }

        /// <summary>
        /// 无打开待办时的席位投影回落：用子 AI 上的 SignIds/Names 快照。
        /// 仅 Leave 计数/投影，非办理权判定（办理权见 Task.SignId）。
        /// </summary>
        public static List<LeaveSeatSnapshot> FromChildActivities(
            IEnumerable<ActivityInstanceEntity> children)
        {
            var list = new List<LeaveSeatSnapshot>();
            if (children == null)
                return list;

            foreach (var a in children)
            {
                if (a == null)
                    continue;

                var cancelled = a.ActivityState == (short)ActivityStateEnum.Cancelled
                                || a.ActivityState == (short)ActivityStateEnum.Withdrawed;

                list.Add(new LeaveSeatSnapshot
                {
                    SignId = a.SignIds,
                    SignName = a.SignNames,
                    Weight = a.Weight.HasValue && a.Weight.Value > 0 ? a.Weight.Value : 1m,
                    ProxySignId = a.ProxySignId,
                    Agreed = a.ApprovalStatus == (short)ApprovalStatusEnum.Agreed,
                    Refused = a.ApprovalStatus == (short)ApprovalStatusEnum.Refused,
                    ClosedOrCancelled = cancelled,
                    ActivityInstanceId = a.ActivityInstanceId,
                    GroupId = a.GroupId
                });
            }

            return list;
        }

        /// <summary>
        /// 优先 Task 投影（含同 AI 代批多 Task）；无 Task 时回落 <see cref="FromChildActivities"/>（非办理权）。
        /// </summary>
        public static List<LeaveSeatSnapshot> BuildSeats(
            IList<ActivityInstanceEntity> children,
            IList<TaskEntity> tasks)
        {
            var childList = children ?? new List<ActivityInstanceEntity>();
            var byAi = childList
                .Where(c => c != null && !string.IsNullOrEmpty(c.ActivityInstanceId))
                .GroupBy(c => c.ActivityInstanceId)
                .ToDictionary(g => g.Key, g => g.First());

            var fromTasks = FromTasks(tasks, byAi);
            if (fromTasks.Count > 0)
                return fromTasks;
            return FromChildActivities(childList);
        }

        /// <summary>
        /// 同级席位：主体 AI 的<strong>直属 Task</strong> ∪ <strong>直属子 AI（各折合一席）</strong>。
        /// 孙代不摊进本层；SignForward 容器未 Completed 则该席未同意。
        /// </summary>
        public static List<LeaveSeatSnapshot> BuildSeatsForActivity(
            ActivityInstanceEntity subject,
            IList<TaskEntity> directTasks,
            IList<ActivityInstanceEntity> directChildren,
            IList<TaskEntity> tasksOnDirectChildren = null)
        {
            var seats = new List<LeaveSeatSnapshot>();
            var subjectMap = new Dictionary<string, ActivityInstanceEntity>();
            if (subject != null && !string.IsNullOrEmpty(subject.ActivityInstanceId))
                subjectMap[subject.ActivityInstanceId] = subject;

            seats.AddRange(FromTasks(directTasks, subjectMap));

            var childList = directChildren ?? new List<ActivityInstanceEntity>();
            var tasksByChild = (tasksOnDirectChildren ?? new List<TaskEntity>())
                .Where(t => t != null && !string.IsNullOrEmpty(t.ActivityInstanceId))
                .GroupBy(t => t.ActivityInstanceId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var child in childList)
            {
                if (child == null)
                    continue;

                var cancelled = child.ActivityState == (short)ActivityStateEnum.Cancelled
                                || child.ActivityState == (short)ActivityStateEnum.Withdrawed;

                var agreed = false;
                var refused = false;
                var closed = cancelled;

                if (!cancelled
                    && tasksByChild.TryGetValue(child.ActivityInstanceId, out var cts)
                    && cts != null
                    && cts.Count > 0)
                {
                    var fromChildTasks = FromTasks(cts, new Dictionary<string, ActivityInstanceEntity>
                    {
                        [child.ActivityInstanceId] = child
                    });
                    // 折合一席：任一同根同意即同意；全拒且无打开则拒
                    if (fromChildTasks.Any(s => s.Agreed))
                        agreed = true;
                    else if (fromChildTasks.All(s => s.Refused || s.ClosedOrCancelled)
                             && fromChildTasks.Any(s => s.Refused))
                        refused = true;
                    else if (fromChildTasks.All(s => s.ClosedOrCancelled))
                        closed = true;
                }
                else if (!cancelled)
                {
                    agreed = child.ActivityState == (short)ActivityStateEnum.Completed
                             && child.ApprovalStatus == (short)ApprovalStatusEnum.Agreed;
                    refused = child.ActivityState == (short)ActivityStateEnum.Completed
                              && child.ApprovalStatus == (short)ApprovalStatusEnum.Refused;
                }

                // SignForward 容器：未完结前对外只占未同意一席（内部进度不外泄）
                var isSignForwardContainer = child.ComplexType == (short)ComplexTypeEnum.SignForward
                    && !string.IsNullOrEmpty(child.LeaveJudgeType);
                if (isSignForwardContainer
                    && child.ActivityState != (short)ActivityStateEnum.Completed)
                {
                    agreed = false;
                    refused = false;
                    closed = cancelled;
                }
                else if (isSignForwardContainer
                         && child.ActivityState == (short)ActivityStateEnum.Completed)
                {
                    agreed = child.ApprovalStatus == (short)ApprovalStatusEnum.Agreed
                             || child.ApprovalStatus == (short)ApprovalStatusEnum.Null;
                    refused = child.ApprovalStatus == (short)ApprovalStatusEnum.Refused;
                }

                seats.Add(new LeaveSeatSnapshot
                {
                    SignId = child.SignIds,
                    SignName = child.SignNames,
                    Weight = child.Weight.HasValue && child.Weight.Value > 0 ? child.Weight.Value : 1m,
                    ProxySignId = child.ProxySignId,
                    Agreed = agreed,
                    Refused = refused,
                    ClosedOrCancelled = closed,
                    ActivityInstanceId = child.ActivityInstanceId,
                    GroupId = child.GroupId
                });
            }

            return seats;
        }
    }
}
