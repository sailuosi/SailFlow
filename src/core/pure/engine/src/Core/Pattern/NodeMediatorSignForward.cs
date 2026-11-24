using System;
using System.Collections.Generic;
using System.Linq;
using sailflow.utils;
using sailflow.data;
using mooSQL.data;
using sailflow.xml;
using sailflow.hosting;
using sailflow.run;

namespace sailflow.run{
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
                    var children = base.IActivityInstanceManager.GetActivityMulitipleInstanceWithState(
                        Kit.useSQL(),
                        container.ActivityInstanceId,
                        container.ProcessInstanceId,
                        null);
                    // 席位数 = 直属发起人 Task(1) + 加签子 AI
                    container.CompleteOrder = children.Count + 1;
                    base.IActivityInstanceManager.Update(Kit.useSQL(), container);
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
            if (IActivityInstanceManager.IsSignForwardContainer(current))
            {
                ApplyStackLeaveOnContainer(current, signForwardType, controlParamSheet);
                base.IActivityInstanceManager.Update(Kit.useSQL(), current);
                // 前加签再加人：发起人待办仍后置
                CloseInitiatorOpenTasksIfBefore(current, signForwardType, runner);
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
                base.IActivityInstanceManager.Update(Kit.useSQL(), current);
                EnsureInitiatorDirectTask(current, resource, signForwardType, runner);
                CloseInitiatorOpenTasksIfBefore(current, signForwardType, runner);
                return current;
            }

            // 扁平节点：抬席——新建容器挂到节点下，迁发起人 Task；节点 Leave 不动
            var node = current;
            var container = base.IActivityInstanceManager.CreateActivityInstanceObject(node);
            container.MainActInstanceId = node.ActivityInstanceId;
            container.SignIds = runner.UserId;
            container.SignNames = runner.UserName;
            container.ComplexType = (short)ComplexTypeEnum.SignForward;
            container.SignForwardType = (short)signForwardType;
            container.LeaveJudgeType = null;
            container.LeaveJudgeParams = null;
            ApplyStackLeaveOnContainer(container, signForwardType, controlParamSheet);
            container.ActivityState = (short)ActivityStateEnum.Ready;
            base.IActivityInstanceManager.Insert(Kit.useSQL(), container);

            RelocateInitiatorTaskToContainer(node, container, runner);
            EnsureInitiatorDirectTask(container, resource, signForwardType, runner);
            // 前加签：迁过来的待办须关闭，等 Sequence 唤醒再建（EnsureDeferredInitiatorTask）
            CloseInitiatorOpenTasksIfBefore(container, signForwardType, runner);
            return container;
        }

        /// <summary>
        /// 前加签期间发起人不应有打开待办；Closed + SignedForwardOut，唤醒时再建。
        /// </summary>
        private void CloseInitiatorOpenTasksIfBefore(
            ActivityInstanceEntity container,
            SignForwardTypeEnum signForwardType,
            FlowCommandContext runner)
        {
            if (signForwardType != SignForwardTypeEnum.SignForwardBefore)
                return;

            var open = base.ITaskManager.GetTaskListByActivity(
                    Kit.useSQL(), container.ProcessInstanceId, container.ActivityInstanceId)
                .Where(t => string.Equals(t.SignId, runner.UserId, StringComparison.OrdinalIgnoreCase)
                    && (t.TaskState == (short)TaskStateEnum.Waiting
                        || t.TaskState == (short)TaskStateEnum.Reading))
                .ToList();

            foreach (var t in open)
            {
                ITaskManager.ApplyTaskEnd(t, TaskStateEnum.Closed, runner, TaskEndTypes.SignedForwardOut);
                base.ITaskManager.Update(Kit.useSQL(), t);
            }
        }

        private static void ApplyStackLeaveOnContainer(
            ActivityInstanceEntity container,
            SignForwardTypeEnum signForwardType,
            ControlParameterSheet controlParamSheet)
        {
            _ = controlParamSheet; // 并加签不再读宿主 Compare/Order；前/后席位数由压栈后回写
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
                // 并加签：强制 Parallel + Percentage 全员（忽略宿主 COUNT/1，防一人即出）
                container.MergeType = (short)MergeTypeEnum.Parallel;
                container.CompareType = (short)CompareTypeEnum.Percentage;
                container.CompleteOrder = 1f;
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
                task = base.ITaskManager.GetTask(Kit.useSQL(), taskId);

            if (task == null)
            {
                var list = base.ITaskManager.GetTaskListByActivity(
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
            base.ITaskManager.Update(Kit.useSQL(), task);
        }

        private void EnsureInitiatorDirectTask(
            ActivityInstanceEntity container,
            ActivityResource resource,
            SignForwardTypeEnum signForwardType,
            FlowCommandContext runner)
        {
            var existing = base.ITaskManager.GetTaskListByActivity(
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
            base.ITaskManager.Insert(Kit.useSQL(), container, signer, runner);
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
                var child = base.IActivityInstanceManager.CreateActivityInstanceObject(container);
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

                base.IActivityInstanceManager.Insert(Kit.useSQL(), child);
                base.ITaskManager.Insert(Kit.useSQL(), child, plist[i], resource.AppRunner);
            }
        }
    }
}
