using System;
using saiflow.data;
using mooSQL.data;
using saiflow.hub.Localize;
using saiflow.engine.Xpdl.Common;
using saiflow.engine.Xpdl.Entity;
using saiflow.engine.Business.Entity;
using saiflow.engine.Common;

using saiflow.engine.Client;
namespace saiflow.engine.Core.Pattern
{
    /// <summary>
    /// Creator for multi instance node scenes
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
        /// Create activity task transition instance
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
            //判断是否是会签节点，如果是创建会签节点
            //Determine whether it is a countersignature node, and if it is a creation countersignature node
            if (toActivity.ActivityType == ActivityTypeEnum.MultiSignNode)
            {
                if (toActivity.MultiSignDetail.ComplexType == ComplexTypeEnum.SignForward)          
                {
                    //加签节点生成，跟普通任务节点生成一样
                    //Generate signature nodes, just like generating regular task nodes
                    var toActivityInstance = base.CreateActivityInstanceObject(toActivity, processInstance, activityResource.AppRunner);
                    toActivityInstance.ActivityState = (short)ActivityStateEnum.Ready;
                    toActivityInstance = GenerateActivityAssignedUserInfo(toActivityInstance, activityResource);

                    base.ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), toActivityInstance);

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
                    //创建会签节点的主节点，以及会签主节点下的实例子节点记录
                    //Create the master node for the countersignature node and record the instance child nodes under the countersignature master node
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
