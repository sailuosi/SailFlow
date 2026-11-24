using System;
using System.Collections.Generic;
using System.Linq;
using sailflow.utils;
using sailflow.data;
using mooSQL.data;
using sailflow.xml;
using sailflow.engine.Result;
using sailflow.engine.Client;
using sailflow.run;

namespace sailflow.run{
    /// <summary>
    /// 加签办结：容器内 TryCanLeave；Waiting 唤醒下一子；Passed 塌缩并折合一票上浮，仅节点 Passed 才 ContinueForward。
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
            FlowCommandContext runner = new FlowCommandContext
            {
                UserId = activityResource.AppRunner.UserId,
                UserName = activityResource.AppRunner.UserName
            };

            if (taskId != null)
            {
                if (base.ITaskManager.TryCompleteAsDelegationReturn(
                        kit.useSQL(), taskId, activityResource.AppRunner))
                    return false;

                base.ITaskManager.Complete(kit.useSQL(), taskId, activityResource.AppRunner);
            }

            if (base.ActivityForwardContext.IsNotParsedByTransition == true)
                return true;

            var from = base.LinkContext.FromActivityInstance;
            if (from == null)
                return false;

            // 容器直属 Task：不 Complete 容器 AI
            if (IActivityInstanceManager.IsSignForwardContainer(from))
            {
                return EvaluateContainerAndBubble(kit, from, from, activityResource, runner);
            }

            // 叶子子 AI：完结叶子，再问父容器
            base.IActivityInstanceManager.Complete(kit.useSQL(), from.ActivityInstanceId, activityResource.AppRunner);
            from.ActivityState = (short)ActivityStateEnum.Completed;
            base.IActivityInstanceManager.SetApprovalStatus(kit.useSQL(), from.ActivityInstanceId);

            if (string.IsNullOrEmpty(from.MainActInstanceId))
                return true;

            var container = base.IActivityInstanceManager.GetById(from.MainActInstanceId);
            if (container == null)
                return true;

            if (!IActivityInstanceManager.IsSignForwardContainer(container))
            {
                // 父是 SignTogether 主等：沿用会签阈
                var passedType = base.IActivityInstanceManager.CheckMIPassRateInfo(kit.useSQL(), from, container);
                if (passedType == NodePassedTypeEnum.Passed || passedType == NodePassedTypeEnum.NotPassed)
                {
                    container.ActivityState = (short)ActivityStateEnum.Completed;
                    base.IActivityInstanceManager.Update(kit.useSQL(), container);
                    base.IActivityInstanceManager.CancelUnCompletedMultipleInstance(
                        kit.useSQL(), container.ActivityInstanceId, runner);
                    return true;
                }
                HandleSequenceWaiting(kit, container, from, activityResource, runner);
                return false;
            }

            return EvaluateContainerAndBubble(kit, from, container, activityResource, runner);
        }

        /// <summary>容器 Leave → Waiting 唤醒 / Passed 塌缩上浮。</summary>
        private bool EvaluateContainerAndBubble(
            SQLBuilder kit,
            ActivityInstanceEntity current,
            ActivityInstanceEntity container,
            ActivityResource activityResource,
            FlowCommandContext runner)
        {
            var passedType = base.IActivityInstanceManager.EvaluateLeaveOnActivity(
                kit.useSQL(), current, container);

            if (passedType == NodePassedTypeEnum.Passed
                || passedType == NodePassedTypeEnum.NotPassed)
            {
                return CollapseSignForwardAndBubbleLeave(
                    kit, container, activityResource, runner,
                    passedType == NodePassedTypeEnum.Passed);
            }

            HandleSequenceWaiting(kit, container, current, activityResource, runner);
            return false;
        }

        /// <summary>
        /// 塌缩容器并折合一票上浮；返回是否应 ContinueForward（仅节点 Leave Passed）。
        /// </summary>
        internal bool CollapseSignForwardAndBubbleLeave(
            SQLBuilder kit,
            ActivityInstanceEntity container,
            ActivityResource activityResource,
            FlowCommandContext runner,
            bool agreed)
        {
            container.ApprovalStatus = agreed
                ? (short)ApprovalStatusEnum.Agreed
                : (short)ApprovalStatusEnum.Refused;
            container.ActivityState = (short)ActivityStateEnum.Completed;
            base.IActivityInstanceManager.Update(kit.useSQL(), container);
            base.IActivityInstanceManager.CancelUnCompletedMultipleInstance(
                kit.useSQL(), container.ActivityInstanceId, runner);

            // 无父：异常形态，视为可出
            if (string.IsNullOrEmpty(container.MainActInstanceId))
                return true;

            var parent = base.IActivityInstanceManager.GetById(container.MainActInstanceId);
            if (parent == null)
                return true;

            // 父仍是加签容器 → 递归
            if (IActivityInstanceManager.IsSignForwardContainer(parent))
            {
                var parentPass = base.IActivityInstanceManager.EvaluateLeaveOnActivity(
                    kit.useSQL(), container, parent);
                if (parentPass == NodePassedTypeEnum.Passed
                    || parentPass == NodePassedTypeEnum.NotPassed)
                {
                    return CollapseSignForwardAndBubbleLeave(
                        kit, parent, activityResource, runner,
                        parentPass == NodePassedTypeEnum.Passed);
                }
                HandleSequenceWaiting(kit, parent, container, activityResource, runner);
                return false;
            }

            // 父为节点（或 SignTogether 主）：问外层 Leave
            var nodePass = base.IActivityInstanceManager.EvaluateLeaveOnActivity(
                kit.useSQL(), container, parent);

            if (nodePass == NodePassedTypeEnum.Passed
                || nodePass == NodePassedTypeEnum.NotPassed)
            {
                // SignTogether 主：完结主并取消未完成子
                if (parent.ComplexType == (short)ComplexTypeEnum.SignTogether)
                {
                    parent.ActivityState = (short)ActivityStateEnum.Completed;
                    base.IActivityInstanceManager.Update(kit.useSQL(), parent);
                    base.IActivityInstanceManager.CancelUnCompletedMultipleInstance(
                        kit.useSQL(), parent.ActivityInstanceId, runner);
                    return true;
                }

                // 节点：旁关 + 淹没加签子树
                CompleteActivityInstance(parent.ActivityInstanceId, runner, kit);
                base.LinkContext.FromActivityInstance = parent;
                base.LinkContext.FromActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
                return true;
            }

            base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.WaitingForCompletedMore;
            return false;
        }

        private void HandleSequenceWaiting(
            SQLBuilder kit,
            ActivityInstanceEntity container,
            ActivityInstanceEntity fromChild,
            ActivityResource activityResource,
            FlowCommandContext runner)
        {
            var mergeType = container.MergeType ?? (short)MergeTypeEnum.Parallel;
            if (mergeType == (short)MergeTypeEnum.Sequence)
            {
                var suspended = base.IActivityInstanceManager.GetActivityMulitipleInstanceWithState(
                        kit.useSQL(),
                        container.ActivityInstanceId,
                        container.ProcessInstanceId,
                        (short)ActivityStateEnum.Suspended)
                    .OrderBy(a => a.CompleteOrder ?? 0)
                    .ToList();

                if (suspended.Count > 0)
                {
                    var next = suspended[0];
                    next.ActivityState = (short)ActivityStateEnum.Ready;
                    base.IActivityInstanceManager.Update(kit.useSQL(), next);
                    base.FireActivityReady(kit, next, runner);

                    container.NextStepPerformers = NextStepUtility.SerializeNextStepPerformers(
                        activityResource.AppRunner.NextActivityPerformers,
                        Sail.JsonSerializer);
                    base.IActivityInstanceManager.Update(kit.useSQL(), container);
                    base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.ForwardToNextSequenceTask;
                }
                else
                {
                    // 前加签：子席已尽，补建发起人直属 Task
                    EnsureDeferredInitiatorTask(kit, container, activityResource, runner);
                    base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.WaitingForCompletedMore;
                }
            }
            else
            {
                container.NextStepPerformers = NextStepUtility.SerializeNextStepPerformers(
                    activityResource.AppRunner.NextActivityPerformers,
                    Sail.JsonSerializer);
                base.IActivityInstanceManager.Update(kit.useSQL(), container);
                base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.WaitingForCompletedMore;
            }
        }

        private void EnsureDeferredInitiatorTask(
            SQLBuilder kit,
            ActivityInstanceEntity container,
            ActivityResource activityResource,
            FlowCommandContext runner)
        {
            if (!IActivityInstanceManager.IsSignForwardContainer(container))
                return;
            if (container.SignForwardType != (short)SignForwardTypeEnum.SignForwardBefore)
                return;

            var signId = container.SignIds;
            if (string.IsNullOrEmpty(signId))
                return;

            var existing = base.ITaskManager.GetTaskListByActivity(
                    kit.useSQL(), container.ProcessInstanceId, container.ActivityInstanceId)
                .Any(t => string.Equals(t.SignId, signId, StringComparison.OrdinalIgnoreCase)
                    && (t.TaskState == (short)TaskStateEnum.Waiting
                        || t.TaskState == (short)TaskStateEnum.Reading
                        || t.TaskState == (short)TaskStateEnum.Completed));
            if (existing)
                return;

            var performer = new Performer(signId, container.SignNames);
            base.ITaskManager.Insert(kit.useSQL(), container, performer, activityResource.AppRunner);
            base.FireActivityReady(kit, container, runner);
        }
    }
}
