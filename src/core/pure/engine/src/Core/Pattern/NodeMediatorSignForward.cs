using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Xpdl;
using saiflow.engine.Entity;
using saiflow.WebUtility;

using saiflow.engine.Client;
using saiflow.engine.Core;
namespace saiflow.engine.Pattern
{
    /// <summary>
    /// 加签主节点执行器
    /// </summary>
    internal class NodeMediatorSignForward : NodeMediator
    {
        internal NodeMediatorSignForward(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        /// <summary>
        /// 执行工作项
        /// </summary>
        internal override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            try
            {
                //读取加签控制参数
                //记录加签通过类型和通过率
                //读取加签控制参数
                //记录加签审批类型与通过率
                var controlParamSheet = base.ActivityForwardContext.ActivityResource.AppRunner.ControlParameterSheet;
                if (controlParamSheet != null
                    && controlParamSheet.SignForwardCompareType.ToUpper() == "COUNT")
                {
                    base.ActivityForwardContext.FromActivityInstance.CompareType = (short)CompareTypeEnum.Count;
                    base.ActivityForwardContext.FromActivityInstance.CompleteOrder = controlParamSheet.SignForwardCompleteOrder;
                }
                else
                {
                    base.ActivityForwardContext.FromActivityInstance.CompareType = (short)CompareTypeEnum.Percentage;
                    if (controlParamSheet.SignForwardCompleteOrder != 0)
                        base.ActivityForwardContext.FromActivityInstance.CompleteOrder = controlParamSheet.SignForwardCompleteOrder;
                    else
                        base.ActivityForwardContext.FromActivityInstance.CompleteOrder = 1;
                }

                //更新当前实例节点为主节点，并且置当前节点为挂起状态
                //将当前实例升级为主节点并挂起
                var signForwardType = EnumHelper.ParseEnum<SignForwardTypeEnum>(controlParamSheet.SignForwardType);
                UpgradeToMainSignForwardNode(base.ActivityForwardContext.FromActivityInstance, signForwardType);

                //产生加签记录，即要发送给加签人的活动实例记录
                //生成加签记录（发给加签人的活动实例）
                CreateSignForwardTasks(ActivityForwardContext.ActivityResource);
            }
            catch (System.Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// 升级当前节点为加签主节点
        /// </summary>
        /// <param name="currentActivityInstance"></param>
        /// <param name="signForwardType"></param>
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
        /// 创建加签节点记录
        /// </summary>
        /// <param name="activityResource"></param>
        private void CreateSignForwardTasks(ActivityResource activityResource)
        {
            SignForwardTypeEnum signForwardType = (SignForwardTypeEnum)Enum.Parse(typeof(SignForwardTypeEnum), 
                base.ActivityForwardContext.ActivityResource.AppRunner.ControlParameterSheet.SignForwardType);

            //基于当前活动实例新建加签人记录，并相应修改 CompleteOrder
            var newActivityInstance = base.ActivityInstanceManager.CreateActivityInstanceObject(base.ActivityForwardContext.FromActivityInstance);
            newActivityInstance.AssignedUserIds = activityResource.AppRunner.UserId;
            newActivityInstance.AssignedUserNames = activityResource.AppRunner.UserName;
            newActivityInstance.MainActivityInstanceId = base.ActivityForwardContext.FromActivityInstance.Id;
            newActivityInstance.SignForwardType = (short)signForwardType;

            if (signForwardType == SignForwardTypeEnum.SignForwardParallel)
            {
                var controlParamSheet = base.ActivityForwardContext.ActivityResource.AppRunner.ControlParameterSheet;
                if (controlParamSheet.SignForwardCompareType.ToUpper() == "COUNT")
                {
                    newActivityInstance.CompleteOrder = controlParamSheet.SignForwardCompleteOrder;
                }
                else
                {
                    newActivityInstance.CompleteOrder = 1;
                }
            }
            newActivityInstance.ComplexType = (short)ComplexTypeEnum.SignForward;

            //获取加签人集合
            var plist = activityResource.NextActivityPerformers[base.ActivityForwardContext.CurrentActivity.ActivityId];

            //前加签是别人先审核，然后自己再审核
            //前加签：他人先审，再本人审
            if (signForwardType == SignForwardTypeEnum.SignForwardBefore)
            {
                newActivityInstance.CompleteOrder = plist.Count + 1;
                newActivityInstance.ActivityState = (short)ActivityStateEnum.Suspended;
            }
            else if (signForwardType == SignForwardTypeEnum.SignForwardBehind)
            {
                //后加签是自己审批后，其他接收人再加签
                //后加签：本人先审，再由其他人加签
                newActivityInstance.CompleteOrder = 1;
            }
			else if (signForwardType == SignForwardTypeEnum.SignForwardParallel)
            {
                //并行加签子节点都为-1，与并行会签保持一致
                newActivityInstance.CompleteOrder = -1;
            }

            //主节点挂起后，插入当前人的加签记录信息, 并插入任务记录
            //主节点挂起后，插入当前人加签记录与任务记录
            base.ActivityInstanceManager.Insert(Kit.useSQL(), newActivityInstance);
            var signer = new Performer(base.ActivityForwardContext.ActivityResource.AppRunner.UserId,
                base.ActivityForwardContext.ActivityResource.AppRunner.UserName);
            base.TaskManager.Insert(Kit.useSQL(), newActivityInstance, signer, base.ActivityForwardContext.ActivityResource.AppRunner);

            //创建新加签节点记录
            //创建新的加签节点记录
            var signforwardActivityInstance = new ActivityInstanceEntity();
            for (var i = 0; i < plist.Count; i++)
            {
                signforwardActivityInstance = base.ActivityInstanceManager.CreateActivityInstanceObject(base.ActivityForwardContext.FromActivityInstance);
                signforwardActivityInstance.ComplexType = (short)ComplexTypeEnum.SignForward;
                signforwardActivityInstance.AssignedUserIds = plist[i].UserId;
                signforwardActivityInstance.AssignedUserNames = plist[i].UserName;
                signforwardActivityInstance.GroupId = plist[i].GroupId;
                signforwardActivityInstance.GroupName = plist[i].GroupName;
                signforwardActivityInstance.GroupRole = plist[i].GroupRole;
                signforwardActivityInstance.UnitId = plist[i].UnitId;
                signforwardActivityInstance.UnitCode = plist[i].UnitCode;
                signforwardActivityInstance.UnitName = plist[i].UnitName;
                signforwardActivityInstance.MainActivityInstanceId = base.ActivityForwardContext.FromActivityInstance.Id;
                
                if (signForwardType == SignForwardTypeEnum.SignForwardBefore)
                {
                    signforwardActivityInstance.CompleteOrder = (short)(i + 1);
                    if (i > 0)
                    {
                        //加签是串行加签，逐次完成
                        //加签为逐步完成的串行过程
                        signforwardActivityInstance.ActivityState = (short)ActivityStateEnum.Suspended;
                    }
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
                
                signforwardActivityInstance.SignForwardType = (short)signForwardType;

                base.ActivityInstanceManager.Insert(Kit.useSQL(), signforwardActivityInstance);
                base.TaskManager.Insert(Kit.useSQL(), signforwardActivityInstance, plist[i], activityResource.AppRunner);
            }
        }
    }
}
