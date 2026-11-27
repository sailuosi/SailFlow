using System;
using System.Collections.Generic;
using System.Linq;
using sailflow.data;

namespace sailflow.utils
{
    /// <summary>
    /// TakeOn（认领/承办）纯判定：Leave 是否允许、同根席位、待关同伴过滤。
    /// </summary>
    public static class TakeOnPeerHelper
    {
        public const string LeaveRace = "Race";
        public const string LeaveGroupRace = "GroupRace";

        public static bool IsTakeOnLeaveAllowed(string leaveJudgeType)
        {
            if (string.IsNullOrWhiteSpace(leaveJudgeType))
                return false;
            return string.Equals(leaveJudgeType, LeaveRace, StringComparison.OrdinalIgnoreCase)
                || string.Equals(leaveJudgeType, LeaveGroupRace, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsGroupRace(string leaveJudgeType) =>
            string.Equals(leaveJudgeType, LeaveGroupRace, StringComparison.OrdinalIgnoreCase);

        /// <summary>是否与认领人同根席（含代批 Proxy 链）。</summary>
        public static bool IsOwnSeat(
            string taskSignId,
            string taskProxySignId,
            string claimerSignId,
            string claimerProxySignId)
        {
            if (string.IsNullOrWhiteSpace(claimerSignId) && string.IsNullOrWhiteSpace(claimerProxySignId))
                return false;

            var claimerRoot = string.IsNullOrWhiteSpace(claimerProxySignId)
                ? claimerSignId
                : claimerProxySignId;
            var taskRoot = string.IsNullOrWhiteSpace(taskProxySignId)
                ? taskSignId
                : taskProxySignId;

            if (!string.IsNullOrWhiteSpace(claimerRoot)
                && string.Equals(taskRoot, claimerRoot, StringComparison.OrdinalIgnoreCase))
                return true;
            if (!string.IsNullOrWhiteSpace(claimerSignId)
                && string.Equals(taskSignId, claimerSignId, StringComparison.OrdinalIgnoreCase))
                return true;
            if (!string.IsNullOrWhiteSpace(claimerSignId)
                && string.Equals(taskProxySignId, claimerSignId, StringComparison.OrdinalIgnoreCase))
                return true;
            if (!string.IsNullOrWhiteSpace(claimerProxySignId)
                && string.Equals(taskSignId, claimerProxySignId, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        public static bool IsOwnSeat(TaskEntity task, string claimerSignId, string claimerProxySignId) =>
            task != null
            && IsOwnSeat(task.SignId, task.ProxySignId, claimerSignId, claimerProxySignId);

        /// <summary>
        /// 从打开办理人待办中筛出应被 TakeOn 旁关的同伴。
        /// GroupRace：仅同 GroupId；GroupId 皆空时退化为全员旁关。
        /// </summary>
        public static List<TaskEntity> FilterPeersToClose(
            IEnumerable<TaskEntity> openApprovers,
            string claimerTaskId,
            string claimerSignId,
            string claimerProxySignId,
            string claimerGroupId,
            string leaveJudgeType)
        {
            var list = openApprovers?.Where(t => t != null).ToList() ?? new List<TaskEntity>();
            var groupRace = IsGroupRace(leaveJudgeType);
            var restrictGroup = groupRace && !string.IsNullOrWhiteSpace(claimerGroupId);

            return list.Where(t =>
            {
                if (!string.IsNullOrWhiteSpace(claimerTaskId)
                    && string.Equals(t.TaskId, claimerTaskId, StringComparison.OrdinalIgnoreCase))
                    return false;
                if (IsOwnSeat(t, claimerSignId, claimerProxySignId))
                    return false;
                if (restrictGroup
                    && !string.Equals(t.GroupId, claimerGroupId, StringComparison.OrdinalIgnoreCase))
                    return false;
                return true;
            }).ToList();
        }
    }
}
