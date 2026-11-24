using System;
using System.Linq;
using sailflow.data;
using mooSQL.data;
using sailflow.utils;
using sailflow.events;
using sailflow.xml;
using sailflow.assignee;
using sailflow.engine.Client;
using sailflow.run;
using sailflow.hooks;
using sailflow.engine.Result;

namespace sailflow.run{
    /// <summary>
    /// 任务节点执行器（扁平席位；含外层 Leave + 加签子树淹没）。
    /// </summary>
    internal class NodeMediatorTask : NodeMediator
    {
        internal NodeMediatorTask(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {
        }

        internal NodeMediatorTask(SQLBuilder kit, SailClient client)
            : base(kit, client)
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

            var from = base.LinkContext.FromActivityInstance;
            if (from == null)
                return false;

            // 多席 / 有 Leave / 有直属加签子：走 TryCanLeave，勿盲 Complete
            var children = base.IActivityInstanceManager.GetValidActivityInstanceListOfMI(
                kit.useSQL(), from.ActivityInstanceId, from.ProcessInstanceId);
            var directTasks = base.ITaskManager.GetTaskListByActivity(
                kit.useSQL(), from.ProcessInstanceId, from.ActivityInstanceId);
            var approverTasks = directTasks
                .Where(t => t != null && !AssigneeRoleHelper.IsParticipant(t.AssigneeRole))
                .ToList();
            var needsLeave = !string.IsNullOrEmpty(from.LeaveJudgeType)
                || children.Count > 0
                || approverTasks.Count > 1;

            if (needsLeave)
            {
                base.IActivityInstanceManager.SetApprovalStatus(kit.useSQL(), from.ActivityInstanceId);
                var passed = base.IActivityInstanceManager.EvaluateLeaveOnActivity(
                    kit.useSQL(), from, from);

                if (passed == NodePassedTypeEnum.NeedToBeMoreApproved
                    || passed == NodePassedTypeEnum.Default)
                {
                    base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.WaitingForCompletedMore;
                    return false;
                }

                if (passed == NodePassedTypeEnum.NotPassed)
                {
                    CompleteActivityInstance(from.ActivityInstanceId, runner, kit);
                    from.ActivityState = (short)ActivityStateEnum.Completed;
                    return from.CanNotRenewInstance == 0;
                }

                // Passed
                CompleteActivityInstance(from.ActivityInstanceId, runner, kit);
                from.ActivityState = (short)ActivityStateEnum.Completed;
                return from.CanNotRenewInstance == 0;
            }

            CompleteActivityInstance(from.ActivityInstanceId, runner, kit);
            from.ActivityState = (short)ActivityStateEnum.Completed;
            return from.CanNotRenewInstance == 0;
        }

        public override void CreateActivityTaskTransitionInstance(Activity toActivity,
            ProcessInstanceEntity processInstance,
            ActivityInstanceEntity fromActivityInstance,
            string transitionId,
            TransitionTypeEnum transitionType,
            TransitionFlyingTypeEnum flyingType,
            ActivityResource activityResource,
            SQLBuilder kit)
        {
            Boolean isParallel = false;
            if (fromActivityInstance.ActivityType == (short)ActivityTypeEnum.GatewayNode)
            {
                var processModel = Sail.ProcessModelFactory.CreateByProcessInstance(kit.useSQL(), processInstance);
                var activityNode = processModel.GetActivity(fromActivityInstance.ActivityId);
                isParallel = processModel.IsAndSplitMI(activityNode);
            }

            if (isParallel)
            {
                ActivityInstanceEntity entity = null;
                var plist = activityResource.NextActivityPerformers[toActivity.ActivityId];

                for (var i = 0; i < plist.Count; i++)
                {
                    entity = base.CreateActivityInstanceObject(toActivity, processInstance, activityResource.AppRunner);
                    entity.SignIds = plist[i].UserId;
                    entity.SignNames = plist[i].UserName;
                    entity.GroupId = plist[i].GroupId;
                    entity.GroupName = plist[i].GroupName;
                    entity.GroupRole = plist[i].GroupRole;
                    entity.UnitId = plist[i].UnitId;
                    entity.UnitCode = plist[i].UnitCode;
                    entity.UnitName = plist[i].UnitName;
                    entity.ActivityState = (short)ActivityStateEnum.Ready;

                    entity.ActivityInstanceId = base.IActivityInstanceManager.Insert(kit.useSQL(), entity);

                    base.ITaskManager.Insert(kit.useSQL(), entity, plist[i], activityResource.AppRunner);

                    InsertTransitionInstance(processInstance,
                        transitionId,
                        fromActivityInstance,
                        entity,
                        transitionType,
                        flyingType,
                        activityResource.AppRunner,
                        kit);
                }
            }
            else
            {
                var toActivityInstance = base.CreateActivityInstanceObject(toActivity, processInstance, activityResource.AppRunner);

                WriteBackSrcOrgInformation(toActivityInstance, fromActivityInstance, kit);

                toActivityInstance.ActivityState = (short)ActivityStateEnum.Ready;
                toActivityInstance = GenerateActivitySignInfo(toActivityInstance, activityResource);

                base.IActivityInstanceManager.Insert(kit.useSQL(), toActivityInstance);

                base.CreateNewTask(toActivityInstance, activityResource, kit);
                InsertTransitionInstance(processInstance,
                    transitionId,
                    fromActivityInstance,
                    toActivityInstance,
                    transitionType,
                    flyingType,
                    activityResource.AppRunner,
                    kit);

                var createdRunner = activityResource?.AppRunner;
                if (createdRunner != null && processInstance?.ProcessInstanceId != null)
                {
                    var hookCtx = FlowCapabilityPipeline.BuildHookContext(
                        Sail,
                        kit,
                        createdRunner,
                        processInstance.ProcessInstanceId,
                        toActivityInstance,
                        null,
                        EventFireTypeEnum.OnActivityCreated,
                        FlowOperation.Run);
                    FlowCapabilityPipeline.AfterSuccess(
                        Sail, kit, hookCtx, EventFireTypeEnum.OnActivityCreated);
                }
            }
        }

        private void WriteBackSrcOrgInformation(ActivityInstanceEntity toActivityInstance,
            ActivityInstanceEntity fromActivityInstance,
            SQLBuilder kit)
        {
            if (fromActivityInstance.BackSrcActInstanceId != null)
            {
                var backSrcActivityInstance = base.IActivityInstanceManager.GetById(kit.useSQL(), fromActivityInstance.BackSrcActInstanceId);
                toActivityInstance.BackSrcActInstanceId = backSrcActivityInstance.BackSrcActInstanceId;
                toActivityInstance.BackOrgActInstanceId = backSrcActivityInstance.BackOrgActInstanceId;
            }
        }
    }
}
