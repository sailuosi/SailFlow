using System.Collections.Generic;
using System.Linq;
using sailflow.engine.Common;
using sailflow.engine.Entity;

namespace sailflow.engine.Assignee
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
                    // 回落路径：子 AI 单人席 SignIds 即席位人；有 Task 时 BuildSeats 不会走到这里
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
    }
}
