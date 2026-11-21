using System;
using System.Collections.Generic;
using System.Linq;
using sailflow.engine.Common;
using sailflow.data;
using mooSQL.data;
using sailflow.engine.Xpdl;
using sailflow.engine.Entity;
using sailflow.engine.Client;
using sailflow.engine.Core;
using sailflow.engine.Manager;
using sailflow.WebUtility;

namespace sailflow.engine.Pattern
{
    /// <summary>
    /// 加签压栈：发起人 AI 即容器（直属 Task + 加签人子 AI 同级）；不改写父/节点 Leave。
    /// </summary>
    internal class NodeMediatorSignForward : NodeMediator
    {
        internal const string StackLeaveCollaborate = "Collaborate";

        internal NodeMediatorSignForward(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {
        }

        public override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            try
            {
                var controlParamSheet = base.ActivityForwardContext.ActivityResource.AppRunner.ControlParameterSheet
                    ?? throw new InvalidOperationException("SignForward requires ControlParameterSheet.");
                var signForwardType = EnumHelper.ParseEnum<SignForwardTypeEnum>(controlParamSheet.SignForwardType);
                var resource = ActivityForwardContext.ActivityResource;
                var runner = resource.AppRunner;
                var current = base.ActivityForwardContext.FromActivityInstance
                    ?? throw new InvalidOperationException("SignForward requires FromActivityInstance.");

                var container = ResolveOrCreateContainer(current, signForwardType, controlParamSheet, resource, runner);
                CreateSigneeChildren(container, resource, signForwardType);

                if (signForwardType == SignForwardTypeEnum.SignForwardBefore
                    || signForwardType == SignForwardTypeEnum.SignForwardBehind)
                {
                    var children = base.ActivityInstanceManager.GetActivityMulitipleInstanceWithState(
                        Kit.useSQL(),
                        container.ActivityInstanceId,
                        container.ProcessInstanceId,
                        null);
                    // 席位数 = 直属发起人 Task(1) + 加签子 AI
                    container.CompleteOrder = children.Count + 1;
                    base.ActivityInstanceManager.Update(Kit.useSQL(), container);
                }
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 扁平：在节点下新建发起人容器并迁 Task；已有席位 AI：升级为本容器；已是容器：只补 Leave/人。
        /// </summary>
        private ActivityInstanceEntity ResolveOrCreateContainer(
            ActivityInstanceEntity current,
            SignForwardTypeEnum signForwardType,
            ControlParameterSheet controlParamSheet,
            ActivityResource resource,
            FlowCommandContext runner)
        {
            if (sailflow.engine.Manager.ActivityInstanceManager.IsSignForwardContainer(current))
            {
                ApplyStackLeaveOnContainer(current, signForwardType, controlParamSheet);
                base.ActivityInstanceManager.Update(Kit.useSQL(), current);
                return current;
            }

            // 席位 AI（挂在父下）：升级为容器，保留直属 Task
            if (!string.IsNullOrEmpty(current.MainActInstanceId))
            {
                ApplyStackLeaveOnContainer(current, signForwardType, controlParamSheet);
                current.ComplexType = (short)ComplexTypeEnum.SignForward;
                current.SignForwardType = (short)signForwardType;
                if (string.IsNullOrEmpty(current.SignIds))
                {
                    current.SignIds = runner.UserId;
                    current.SignNames = runner.UserName;
                }
                // 容器保持可办（发起人直属 Task）；勿 Suspended
                if (current.ActivityState == (short)ActivityStateEnum.Suspended)
                    current.ActivityState = (short)ActivityStateEnum.Ready;
                base.ActivityInstanceManager.Update(Kit.useSQL(), current);
                EnsureInitiatorDirectTask(current, resource, signForwardType, runner);
                return current;
            }

            // 扁平节点：抬席——新建容器挂到节点下，迁发起人 Task；节点 Leave 不动
            var node = current;
            var container = base.ActivityInstanceManager.CreateActivityInstanceObject(node);
            container.MainActInstanceId = node.ActivityInstanceId;
            container.SignIds = runner.UserId;
            container.SignNames = runner.UserName;
            container.ComplexType = (short)ComplexTypeEnum.SignForward;
            container.SignForwardType = (short)signForwardType;
            container.LeaveJudgeType = null;
            container.LeaveJudgeParams = null;
            ApplyStackLeaveOnContainer(container, signForwardType, controlParamSheet);
            container.ActivityState = (short)ActivityStateEnum.Ready;
            base.ActivityInstanceManager.Insert(Kit.useSQL(), container);

            RelocateInitiatorTaskToContainer(node, container, runner);
            EnsureInitiatorDirectTask(container, resource, signForwardType, runner);
            return container;
        }

        private static void ApplyStackLeaveOnContainer(
            ActivityInstanceEntity container,
            SignForwardTypeEnum signForwardType,
            ControlParameterSheet controlParamSheet)
        {
            container.LeaveJudgeType = StackLeaveCollaborate;
            container.LeaveJudgeParams = null;

            if (signForwardType == SignForwardTypeEnum.SignForwardBefore
                || signForwardType == SignForwardTypeEnum.SignForwardBehind)
            {
                container.MergeType = (short)MergeTypeEnum.Sequence;
                container.CompareType = (short)CompareTypeEnum.Count;
                container.CompleteOrder = null;
            }
            else
            {
                container.MergeType = (short)MergeTypeEnum.Parallel;
                if (controlParamSheet != null
                    && !string.IsNullOrEmpty(controlParamSheet.SignForwardCompareType)
                    && controlParamSheet.SignForwardCompareType.ToUpper() == "COUNT")
                {
                    container.CompareType = (short)CompareTypeEnum.Count;
                    container.CompleteOrder = controlParamSheet.SignForwardCompleteOrder > 0
                        ? controlParamSheet.SignForwardCompleteOrder
                        : null;
                }
                else
                {
                    container.CompareType = (short)CompareTypeEnum.Percentage;
                    container.CompleteOrder = controlParamSheet != null && controlParamSheet.SignForwardCompleteOrder != 0
                        ? controlParamSheet.SignForwardCompleteOrder
                        : 1f;
                }
            }
        }

        private void RelocateInitiatorTaskToContainer(
            ActivityInstanceEntity node,
            ActivityInstanceEntity container,
            FlowCommandContext runner)
        {
            var taskId = ActivityForwardContext.TaskId;
            TaskEntity task = null;
            if (!string.IsNullOrEmpty(taskId))
                task = base.TaskManager.GetTask(Kit.useSQL(), taskId);

            if (task == null)
            {
                var list = base.TaskManager.GetTaskListByActivity(
                    Kit.useSQL(), node.ProcessInstanceId, node.ActivityInstanceId);
                task = list.FirstOrDefault(t =>
                    string.Equals(t.SignId, runner.UserId, StringComparison.OrdinalIgnoreCase)
                    && (t.TaskState == (short)TaskStateEnum.Waiting
                        || t.TaskState == (short)TaskStateEnum.Reading));
            }

            if (task == null)
                return;

            task.ActivityInstanceId = container.ActivityInstanceId;
            task.ActivityId = container.ActivityId;
            task.ActivityName = container.ActivityName;
            base.TaskManager.Update(Kit.useSQL(), task);
        }

        private void EnsureInitiatorDirectTask(
            ActivityInstanceEntity container,
            ActivityResource resource,
            SignForwardTypeEnum signForwardType,
            FlowCommandContext runner)
        {
            var existing = base.TaskManager.GetTaskListByActivity(
                    Kit.useSQL(), container.ProcessInstanceId, container.ActivityInstanceId)
                .Where(t => string.Equals(t.SignId, runner.UserId, StringComparison.OrdinalIgnoreCase)
                    && (t.TaskState == (short)TaskStateEnum.Waiting
                        || t.TaskState == (short)TaskStateEnum.Reading
                        || t.TaskState == (short)TaskStateEnum.Completed))
                .ToList();

            if (existing.Count > 0)
                return;

            // 前加签：发起人直属 Task 后置创建（等 Sequence 唤醒）
            if (signForwardType == SignForwardTypeEnum.SignForwardBefore)
                return;

            var signer = new Performer(runner.UserId, runner.UserName);
            base.TaskManager.Insert(Kit.useSQL(), container, signer, runner);
        }

        private void CreateSigneeChildren(
            ActivityInstanceEntity container,
            ActivityResource resource,
            SignForwardTypeEnum signForwardType)
        {
            var activityId = base.ActivityForwardContext.CurrentActivity.ActivityId;
            if (!resource.NextActivityPerformers.TryGetValue(activityId, out var plist) || plist == null)
                plist = new PerformerList();

            for (var i = 0; i < plist.Count; i++)
            {
                var child = base.ActivityInstanceManager.CreateActivityInstanceObject(container);
                child.LeaveJudgeType = null;
                child.LeaveJudgeParams = null;
                child.ComplexType = null;
                child.SignIds = plist[i].UserId;
                child.SignNames = plist[i].UserName;
                child.GroupId = plist[i].GroupId;
                child.GroupName = plist[i].GroupName;
                child.GroupRole = plist[i].GroupRole;
                child.UnitId = plist[i].UnitId;
                child.UnitCode = plist[i].UnitCode;
                child.UnitName = plist[i].UnitName;
                child.MainActInstanceId = container.ActivityInstanceId;
                child.SignForwardType = (short)signForwardType;

                if (signForwardType == SignForwardTypeEnum.SignForwardBefore)
                {
                    child.CompleteOrder = (short)(i + 1);
                    if (i > 0)
                        child.ActivityState = (short)ActivityStateEnum.Suspended;
                }
                else if (signForwardType == SignForwardTypeEnum.SignForwardBehind)
                {
                    child.CompleteOrder = (short)(i + 2);
                    child.ActivityState = (short)ActivityStateEnum.Suspended;
                }
                else
                {
                    child.CompleteOrder = -1;
                }

                base.ActivityInstanceManager.Insert(Kit.useSQL(), child);
                base.TaskManager.Insert(Kit.useSQL(), child, plist[i], resource.AppRunner);
            }
        }
    }
}
