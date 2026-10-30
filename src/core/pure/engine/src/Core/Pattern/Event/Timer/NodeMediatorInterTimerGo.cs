
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.hub.Localize;
using saiflow.engine.Core.Runtime;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.engine.Xpdl;
using saiflow.engine.Xpdl.Entity;

using saiflow.engine.Client;
namespace saiflow.engine.Core.Pattern.Event.Timer
{
    /// <summary>
    /// Intermediate timer go node mediator
    /// 中间事件节点处理类
    /// </summary>
    internal class NodeMediatorInterTimerGo : NodeMediator
    {
        internal NodeMediatorInterTimerGo(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        /// <summary>
        /// Execute work item
        /// </summary>
        internal override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            try
            {
                //检查Timer节点运行条件
                //Check if the operating conditions of the node are met
                CheckBeingExecutedInfo();

                OnBeforeExecuteWorkItem();

                //完成当前的任务节点
                //Complete current task node
                bool canContinueForwardCurrentNode = CompleteWorkItem(ActivityForwardContext.TaskId,
                    ActivityForwardContext.ActivityResource,
                    Kit);

                OnAfterExecuteWorkItem();

                //获取下一步节点列表：并继续执行
                //Get the next node list: and continue execution
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
        /// Check if the operating conditions of the node are met
        /// 检查节点运行条件是否满足
        /// </summary>
        private void CheckBeingExecutedInfo()
        {
            if (LinkContext.FromActivityInstance.OverdueDateTime < DateTime.UtcNow)
            {
                throw new WfRuntimeException(LocalizeHelper.GetEngineMessage("nodemediatorintertimergo.CheckBeingExecutedInfo.exception"));
            }
        }

        /// <summary>
        /// Complete work item
        /// 完成任务实例
        /// </summary>     
        internal bool CompleteWorkItem(int? taskId,
            ActivityResource activityResource,
            SQLBuilder kit)
        {
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
                TaskManager.Complete(SailSqlAccess.Sibling(kit), taskId.Value, activityResource.AppRunner);
            }

            ActivityInstanceManager.Complete(SailSqlAccess.Sibling(kit), LinkContext.FromActivityInstance.Id, activityResource.AppRunner);

            LinkContext.FromActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
            bool canContinueForwardCurrentNode = LinkContext.FromActivityInstance.CanNotRenewInstance == 0;

            return canContinueForwardCurrentNode;
        }

        /// <summary>
        /// Create activity task transition instance
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
                //并发多实例分支判断(AndSplit Multiple)
                //Concurrent multi instance branch judgment (AndSplit Multiple)
                var processModel = ProcessModelFactory.CreateByProcessInstance(Sail, SailSqlAccess.Sibling(kit),
                    processInstance);
                var activityNode = processModel.GetActivity(fromActivityInstance.ActivityId);
                isParallel = processModel.IsAndSplitMI(activityNode);
            }

            if (isParallel)
            {
                //并行多实例容器
                //Parallel multi instance container
                var entity = new ActivityInstanceEntity();
                var plist = activityResource.NextActivityPerformers[toActivity.ActivityId];

                //创建并行多实例分支
                //Create parallel multi instance branches
                for (var i = 0; i < plist.Count; i++)
                {
                    entity = CreateActivityInstanceObject(toActivity, processInstance, activityResource.AppRunner);
                    entity.AssignedUserIds = plist[i].UserId;
                    entity.AssignedUserNames = plist[i].UserName;
                    entity.ActivityState = (short)ActivityStateEnum.Ready;

                    entity.Id = ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), entity);

                    TaskManager.Insert(SailSqlAccess.Sibling(kit), entity, plist[i], activityResource.AppRunner);

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
                //Normal task node
                var toActivityInstance = CreateActivityInstanceObject(toActivity, processInstance, activityResource.AppRunner);

                toActivityInstance.ActivityState = (short)ActivityStateEnum.Ready;
                toActivityInstance = GenerateActivityAssignedUserInfo(toActivityInstance, activityResource);

                newActivityInstanceId = ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), toActivityInstance);
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
        }
    }
}
