using System.Collections.Generic;
using sailflow.hosting;
using sailflow.utils;

namespace sailflow.lanes
{
    /// <summary>Reject / Close / Jump 共用：按 JumpOption 填充下一办理人。</summary>
    internal static class TransitionNextPerformers
    {
        public static IDictionary<string, PerformerList> Fill(
            SailClient sail,
            FlowCommandContext context,
            JumpOptionEnum jumpOption)
        {
            IDictionary<string, PerformerList> nextActivityPerformers = null;
            var pm = sail.ProcessModelFactory.CreateByProcess(context.ProcessId, context.Version);
            if (jumpOption == JumpOptionEnum.Default)
            {
                nextActivityPerformers = context.NextActivityPerformers;
            }
            else if (jumpOption == JumpOptionEnum.Startup)
            {
                var firstActivity = pm.GetFirstActivity();
                var aim = sail.ManagerFactory.Activities;
                var firstActivityInstance = aim.GetActivityInstanceLatest(context.BizPK, context.ProcessId, firstActivity.ActivityId);
                nextActivityPerformers = new Dictionary<string, PerformerList>();
                var performer = new Performer(firstActivityInstance.CreatedUserId, firstActivityInstance.CreatedUserName);
                var performerList = new PerformerList();
                performerList.Add(performer);
                nextActivityPerformers.Add(firstActivity.ActivityId, performerList);
            }
            else if (jumpOption == JumpOptionEnum.End)
            {
                var endActivity = pm.GetEndActivity();
                nextActivityPerformers = new Dictionary<string, PerformerList>();
                var performer = new Performer(context.UserId, context.UserName);
                var performerList = new PerformerList();
                performerList.Add(performer);
                nextActivityPerformers.Add(endActivity.ActivityId, performerList);
            }
            return nextActivityPerformers;
        }
    }
}
