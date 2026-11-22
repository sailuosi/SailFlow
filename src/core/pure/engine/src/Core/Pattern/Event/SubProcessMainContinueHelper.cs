using System.Collections.Generic;
using mooSQL.data;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.engine.Xpdl;
using sailflow.hub.Resource;

namespace sailflow.engine.Pattern
{
    /// <summary>
    /// 子流程结束后续推主流程时的下一跳办理人预填（四处 End 中介共用）。
    /// </summary>
    internal static class SubProcessMainContinueHelper
    {
        /// <summary>
        /// 按主流程被调子活动出口预填 <see cref="FlowCommandContext.NextActivityPerformers"/>。
        /// Roles 有值时按角色展开；为空且非 End 时回落为办结人（对齐宿主首跳 ApproverPick 默认）。
        /// </summary>
        internal static FlowCommandContext FillNextActivityPerformers(
            SailClient sail,
            string mainActivityInstanceId,
            string mainActivityId,
            SQLBuilder kit)
        {
            var pm = sail.ProcessInstances;
            var mainProcessInstance = pm.GetByActivity(kit, mainActivityInstanceId);
            var processModel = sail.ProcessModelFactory.CreateByProcessInstance(kit, mainProcessInstance);
            var nextSteps = processModel.GetNextActivityTreeView(mainActivityId);

            var task = sail.ManagerFactory.Tasks.GetTaskByActivity(
                kit.useSQL(), mainProcessInstance.ProcessInstanceId, mainActivityInstanceId);
            var runner = new FlowCommandContext
            {
                BizChannel = mainProcessInstance.BizChannel,
                BizPK = mainProcessInstance.BizPK,
                BizCode = mainProcessInstance.BizCode,
                ProcessId = mainProcessInstance.ProcessId,
                Version = mainProcessInstance.Version,
                UserId = task.SignId,
                UserName = task.SignName
            };

            var dict = new Dictionary<string, PerformerList>();
            foreach (var node in nextSteps ?? System.Array.Empty<NodeView>())
            {
                var roles = node.Roles ?? new List<Role>();
                var performerList = PerformerBuilder.CreatePerformerList(roles);
                if (node.ActivityType != ActivityTypeEnum.EndNode
                    && performerList.Count == 0
                    && !string.IsNullOrWhiteSpace(runner.UserId))
                {
                    performerList = PerformerBuilder.CreatePerformerList(
                        runner.UserId,
                        string.IsNullOrWhiteSpace(runner.UserName) ? runner.UserId : runner.UserName);
                }

                dict[node.ActivityId] = performerList;
            }

            runner.NextActivityPerformers = dict;
            return runner;
        }
    }
}
