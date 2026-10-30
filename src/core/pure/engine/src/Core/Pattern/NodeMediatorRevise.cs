using System;
using System.Collections.Generic;
using saiflow.data;
using mooSQL.data;
using saiflow.hub.Localize;
using saiflow.engine.Common;
using saiflow.engine.Xpdl.Common;
using saiflow.engine.Xpdl.Entity;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;


using saiflow.engine.Client;
namespace saiflow.engine.Core.Pattern
{
    /// <summary>
    /// Node Mediator Revise
    /// 流程修订的具体执行类
    /// </summary>
    internal class NodeMediatorRevise : NodeMediator
    {
        internal NodeMediatorRevise(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        internal NodeMediatorRevise(SQLBuilder kit, SailClient client)
            : base(kit, client)
        {

        }

        /// <summary>
        /// Execute work item
        /// </summary>
        internal override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            try
            {
                OnBeforeExecuteWorkItem();

                //先取出原始退回节点信息
                //First, retrieve the original return node information
                var backSrcActivityInstanceId = base.ActivityForwardContext.FromActivityInstance.BackSrcActivityInstanceId;
                if (backSrcActivityInstanceId != null)
                {
                    //完成当前的任务节点
                    //Complete the current task node
                    bool canContinueForwardCurrentNode = CompleteWorkItem(ActivityForwardContext.TaskId,
                        ActivityForwardContext.ActivityResource,
                        this.Kit);

                    if (canContinueForwardCurrentNode == true)
                    {
                        var aim = new ActivityInstanceManager(Sail);
                        var backSrcActivityInstance = aim.GetById(backSrcActivityInstanceId.Value);

                        //取出下一步办理人员信息
                        //Retrieve the information of the next processing personnel
                        var nextStep = base.ActivityForwardContext.ActivityResource.AppRunner.NextActivityPerformers;
                        var performerList = nextStep[backSrcActivityInstance.ActivityId];

                        //判断不同的修订模式
                        //Determine different revision modes
                        var mainActivityInstanceId = backSrcActivityInstance.MainActivityInstanceId;
                        if (mainActivityInstanceId != null)
                        {
                            //会签模式
                            //复制与会签子节点相同的活动实例和任务记录 
                            //Signing mode
                            //Copy activity instances and task records with the same attendance child nodes
                            var mainActivityInstance = aim.GetById(mainActivityInstanceId.Value);
                            CloneChildNodeOfMI(performerList, mainActivityInstance, base.Kit);
                        }
                        else
                        {
                            //并行分支（多实例）的情况
                            ////In the case of parallel branches (multiple instances)
                            var transitionList = ActivityForwardContext.ProcessModel.GetBackwardTransitionList(backSrcActivityInstance.ActivityId);
                            if (transitionList != null && transitionList.Count == 1)
                            {
                                var transition = transitionList[0];
                                var gatewayNode = ActivityForwardContext.ProcessModel.GetActivity(transition.FromActivityId);
                                if (gatewayNode.GatewayDetail.DirectionType == GatewayDirectionEnum.AndSplitMI)
                                {
                                    //复制并行分支多实例
                                    //Copy parallel branches with multiple instances
                                    var gatewayActivityInstance = base.ActivityInstanceManager.GetActivityInstanceLatest(SailSqlAccess.Sibling(Kit), 
                                        backSrcActivityInstance.ProcessInstanceId, gatewayNode.ActivityId);
                                    CloneChildNodeOfAndSplitMI(performerList, transition.TransitionId, gatewayActivityInstance, backSrcActivityInstance, base.Kit);
                                }
                                else
                                {
                                    throw new WorkflowException(LocalizeHelper.GetEngineMessage("nodemediatorrevise.ExecuteWorkItem.warn"));
                                }
                            }
                            else
                            {
                                throw new WorkflowException(LocalizeHelper.GetEngineMessage("nodemediatorrevise.ExecuteWorkItem.warn"));
                            }
                        }
                    }
                    else
                    {
                        throw new WorkflowException(LocalizeHelper.GetEngineMessage("nodemediatorrevise.ExecuteWorkItem.exception"));
                    } 
                }
                else
                {
                    throw new WorkflowException(LocalizeHelper.GetEngineMessage("nodemediatorrevise.ExecuteWorkItem.error"));
                }
                OnAfterExecuteWorkItem();
            }
            catch (System.Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Complete work item
        /// </summary>
        internal bool CompleteWorkItem(int? taskId,
            ActivityResource activityResource,
            SQLBuilder kit)
        {
            bool canContinueForwardCurrentNode = true;

            WfAppRunner runner = new WfAppRunner
            {
                UserId = activityResource.AppRunner.UserId,        
                UserName = activityResource.AppRunner.UserName
            };

            if (taskId != null)
            {
                //完成本任务，返回任务已经转移到下一个会签任务，不继续执行其它节点
                //Complete this task, return that the task has been transferred to the next co signing task,
                //and do not continue to execute other nodes
                base.TaskManager.Complete(SailSqlAccess.Sibling(kit), taskId.Value, activityResource.AppRunner);

                //设置活动节点的状态为完成状态
                //Set the status of the activity node to complete status
                var activityInstanceId = base.ActivityInstanceManager.GetByTask(taskId.Value).Id;
                base.ActivityInstanceManager.Complete(SailSqlAccess.Sibling(kit), activityInstanceId, activityResource.AppRunner);
            }
            return canContinueForwardCurrentNode;
        }

        /// <summary>
        /// Copy multi instance node data
        /// 复制多实例子节点数据
        /// </summary>
        /// <param name="plist"></param>
        /// <param name="mainActivityInstance"></param>
        /// <param name="kit"></param>
        private void CloneChildNodeOfMI(PerformerList plist,
            ActivityInstanceEntity mainActivityInstance, 
            SQLBuilder kit)
        {
            var childNodeListOfMI = base.ActivityInstanceManager.GetValidActivityInstanceListOfMI(SailSqlAccess.Sibling(kit), mainActivityInstance.Id, mainActivityInstance.ProcessInstanceId);

            ActivityInstanceEntity entity = new ActivityInstanceEntity();
            for (short i = 0; i < plist.Count; i++)
            {
                var userId = plist[i].UserId;
                var userName = plist[i].UserName;
                var isTaskExisted = IsTaskExisted(childNodeListOfMI, userId);
                if (isTaskExisted == true)
                {
                    //如果活动或者任务已经存在，则不用创建新活动和任务
                    //If an activity or task already exists, there is no need to create a new one
                    continue;
                }

                //根据主节点来复制子节点数据
                //Copy child node data based on the master node
                entity = ActivityInstanceManager.CreateActivityInstanceObject(mainActivityInstance);
                entity.AssignedUserIds = userId;
                entity.AssignedUserNames = userName;
                entity.MainActivityInstanceId = mainActivityInstance.Id;

                //并行串行下，多实例子节点的执行顺序设置
                //Setting the Execution Order of Multiple Real Example Nodes in Parallel Serial
                if (mainActivityInstance.MergeType == (short)MergeTypeEnum.Sequence)
                {
                    entity.CompleteOrder = (short)(i + 1);
                }
                else if (mainActivityInstance.MergeType == (short)MergeTypeEnum.Parallel)
                {
                    //并行模式下CompleteOrder的优先级一样，所以置为 -1
                    //In parallel mode, the priority of Completed Order is the same, so it is set to -1
                    entity.CompleteOrder = -1;       
                }

                //如果是串行会签，只有第一个节点处于运行状态，其它节点挂起
                //If it is a serial countersignature, only the first node is running, and the other nodes are suspended
                if ((i > 0) && (mainActivityInstance.MergeType.Value == (short)MergeTypeEnum.Sequence))
                {
                    entity.ActivityState = (short)ActivityStateEnum.Suspended;
                }

                entity.Id = ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), entity);

                base.TaskManager.Insert(SailSqlAccess.Sibling(kit), entity, plist[i], base.ActivityForwardContext.ActivityResource.AppRunner);
            }
        }

        /// <summary>
        /// Determine whether the activity instance already exists
        /// 判断活动实例是否已经存在
        /// </summary>
        /// <param name="childNodeOfMI"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private Boolean IsTaskExisted(List<ActivityInstanceEntity> childNodeOfMI, 
            string userId)
        {
            var isExisted = false;
            var child = childNodeOfMI.Find(a => a.AssignedUserIds.Contains(userId));

            if (child != null)
                isExisted = true;
           
            return isExisted;
        }

        /// <summary>
        /// Copy parallel branch multi instance node data
        /// 复制并行分支多实例节点数据
        /// </summary>
        private void CloneChildNodeOfAndSplitMI(PerformerList plist,
            string transitionId,
            ActivityInstanceEntity gatewayActivityInstance,
            ActivityInstanceEntity toActivityInstance, 
            SQLBuilder kit)
        {
            var childNodeListOfAndSplitMI = base.ActivityInstanceManager.GetValidSplitedActivityInstanceList(SailSqlAccess.Sibling(kit), gatewayActivityInstance.ProcessInstanceId, gatewayActivityInstance.Id);

            ActivityInstanceEntity newChildInstance = new ActivityInstanceEntity();
            for (short i = 0; i < plist.Count; i++)
            {
                var userId = plist[i].UserId;
                var userName = plist[i].UserName;
                var isTaskExisted = IsTaskExisted(childNodeListOfAndSplitMI, userId);
                if (isTaskExisted == true)
                {
                    //如果活动或者任务已经存在，则不用创建新活动和任务
                    //If an activity or task already exists, there is no need to create a new one
                    continue;
                }

                newChildInstance = ActivityInstanceManager.CreateActivityInstanceObject(toActivityInstance);
                newChildInstance.AssignedUserIds = userId;
                newChildInstance.AssignedUserNames = userName;

                newChildInstance.Id = ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), newChildInstance);

                base.InsertTransitionInstance(base.ActivityForwardContext.ProcessInstance,
                    transitionId,
                    gatewayActivityInstance,
                    newChildInstance,
                    TransitionTypeEnum.Forward,
                    TransitionFlyingTypeEnum.NotFlying,
                    base.ActivityForwardContext.ActivityResource.AppRunner,
                    kit);

                base.TaskManager.Insert(SailSqlAccess.Sibling(kit), newChildInstance, plist[i], base.ActivityForwardContext.ActivityResource.AppRunner);
            }
        }
    }
}
