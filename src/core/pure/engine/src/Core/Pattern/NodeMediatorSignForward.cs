using System;
using System.Collections.Generic;
using System.Linq;
using sailflow.engine.Common;
using sailflow.data;
using mooSQL.data;
using sailflow.engine.Xpdl;
using sailflow.engine.Entity;
using sailflow.WebUtility;
using sailflow.engine.Client;
using sailflow.engine.Core;

namespace sailflow.engine.Pattern
{
    /// <summary>
    /// 加签主节点执行器：升级当前 AI 为临时子堆栈容器，并写入子堆栈 Leave（Collaborate + Merge）。
    /// </summary>
    internal class NodeMediatorSignForward : NodeMediator
    {
        /// <summary>与 <c>LeaveJudgeTypeCodes.Collaborate</c> 对齐（engine 不引用 assignee 程序集）。</summary>
        internal const string StackLeaveCollaborate = "Collaborate";

        internal NodeMediatorSignForward(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {
        }

        /// <summary>
        /// 执行工作项
        /// </summary>
        public override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            try
            {
                var controlParamSheet = base.ActivityForwardContext.ActivityResource.AppRunner.ControlParameterSheet
                    ?? throw new InvalidOperationException("SignForward requires ControlParameterSheet.");
                var signForwardType = EnumHelper.ParseEnum<SignForwardTypeEnum>(controlParamSheet.SignForwardType);

                ApplyStackLeaveOnMain(
                    base.ActivityForwardContext.FromActivityInstance,
                    signForwardType,
                    controlParamSheet);

                UpgradeToMainSignForwardNode(base.ActivityForwardContext.FromActivityInstance, signForwardType);
                CreateSignForwardTasks(ActivityForwardContext.ActivityResource, signForwardType);
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 子堆栈 Leave：前/后 = Collaborate + Sequence + Count 全员；并 = Collaborate + Parallel + Percentage 全员。
        /// 串行禁止把宿主 count/1 写到主 AI。
        /// </summary>
        private static void ApplyStackLeaveOnMain(
            ActivityInstanceEntity main,
            SignForwardTypeEnum signForwardType,
            ControlParameterSheet controlParamSheet)
        {
            main.LeaveJudgeType = StackLeaveCollaborate;
            main.LeaveJudgeParams = null;

            if (signForwardType == SignForwardTypeEnum.SignForwardBefore
                || signForwardType == SignForwardTypeEnum.SignForwardBehind)
            {
                main.MergeType = (short)MergeTypeEnum.Sequence;
                main.CompareType = (short)CompareTypeEnum.Count;
                // CompleteOrder 在建完子席后写为席位数（见 CreateSignForwardTasks）
                main.CompleteOrder = null;
            }
            else
            {
                main.MergeType = (short)MergeTypeEnum.Parallel;
                // 并加签默认全员：Percentage + 1；宿主可传 count 则按人数阈
                if (controlParamSheet != null
                    && !string.IsNullOrEmpty(controlParamSheet.SignForwardCompareType)
                    && controlParamSheet.SignForwardCompareType.ToUpper() == "COUNT")
                {
                    main.CompareType = (short)CompareTypeEnum.Count;
                    main.CompleteOrder = controlParamSheet.SignForwardCompleteOrder > 0
                        ? controlParamSheet.SignForwardCompleteOrder
                        : null;
                }
                else
                {
                    main.CompareType = (short)CompareTypeEnum.Percentage;
                    main.CompleteOrder = controlParamSheet != null && controlParamSheet.SignForwardCompleteOrder != 0
                        ? controlParamSheet.SignForwardCompleteOrder
                        : 1f;
                }
            }
        }

        /// <summary>
        /// 升级当前节点为加签主节点
        /// </summary>
        private void UpgradeToMainSignForwardNode(ActivityInstanceEntity currentActivityInstance,
            SignForwardTypeEnum signForwardType)
        {
            currentActivityInstance.ComplexType = (short)ComplexTypeEnum.SignForward;
            if (signForwardType == SignForwardTypeEnum.SignForwardBefore
                || signForwardType == SignForwardTypeEnum.SignForwardBehind)
            {
                currentActivityInstance.MergeType = (short)MergeTypeEnum.Sequence;
            }
            else
            {
                currentActivityInstance.MergeType = (short)MergeTypeEnum.Parallel;
            }
            currentActivityInstance.ActivityState = (short)ActivityStateEnum.Suspended;

            base.ActivityInstanceManager.Update(Kit.useSQL(), currentActivityInstance);
        }

        /// <summary>
        /// 创建加签节点记录；串行时回写主 AI CompleteOrder = 子席总数。
        /// </summary>
        private void CreateSignForwardTasks(ActivityResource activityResource, SignForwardTypeEnum signForwardType)
        {
            var main = base.ActivityForwardContext.FromActivityInstance;
            var plist = activityResource.NextActivityPerformers[base.ActivityForwardContext.CurrentActivity.ActivityId];

            var newActivityInstance = base.ActivityInstanceManager.CreateActivityInstanceObject(main);
            // 子 AI 不承载 Leave（判定只读主容器）
            newActivityInstance.LeaveJudgeType = null;
            newActivityInstance.LeaveJudgeParams = null;
            newActivityInstance.SignIds = activityResource.AppRunner.UserId;
            newActivityInstance.SignNames = activityResource.AppRunner.UserName;
            newActivityInstance.MainActInstanceId = main.ActivityInstanceId;
            newActivityInstance.SignForwardType = (short)signForwardType;
            newActivityInstance.ComplexType = (short)ComplexTypeEnum.SignForward;

            if (signForwardType == SignForwardTypeEnum.SignForwardBefore)
            {
                newActivityInstance.CompleteOrder = plist.Count + 1;
                newActivityInstance.ActivityState = (short)ActivityStateEnum.Suspended;
            }
            else if (signForwardType == SignForwardTypeEnum.SignForwardBehind)
            {
                newActivityInstance.CompleteOrder = 1;
            }
            else if (signForwardType == SignForwardTypeEnum.SignForwardParallel)
            {
                newActivityInstance.CompleteOrder = -1;
            }

            base.ActivityInstanceManager.Insert(Kit.useSQL(), newActivityInstance);
            var signer = new Performer(activityResource.AppRunner.UserId, activityResource.AppRunner.UserName);
            base.TaskManager.Insert(Kit.useSQL(), newActivityInstance, signer, activityResource.AppRunner);

            for (var i = 0; i < plist.Count; i++)
            {
                var signforwardActivityInstance = base.ActivityInstanceManager.CreateActivityInstanceObject(main);
                signforwardActivityInstance.LeaveJudgeType = null;
                signforwardActivityInstance.LeaveJudgeParams = null;
                signforwardActivityInstance.ComplexType = (short)ComplexTypeEnum.SignForward;
                signforwardActivityInstance.SignIds = plist[i].UserId;
                signforwardActivityInstance.SignNames = plist[i].UserName;
                signforwardActivityInstance.GroupId = plist[i].GroupId;
                signforwardActivityInstance.GroupName = plist[i].GroupName;
                signforwardActivityInstance.GroupRole = plist[i].GroupRole;
                signforwardActivityInstance.UnitId = plist[i].UnitId;
                signforwardActivityInstance.UnitCode = plist[i].UnitCode;
                signforwardActivityInstance.UnitName = plist[i].UnitName;
                signforwardActivityInstance.MainActInstanceId = main.ActivityInstanceId;
                signforwardActivityInstance.SignForwardType = (short)signForwardType;

                if (signForwardType == SignForwardTypeEnum.SignForwardBefore)
                {
                    signforwardActivityInstance.CompleteOrder = (short)(i + 1);
                    if (i > 0)
                        signforwardActivityInstance.ActivityState = (short)ActivityStateEnum.Suspended;
                }
                else if (signForwardType == SignForwardTypeEnum.SignForwardBehind)
                {
                    signforwardActivityInstance.CompleteOrder = (short)(i + 2);
                    signforwardActivityInstance.ActivityState = (short)ActivityStateEnum.Suspended;
                }
                else if (signForwardType == SignForwardTypeEnum.SignForwardParallel)
                {
                    signforwardActivityInstance.CompleteOrder = -1;
                }

                base.ActivityInstanceManager.Insert(Kit.useSQL(), signforwardActivityInstance);
                base.TaskManager.Insert(Kit.useSQL(), signforwardActivityInstance, plist[i], activityResource.AppRunner);
            }

            // 串行：主 CompleteOrder = 子席总数（本人 + 加签人），供 Collaborate Count 全员阈
            if (signForwardType == SignForwardTypeEnum.SignForwardBefore
                || signForwardType == SignForwardTypeEnum.SignForwardBehind)
            {
                main.CompleteOrder = plist.Count + 1;
                base.ActivityInstanceManager.Update(Kit.useSQL(), main);
            }
        }
    }
}
