using System;
using sailflow.data;
using mooSQL.data;
using sailflow.hub.Localize;
using sailflow.engine.Xpdl;
using sailflow.engine.Entity;
using sailflow.engine.Common;

using sailflow.engine.Client;
using sailflow.engine.Core;
namespace sailflow.engine.Pattern
{
    /// <summary>
    /// 多实例节点场景的创建器
    /// </summary>
    internal class NodeMediatorMultiSignCreator : NodeMediator
    {
        internal NodeMediatorMultiSignCreator(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        internal NodeMediatorMultiSignCreator(SQLBuilder kit, SailClient client)
            : base(kit, client)
        {
        }

        internal override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            try
            {
                ;
            }
            catch (System.Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// 创建活动任务转移实例
        /// </summary>
        internal override void CreateActivityTaskTransitionInstance(Activity toActivity,
            ProcessInstanceEntity processInstance,
            ActivityInstanceEntity fromActivityInstance,
            string transitionId,
            TransitionTypeEnum transitionType,
            TransitionFlyingTypeEnum flyingType,
            ActivityResource activityResource,
            SQLBuilder kit)
        {
            //判断是否为会签节点；若是则创建会签节点
            if (toActivity.ActivityType == ActivityTypeEnum.MultiSignNode)
            {
                if (toActivity.MultiSignDetail.ComplexType == ComplexTypeEnum.SignForward)          
                {
                    //生成加签节点（同普通任务节点）
                    var toActivityInstance = base.CreateActivityInstanceObject(toActivity, processInstance, activityResource.AppRunner);
                    toActivityInstance.ActivityState = (short)ActivityStateEnum.Ready;
                    toActivityInstance = GenerateActivityAssignedUserInfo(toActivityInstance, activityResource);

                    base.ActivityInstanceManager.Insert(kit.useSQL(), toActivityInstance);

                    base.CreateNewTask(toActivityInstance, activityResource, kit);

                    InsertTransitionInstance(processInstance,
                        transitionId,
                        fromActivityInstance,
                        toActivityInstance,
                        transitionType,
                        flyingType,
                        activityResource.AppRunner,
                        kit);
                }
                else if (toActivity.MultiSignDetail.ComplexType == ComplexTypeEnum.SignTogether)            
                {
                    //创建会签主节点并记录其下多实例子节点
                    CreateMultipleInstance(toActivity, processInstance, fromActivityInstance,
                        transitionId, transitionType, flyingType, activityResource, kit);
                }
                else
                {
                    throw new ApplicationException(Sail.Messages.GetEngineMessage("nodemediatormicreator.CreateActivityTaskTransitionInstance.warn"));
                }
            }
            else
            {
                throw new ApplicationException(Sail.Messages.GetEngineMessage("nodemediatormicreator.CreateActivityTaskTransitionInstance.exception"));
            }
        }
    }
}
