using System;
using System.Collections.Generic;
using mooSQL.data;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.engine.Entity;
using sailflow.engine.Runtime;
using sailflow.engine.Xpdl;
using sailflow.hub.Resource;

namespace sailflow.engine.Pattern
{
    /// <summary>
    /// 子流程结束后续推主流程时的下一跳办理人预填与完成态校验（四处 End / SubProcess 中介共用）。
    /// </summary>
    internal static class SubProcessMainContinueHelper
    {
        /// <summary>同步续推时写入 runner.DynamicVariables，供主流程 SubProcess 中介确定性校验。</summary>
        internal const string DynCompletedSubProcessInstanceId = "sail.completedSubProcessInstanceId";

        /// <summary>
        /// 按主流程被调子活动出口预填 <see cref="FlowCommandContext.NextActivityPerformers"/>。
        /// Roles 有值时按角色展开；为空且非 End 时回落为办结人（对齐宿主首跳 ApproverPick 默认）。
        /// </summary>
        /// <param name="completedSubProcessInstanceId">刚 Complete 的子流程实例 Id；写入 DynamicVariables 供续推校验。</param>
        internal static FlowCommandContext FillNextActivityPerformers(
            SailClient sail,
            string mainActivityInstanceId,
            string mainActivityId,
            SQLBuilder kit,
            string completedSubProcessInstanceId = null)
        {
            var pm = sail.ProcessInstances;
            var mainProcessInstance = pm.GetByActivity(kit, mainActivityInstanceId);
            var processModel = sail.ProcessModelFactory.CreateByProcessInstance(kit, mainProcessInstance);
            var nextSteps = processModel.GetNextActivityTreeView(mainActivityId);

            var task = sail.ManagerFactory.Tasks.GetTaskByActivity(
                kit.useSQL(), mainProcessInstance.ProcessInstanceId, mainActivityInstanceId);
            if (task == null || string.IsNullOrWhiteSpace(task.TaskId))
            {
                throw new WfRuntimeException(
                    "Main subprocess shell task missing for ActivityInstanceId="
                    + mainActivityInstanceId);
            }

            var runner = new FlowCommandContext
            {
                BizChannel = mainProcessInstance.BizChannel,
                BizPK = mainProcessInstance.BizPK,
                BizCode = mainProcessInstance.BizCode,
                ProcessId = mainProcessInstance.ProcessId,
                Version = mainProcessInstance.Version,
                UserId = task.SignId,
                UserName = task.SignName,
                TaskId = task.TaskId,
                ActivityInstanceId = mainActivityInstanceId
            };

            if (!string.IsNullOrWhiteSpace(completedSubProcessInstanceId))
            {
                runner.DynamicVariables ??= new Dictionary<string, string>();
                runner.DynamicVariables[DynCompletedSubProcessInstanceId] = completedSubProcessInstanceId.Trim();
            }

            var dict = new Dictionary<string, PerformerList>();
            foreach (var node in nextSteps ?? Array.Empty<NodeView>())
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

        /// <summary>
        /// 主流程 SubProcess 节点推进前：优先用同步续推写入的子 PI Id 校验；否则回落库查。
        /// </summary>
        internal static bool IsInvokedSubProcessCompleted(
            SailClient sail,
            SQLBuilder kit,
            FlowCommandContext runner,
            ActivityInstanceEntity fromActivityInstance)
        {
            if (fromActivityInstance == null
                || string.IsNullOrWhiteSpace(fromActivityInstance.ActivityInstanceId))
                return false;

            var pim = sail.ProcessInstances;
            if (runner?.DynamicVariables != null
                && runner.DynamicVariables.TryGetValue(DynCompletedSubProcessInstanceId, out var completedId)
                && !string.IsNullOrWhiteSpace(completedId))
            {
                var child = pim.GetById(kit, completedId.Trim());
                if (child == null)
                    return false;
                if (child.ProcessState != (short)ProcessStateEnum.Completed)
                    return false;
                if (!string.Equals(
                        child.InvokedActInstanceId,
                        fromActivityInstance.ActivityInstanceId,
                        StringComparison.OrdinalIgnoreCase))
                    return false;
                if (!string.IsNullOrWhiteSpace(child.InvokedActivityId)
                    && !string.IsNullOrWhiteSpace(fromActivityInstance.ActivityId)
                    && !string.Equals(
                        child.InvokedActivityId,
                        fromActivityInstance.ActivityId,
                        StringComparison.OrdinalIgnoreCase))
                    return false;
                return true;
            }

            return pim.CheckSubProcessInstanceCompleted(
                kit,
                fromActivityInstance.ActivityInstanceId,
                fromActivityInstance.ActivityId);
        }
    }
}
