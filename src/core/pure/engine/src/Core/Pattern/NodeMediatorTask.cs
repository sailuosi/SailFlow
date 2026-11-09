using System;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Common;
using saiflow.engine.Event;
using saiflow.engine.Xpdl;
using saiflow.engine.Entity;


using saiflow.engine.Client;
using saiflow.engine.Core;
using saiflow.engine.Hooks;
namespace saiflow.engine.Pattern
{
    /// <summary>
    /// 任务节点执行器
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

        /// <summary>
        /// 执行工作项
        /// </summary>
        internal override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            try
            {
                OnBeforeExecuteWorkItem();

                bool canContinueForwardCurrentNode = CompleteWorkItem(ActivityForwardContext.TaskId,
                    ActivityForwardContext.ActivityResource,
                    this.Kit);

                OnAfterExecuteWorkItem();

                //获取下一节点列表并继续执行
                if (canContinueForwardCurrentNode)
                {
                    ContinueForwardCurrentNode(ActivityForwardContext.IsNotParsedByTransition, this.Kit);
                }
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
            FlowCommandContext runner = new FlowCommandContext
            {
                UserId = activityResource.AppRunner.UserId,         
                UserName = activityResource.AppRunner.UserName
            };

            if (taskId != null)
            {
                //完成本任务后转入下一会签任务，不再继续执行其它节点
                base.TaskManager.Complete(kit.useSQL(), taskId, activityResource.AppRunner);
            }

            //将活动节点状态设为完成
            base.ActivityInstanceManager.Complete(kit.useSQL(), base.LinkContext.FromActivityInstance.Id, activityResource.AppRunner);

            base.LinkContext.FromActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
            Boolean canContinueForwardCurrentNode = base.LinkContext.FromActivityInstance.CanNotRenewInstance == 0;

            return canContinueForwardCurrentNode;
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
            Boolean isParallel = false;
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
                ActivityInstanceEntity entity = null;
                var plist = activityResource.NextActivityPerformers[toActivity.ActivityId];

                //创建并行多实例分支
                for (var i = 0; i < plist.Count; i++)
                {
                    entity = base.CreateActivityInstanceObject(toActivity, processInstance, activityResource.AppRunner);
                    entity.AssignedUserIds = plist[i].UserId;
                    entity.AssignedUserNames = plist[i].UserName;
                    entity.GroupId = plist[i].GroupId;
                    entity.GroupName = plist[i].GroupName;
                    entity.GroupRole = plist[i].GroupRole;
                    entity.UnitId = plist[i].UnitId;
                    entity.UnitCode = plist[i].UnitCode;
                    entity.UnitName = plist[i].UnitName;
                    entity.ActivityState = (short)ActivityStateEnum.Ready;

                    entity.Id = base.ActivityInstanceManager.Insert(kit.useSQL(), entity);

                    base.TaskManager.Insert(kit.useSQL(), entity, plist[i], activityResource.AppRunner);

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
                var toActivityInstance = base.CreateActivityInstanceObject(toActivity, processInstance, activityResource.AppRunner);

                //处理多次退回后的返回
                WriteBackSrcOrgInformation(toActivityInstance, fromActivityInstance, kit);

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

                var runner = activityResource?.AppRunner;
                if (runner != null && processInstance?.Id != null)
                {
                    var hookCtx = FlowCapabilityPipeline.BuildHookContext(
                        Sail,
                        kit,
                        runner,
                        processInstance.Id,
                        toActivityInstance,
                        null,
                        EventFireTypeEnum.OnActivityCreated,
                        FlowOperation.Run);
                    FlowCapabilityPipeline.AfterSuccess(
                        Sail, kit, hookCtx, EventFireTypeEnum.OnActivityCreated);
                }
            }
        }

        /// <summary>
        /// 维护多次退回时的源节点信息
        /// </summary>
        /// <param name="toActivityInstance"></param>
        /// <param name="fromActivityInstance"></param>
        /// <param name="kit"></param>
        private void WriteBackSrcOrgInformation(ActivityInstanceEntity toActivityInstance, 
            ActivityInstanceEntity fromActivityInstance,
            SQLBuilder kit)
        {
            if (fromActivityInstance.BackSrcActivityInstanceId != null)
            {
                var backSrcActivityInstance = base.ActivityInstanceManager.GetById(kit.useSQL(), fromActivityInstance.BackSrcActivityInstanceId);
                toActivityInstance.BackSrcActivityInstanceId = backSrcActivityInstance.BackSrcActivityInstanceId;
                toActivityInstance.BackOrgActivityInstanceId = backSrcActivityInstance.BackOrgActivityInstanceId;
            }
        }
    }
}
