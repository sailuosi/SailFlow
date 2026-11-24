using System;
using System.Collections.Generic;
using mooSQL.data;

using sailflow.utils;
using sailflow.xml;
using sailflow.data;
using sailflow.engine.Client;
using sailflow.run;
namespace sailflow.run{
    /// <summary>
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
        /// 执行工作项
        /// </summary>
        public override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            try
            {
                OnBeforeExecuteWorkItem();

                //先获取原始退回节点信息
                var backSrcActivityInstanceId = base.ActivityForwardContext.FromActivityInstance.BackSrcActInstanceId;
                if (backSrcActivityInstanceId != null)
                {
                    //完成当前的任务节点
                    //完成当前任务节点
                    bool canContinueForwardCurrentNode = CompleteWorkItem(ActivityForwardContext.TaskId,
                        ActivityForwardContext.ActivityResource,
                        this.Kit);

                    if (canContinueForwardCurrentNode == true)
                    {
                        var aim = Sail.ManagerFactory.Activities;
                        var backSrcActivityInstance = aim.GetById(backSrcActivityInstanceId);

                        //获取下一步办理人信息
                        var nextStep = base.ActivityForwardContext.ActivityResource.AppRunner.NextActivityPerformers;
                        var performerList = nextStep[backSrcActivityInstance.ActivityId];

                        //判定不同修订模式
                        var mainActivityInstanceId = backSrcActivityInstance.MainActInstanceId;
                        if (mainActivityInstanceId != null)
                        {
                            //会签模式
                            //复制与会签子节点相同的活动实例和任务记录 
                            //加签模式：复制同出席子节点的活动实例与任务记录
                            var mainActivityInstance = aim.GetById(mainActivityInstanceId);
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
                                    //复制并行多实例分支
                                    var gatewayActivityInstance = base.IActivityInstanceManager.GetActivityInstanceLatest(Kit.useSQL(), 
                                        backSrcActivityInstance.ProcessInstanceId, gatewayNode.ActivityId);
                                    CloneChildNodeOfAndSplitMI(performerList, transition.TransitionId, gatewayActivityInstance, backSrcActivityInstance, base.Kit);
                                }
                                else
                                {
                                    throw new WorkflowException(Sail.Messages.GetEngineMessage("nodemediatorrevise.ExecuteWorkItem.warn"));
                                }
                            }
                            else
                            {
                                throw new WorkflowException(Sail.Messages.GetEngineMessage("nodemediatorrevise.ExecuteWorkItem.warn"));
                            }
                        }
                    }
                    else
                    {
                        throw new WorkflowException(Sail.Messages.GetEngineMessage("nodemediatorrevise.ExecuteWorkItem.exception"));
                    } 
                }
                else
                {
                    throw new WorkflowException(Sail.Messages.GetEngineMessage("nodemediatorrevise.ExecuteWorkItem.error"));
                }
                OnAfterExecuteWorkItem();
            }
            catch (System.Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// 完成工作项
        /// </summary>
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
                if (base.ITaskManager.TryCompleteAsDelegationReturn(
                        kit.useSQL(), taskId, activityResource.AppRunner))
                    return false;

                //完成本任务，返回任务已经转移到下一个会签任务，不继续执行其它节点
                //完成本任务后转入下一会签任务，不再继续执行其它节点
                base.ITaskManager.Complete(kit.useSQL(), taskId, activityResource.AppRunner);

                //设置活动节点的状态为完成状态
                //将活动节点状态设为完成
                var activityInstanceId = base.IActivityInstanceManager.GetByTask(taskId).ActivityInstanceId;
                base.IActivityInstanceManager.Complete(kit.useSQL(), activityInstanceId, activityResource.AppRunner);
            }
            return canContinueForwardCurrentNode;
        }

        /// <summary>
        /// 复制多实例子节点数据
        /// </summary>
        /// <param name="plist"></param>
        /// <param name="mainActivityInstance"></param>
        /// <param name="kit"></param>
        private void CloneChildNodeOfMI(PerformerList plist,
            ActivityInstanceEntity mainActivityInstance, 
            SQLBuilder kit)
        {
            var childNodeListOfMI = base.IActivityInstanceManager.GetValidActivityInstanceListOfMI(kit.useSQL(), mainActivityInstance.ActivityInstanceId, mainActivityInstance.ProcessInstanceId);

            ActivityInstanceEntity entity = new ActivityInstanceEntity();
            for (short i = 0; i < plist.Count; i++)
            {
                var userId = plist[i].UserId;
                var userName = plist[i].UserName;
                var isTaskExisted = IsTaskExisted(childNodeListOfMI, userId);
                if (isTaskExisted == true)
                {
                    //若活动或任务已存在则无需新建
                    continue;
                }

                //基于主节点复制子节点数据
                entity = IActivityInstanceManager.CreateActivityInstanceObject(mainActivityInstance);
                entity.SignIds = userId;
                entity.SignNames = userName;
                entity.MainActInstanceId = mainActivityInstance.ActivityInstanceId;

                //设置多实例子节点串行/并行执行顺序
                if (mainActivityInstance.MergeType == (short)MergeTypeEnum.Sequence)
                {
                    entity.CompleteOrder = (short)(i + 1);
                }
                else if (mainActivityInstance.MergeType == (short)MergeTypeEnum.Parallel)
                {
                    //并行模式下完成顺序相同，设为 -1
                    entity.CompleteOrder = -1;       
                }

                //串行会签时仅第一个节点运行，其余挂起
                if ((i > 0) && (mainActivityInstance.MergeType.Value == (short)MergeTypeEnum.Sequence))
                {
                    entity.ActivityState = (short)ActivityStateEnum.Suspended;
                }

                entity.ActivityInstanceId = IActivityInstanceManager.Insert(kit.useSQL(), entity);

                base.ITaskManager.Insert(kit.useSQL(), entity, plist[i], base.ActivityForwardContext.ActivityResource.AppRunner);
            }
        }

        /// <summary>
        /// 判断活动实例是否已经存在
        /// </summary>
        /// <param name="childNodeOfMI"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private Boolean IsTaskExisted(List<ActivityInstanceEntity> childNodeOfMI, 
            string userId)
        {
            // 会签子 AI 单人席：精确匹配 SignIds（禁止 Contains 子串误命中）
            if (string.IsNullOrWhiteSpace(userId) || childNodeOfMI == null)
                return false;
            return childNodeOfMI.Exists(a =>
                a != null && string.Equals(a.SignIds, userId, StringComparison.Ordinal));
        }

        /// <summary>
        /// 复制并行分支多实例节点数据
        /// </summary>
        private void CloneChildNodeOfAndSplitMI(PerformerList plist,
            string transitionId,
            ActivityInstanceEntity gatewayActivityInstance,
            ActivityInstanceEntity toActivityInstance, 
            SQLBuilder kit)
        {
            var childNodeListOfAndSplitMI = base.IActivityInstanceManager.GetValidSplitedActivityInstanceList(kit.useSQL(), gatewayActivityInstance.ProcessInstanceId, gatewayActivityInstance.ActivityInstanceId);

            ActivityInstanceEntity newChildInstance = new ActivityInstanceEntity();
            for (short i = 0; i < plist.Count; i++)
            {
                var userId = plist[i].UserId;
                var userName = plist[i].UserName;
                var isTaskExisted = IsTaskExisted(childNodeListOfAndSplitMI, userId);
                if (isTaskExisted == true)
                {
                    //若活动或任务已存在则无需新建
                    continue;
                }

                newChildInstance = IActivityInstanceManager.CreateActivityInstanceObject(toActivityInstance);
                newChildInstance.SignIds = userId;
                newChildInstance.SignNames = userName;

                newChildInstance.ActivityInstanceId = IActivityInstanceManager.Insert(kit.useSQL(), newChildInstance);

                base.InsertTransitionInstance(base.ActivityForwardContext.ProcessInstance,
                    transitionId,
                    gatewayActivityInstance,
                    newChildInstance,
                    TransitionTypeEnum.Forward,
                    TransitionFlyingTypeEnum.NotFlying,
                    base.ActivityForwardContext.ActivityResource.AppRunner,
                    kit);

                base.ITaskManager.Insert(kit.useSQL(), newChildInstance, plist[i], base.ActivityForwardContext.ActivityResource.AppRunner);
            }
        }
    }
}
