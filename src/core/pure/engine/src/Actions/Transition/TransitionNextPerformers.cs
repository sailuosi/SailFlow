using System.Collections.Generic;
using sailflow.engine.Client;
using sailflow.utils;

namespace sailflow.lanes
{
    /// <summary>Reject / Close / Jump 共用：按 JumpOption 填充下一办理人。</summary>
    internal static class TransitionNextPerformers
    {
        public static IDictionary<string, PerformerList> Fill(
            SailClient sail,
            FlowCommandContext runner,
            JumpOptionEnum jumpOption)
        {
            IDictionary<string, PerformerList> nextActivityPerformers = null;
            var pm = sail.ProcessModelFactory.CreateByProcess(runner.ProcessId, runner.Version);
            if (jumpOption == JumpOptionEnum.Default)
            {
                nextActivityPerformers = runner.NextActivityPerformers;
            }
            else if (jumpOption == JumpOptionEnum.Startup)
            {
                var firstActivity = pm.GetFirstActivity();
                var aim = sail.ManagerFactory.Activities;
                var firstActivityInstance = aim.GetActivityInstanceLatest(runner.BizPK, runner.ProcessId, firstActivity.ActivityId);
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
                var performer = new Performer(runner.UserId, runner.UserName);
                var performerList = new PerformerList();
                performerList.Add(performer);
                nextActivityPerformers.Add(endActivity.ActivityId, performerList);
            }
            return nextActivityPerformers;
        }
    }
}
