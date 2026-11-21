using System;
using System.Collections.Generic;
using System.Linq;
using sailflow.engine.Common;
using sailflow.data;
using mooSQL.data;
using sailflow.engine.Xpdl;
using sailflow.engine.Entity;
using sailflow.engine.Result;
using sailflow.engine.Client;
using sailflow.engine.Core;

namespace sailflow.engine.Pattern
{
    /// <summary>
    /// 加签子节点执行：办结后以主 AI Leave（TryCanLeave）判定；Waiting+Sequence 唤醒下一席；Passed 塌缩主容器。
    /// </summary>
    internal class NodeMediatorMultiSignForward : NodeMediator
    {
        internal NodeMediatorMultiSignForward(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {
        }

        public override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            try
            {
                OnBeforeExecuteWorkItem();

                bool canContinueForwardCurrentNode = CompleteWorkItem(ActivityForwardContext.TaskId,
                    ActivityForwardContext.ActivityResource,
                    this.Kit);

                OnAfterExecuteWorkItem();

                if (canContinueForwardCurrentNode)
                {
                    ContinueForwardCurrentNode(ActivityForwardContext.IsNotParsedByTransition, this.Kit);
                }
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        internal bool CompleteWorkItem(string taskId,
            ActivityResource activityResource,
            SQLBuilder kit)
        {
            bool canContinueForwardCurrentNode = true;

            FlowCommandContext runner = new FlowCommandContext
            {
                UserId = activityResource.AppRunner.UserId,
                UserName = activityResource.AppRunner.UserName
            };

            if (taskId != null)
            {
                if (base.TaskManager.TryCompleteAsDelegationReturn(
                        kit.useSQL(), taskId, activityResource.AppRunner))
                    return false;

                base.TaskManager.Complete(kit.useSQL(), taskId, activityResource.AppRunner);
            }

            base.ActivityInstanceManager.Complete(kit.useSQL(),
                base.LinkContext.FromActivityInstance.ActivityInstanceId, activityResource.AppRunner);
            base.LinkContext.FromActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;

            if (base.ActivityForwardContext.IsNotParsedByTransition == true)
                return true;

            var fromChild = base.LinkContext.FromActivityInstance;
            if (fromChild.MainActInstanceId == null
                || fromChild.SignForwardType == null
                || fromChild.SignForwardType == (short)SignForwardTypeEnum.None)
                return canContinueForwardCurrentNode;

            var mainActivityInstance = base.ActivityInstanceManager.GetById(fromChild.MainActInstanceId);
            if (mainActivityInstance == null)
                return canContinueForwardCurrentNode;

            // 统一走 Leave（AI LeaveJudgeType 优先）
            var passedType = base.ActivityInstanceManager.CheckMIPassRateInfo(
                kit.useSQL(), fromChild, mainActivityInstance);

            if (passedType == NodePassedTypeEnum.Passed
                || passedType == NodePassedTypeEnum.NotPassed)
            {
                mainActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
                base.ActivityInstanceManager.Update(kit.useSQL(), mainActivityInstance);
                base.ActivityInstanceManager.CancelUnCompletedMultipleInstance(
                    kit.useSQL(), mainActivityInstance.ActivityInstanceId, runner);
                canContinueForwardCurrentNode = true;
                return canContinueForwardCurrentNode;
            }

            // Waiting：串行唤醒下一挂起子；并行继续等
            var mergeType = mainActivityInstance.MergeType ?? (short)MergeTypeEnum.Parallel;
            if (mergeType == (short)MergeTypeEnum.Sequence)
            {
                var suspended = base.ActivityInstanceManager.GetActivityMulitipleInstanceWithState(
                        kit.useSQL(),
                        mainActivityInstance.ActivityInstanceId,
                        fromChild.ProcessInstanceId,
                        (short)ActivityStateEnum.Suspended)
                    .OrderBy(a => a.CompleteOrder ?? 0)
                    .ToList();

                if (suspended.Count > 0)
                {
                    var next = suspended[0];
                    next.ActivityState = (short)ActivityStateEnum.Ready;
                    base.ActivityInstanceManager.Update(kit.useSQL(), next);
                    base.FireActivityReady(kit, next, runner);

                    mainActivityInstance.NextStepPerformers = NextStepUtility.SerializeNextStepPerformers(
                        base.ActivityForwardContext.ActivityResource.AppRunner.NextActivityPerformers,
                        Sail.JsonSerializer);
                    base.ActivityInstanceManager.Update(kit.useSQL(), mainActivityInstance);
                    base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.ForwardToNextSequenceTask;
                }
                else
                {
                    base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.WaitingForCompletedMore;
                }
            }
            else
            {
                mainActivityInstance.NextStepPerformers = NextStepUtility.SerializeNextStepPerformers(
                    base.ActivityForwardContext.ActivityResource.AppRunner.NextActivityPerformers,
                    Sail.JsonSerializer);
                base.ActivityInstanceManager.Update(kit.useSQL(), mainActivityInstance);
                base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.WaitingForCompletedMore;
            }

            canContinueForwardCurrentNode = false;
            return canContinueForwardCurrentNode;
        }
    }
}
