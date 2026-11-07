using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.hub.Localize;
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using saiflow.engine.Runtime;
using saiflow.engine.Xpdl;
using System.Threading.Tasks;

using saiflow.engine.Client;
using saiflow.engine.Core;
using saiflow.engine.Factories;

namespace saiflow.engine.Pattern
{
    /// <summary>
    /// 中间事件节点处理类
    /// </summary>
    internal class NodeMediatorInterConditionalGo : NodeMediator
    {
        internal NodeMediatorInterConditionalGo(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
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
                //检查条件节点运行条件
                CheckBeingExecutedInfo(Kit);

                //执行前Action列表
                //工作项执行前
                OnBeforeExecuteWorkItem();

                //完成当前的任务节点
                //完成当前任务节点
                bool canContinueForwardCurrentNode = CompleteWorkItem(ActivityForwardContext.TaskId,
                    ActivityForwardContext.ActivityResource,
                    Kit);

                //执行后Action列表
                //工作项执行后
                OnAfterExecuteWorkItem();

                //获取下一步节点列表：并继续执行
                //获取下一节点列表并继续执行
                if (canContinueForwardCurrentNode)
                {
                    ContinueForwardCurrentNode(ActivityForwardContext.IsNotParsedByTransition, Kit);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// 检查节点运行条件是否满足
        /// </summary>
        private void CheckBeingExecutedInfo(SQLBuilder kit)
        {
            var expression = ActivityForwardContext.CurrentActivity.TriggerDetail.Expression;
            var pvm = SailFactoryAccess.Managers(Sail).ProcessVariables;
            var isConditionPassed = pvm.ValidateProcessVariable(kit, LinkContext.FromActivityInstance.ProcessInstanceId.Value,
                expression);
            if (isConditionPassed == false)
            {
                throw new WfRuntimeException(Sail.Messages.GetEngineMessage("nodemediatorinterconditionalgo.CheckBeingExecutedInfo.exception"));
            }
        }

        /// <summary>
        /// 完成任务实例
        /// </summary>  
        internal bool CompleteWorkItem(int? taskId,
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
                //完成本任务，返回任务已经转移到下一个会签任务，不继续执行其它节点
                //完成本任务后转入下一会签任务，不再继续执行其它节点
                TaskManager.Complete(kit.useSQL(), taskId.Value, activityResource.AppRunner);
            }

            //设置活动节点的状态为完成状态
            //将活动节点状态设为完成
            ActivityInstanceManager.Complete(kit.useSQL(), LinkContext.FromActivityInstance.Id.Value, activityResource.AppRunner);

            LinkContext.FromActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
            bool canContinueForwardCurrentNode = LinkContext.FromActivityInstance.CanNotRenewInstance == 0;

            return canContinueForwardCurrentNode;
        }

        /// <summary>
        /// 创建活动任务转移实例数据
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
            int newActivityInstanceId = 0;
            bool isParallel = false;
            if (fromActivityInstance.ActivityType == (short)ActivityTypeEnum.GatewayNode)
            {
                //并发多实例分支判定（AndSplit Multiple）
                var processModel = Sail.ProcessModelFactory.CreateByProcessInstance(kit.useSQL(), processInstance);
                var activityNode = processModel.GetActivity(fromActivityInstance.ActivityId);
                isParallel = processModel.IsAndSplitMI(activityNode);
            }

            if (isParallel)
            {
                //并行多实例容器
                var entity = new ActivityInstanceEntity();
                var plist = activityResource.NextActivityPerformers[toActivity.ActivityId];

                //创建并行多实例分支
                for (var i = 0; i < plist.Count; i++)
                {
                    entity = CreateActivityInstanceObject(toActivity, processInstance, activityResource.AppRunner);
                    entity.AssignedUserIds = plist[i].UserId;
                    entity.AssignedUserNames = plist[i].UserName;
                    entity.GroupId = plist[i].GroupId;
                    entity.GroupName = plist[i].GroupName;
                    entity.GroupRole = plist[i].GroupRole;
                    entity.UnitId = plist[i].UnitId;
                    entity.UnitCode = plist[i].UnitCode;
                    entity.UnitName = plist[i].UnitName;
                    entity.ActivityState = (short)ActivityStateEnum.Ready;
                    //插入活动实例数据
                    //插入活动实例
                    entity.Id = ActivityInstanceManager.Insert(kit.useSQL(), entity);
                    //插入任务
                    TaskManager.Insert(kit.useSQL(), entity, plist[i], activityResource.AppRunner);
                    //插入转移数据
                    //插入转移实例
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
                //普通任务节点
                var toActivityInstance = CreateActivityInstanceObject(toActivity, processInstance, activityResource.AppRunner);

                //进入运行状态
                //就绪状态
                toActivityInstance.ActivityState = (short)ActivityStateEnum.Ready;
                toActivityInstance = GenerateActivityAssignedUserInfo(toActivityInstance, activityResource);

                //插入活动实例数据
                //插入活动实例
                newActivityInstanceId = ActivityInstanceManager.Insert(kit.useSQL(), toActivityInstance);

                //插入任务数据
                base.CreateNewTask(toActivityInstance, activityResource, kit);

                //插入转移数据
                //插入转移实例
                InsertTransitionInstance(processInstance,
                    transitionId,
                    fromActivityInstance,
                    toActivityInstance,
                    transitionType,
                    flyingType,
                    activityResource.AppRunner,
                    kit);
            }
        }
    }
}
