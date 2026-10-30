using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.hub.Localize;
using saiflow.engine.Xpdl;
using saiflow.engine.Xpdl.Common;
using saiflow.engine.Xpdl.Node;
using saiflow.engine.Xpdl.Entity;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.engine.Core.Result;
using saiflow.engine.Core.Event;
using saiflow.engine.Core.Runtime;


using saiflow.engine.Client;
namespace saiflow.engine.Core.Pattern
{
    /// <summary>
    /// Node Mediator Sub Process Nested
    /// 子流程节点执行器
    /// </summary>
    internal class NodeMediatorSubProcessNested : NodeMediator
    {
        internal NodeMediatorSubProcessNested(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        internal NodeMediatorSubProcessNested(SQLBuilder kit, SailClient client)
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
                if (base.LinkContext.FromActivity.ActivityType == ActivityTypeEnum.SubProcessNode)
                {
                    //检查子流程是否结束
                    //Check if the sub process has ended
                    var pim = new ProcessInstanceManager(Sail);
                    var kit = SailSqlAccess.Sibling(Kit);
                    bool isCompleted = pim.CheckSubProcessInstanceCompleted(kit,
                        base.LinkContext.FromActivityInstance.Id,
                        base.LinkContext.FromActivityInstance.ActivityId);
                    if (isCompleted == false)
                    {
                        throw new WfRuntimeException(Sail.Messages.GetEngineMessage("nodemediatorsubprocess.ExecuteWorkItem.notcompleted.warn",
                            string.Format("Activity:{0}", base.LinkContext.FromActivity.ActivityName)));
                    }
                }

                //完成当前的任务节点
                //Complete current node
                bool canContinueForwardCurrentNode = CompleteWorkItem(ActivityForwardContext.TaskId,
                    ActivityForwardContext.ActivityResource,
                    this.Kit);

                //获取下一步节点列表：并继续执行
                //Get the next node list: and continue execution
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
        /// Complete work item
        /// </summary>
        internal bool CompleteWorkItem(int? taskId,
            ActivityResource activityResource,
            SQLBuilder kit)
        {
            bool canContinueForwardCurrentNode = true;

            if (taskId != null)
            {
                //完成本任务，返回任务已经转移到下一个会签任务，不继续执行其它节点
                //Complete this task, return that the task has been transferred to the next co signing task,
                //and do not continue to execute other nodes
                base.TaskManager.Complete(SailSqlAccess.Sibling(kit), taskId.Value, activityResource.AppRunner);
            }

            //设置活动节点的状态为完成状态
            //Set the status of the activity node to complete status
            base.ActivityInstanceManager.Complete(SailSqlAccess.Sibling(kit), base.LinkContext.FromActivityInstance.Id, activityResource.AppRunner);
            base.LinkContext.FromActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;

            //先判断是否是多实例类型的任务
            //First, determine whether it is a multi instance type task
            var miDetail = base.LinkContext.FromActivity.MultiSignDetail;
            if (miDetail != null && miDetail.ComplexType == ComplexTypeEnum.SignTogether)
            {
                //取出主节点信息
                //Retrieve main node information
                var mainNodeIndex = base.LinkContext.FromActivityInstance.MainActivityInstanceId.Value;
                var mainActivityInstance = base.ActivityInstanceManager.GetById(mainNodeIndex);

                //取出多实例节点列表
                //Retrieve a list of multiple instance nodes
                var sqList = base.ActivityInstanceManager.GetActivityMulitipleInstanceWithState(
                    SailSqlAccess.Sibling(kit),mainNodeIndex,base.LinkContext.FromActivityInstance.ProcessInstanceId,(short)ActivityStateEnum.Suspended).ToList<ActivityInstanceEntity>();

                if (base.LinkContext.FromActivity.MultiSignDetail.MergeType == MergeTypeEnum.Sequence)
                {
                    //取出最大执行节点
                    //Retrieve the maximum execution node 
                    short maxOrder = (short)sqList.Max<ActivityInstanceEntity>(t => t.CompleteOrder).Value;

                    if (base.LinkContext.FromActivityInstance.CompleteOrder < maxOrder)
                    {
                        //设置下一个任务进入准备状态
                        //Set the next task to enter preparation mode
                        var currentNodeIndex = (short)base.LinkContext.FromActivityInstance.CompleteOrder.Value;
                        var nextActivityInstance = sqList[currentNodeIndex];
                        nextActivityInstance.ActivityState = (short)ActivityStateEnum.Ready;
                        base.ActivityInstanceManager.Update(SailSqlAccess.Sibling(kit), nextActivityInstance);

                        //设置下一个任务对应的子流程进入运行状态
                        //Set the sub process corresponding to the next task to enter the running state
                        base.ProcessInstanceManager.RecallSubProcess(SailSqlAccess.Sibling(Kit),
                            nextActivityInstance.Id,
                            activityResource.AppRunner);

                        canContinueForwardCurrentNode = false;
                        base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.ForwardToNextSequenceTask;
                    }
                    else if (base.LinkContext.FromActivityInstance.CompleteOrder == maxOrder)
                    {
                        //完成最后一个会签任务，会签主节点状态由挂起设置为准备状态
                        //Complete the last countersignature task,
                        //and set the status of the countersignature master node from suspended to ready
                        mainActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
                        base.ActivityInstanceManager.Update(SailSqlAccess.Sibling(kit), mainActivityInstance);
                        //将执行权责交由会签主节点
                        //Transfer the execution responsibilities to the main signing node
                        base.LinkContext.FromActivityInstance = mainActivityInstance;
                    }
                }
                else if (base.LinkContext.FromActivity.MultiSignDetail.MergeType == MergeTypeEnum.Parallel)
                {
                    var allCount = sqList.Count();
                    var completedCount = sqList.Where<ActivityInstanceEntity>(w => w.ActivityState == (short)ActivityStateEnum.Completed)
                        .ToList<ActivityInstanceEntity>()
                        .Count();

                    if (completedCount / allCount >= mainActivityInstance.CompleteOrder)
                    {
                        //如果超过约定的比例数，则执行下一步节点
                        //If the agreed proportion is exceeded, proceed to the next node
                        mainActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
                        base.ActivityInstanceManager.Update(SailSqlAccess.Sibling(kit), mainActivityInstance);
                        //将执行权责交由会签主节点
                        //Transfer the execution responsibilities to the main signing node
                        base.LinkContext.FromActivityInstance = mainActivityInstance;
                    }
                    else
                    {
                        canContinueForwardCurrentNode = false;
                        base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.WaitingForCompletedMore;
                    }
                }
            }
            return canContinueForwardCurrentNode;
        }

        /// <summary>
        /// Create activity task transition instance
        /// 创建活动任务转移数据
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
                var processModel = ProcessModelFactory.CreateByProcessInstance(Sail, SailSqlAccess.Sibling(kit), processInstance);
                var activityNode = processModel.GetActivity(fromActivityInstance.ActivityId);
                isParallel = processModel.IsAndSplitMI(activityNode);
            }

            if (isParallel)
            {
                var entity = new ActivityInstanceEntity();
                var plist = activityResource.NextActivityPerformers[toActivity.ActivityId];
                //创建并行多实例分支
                //Create parallel multi instance branches
                for (var i = 0; i < plist.Count; i++)
                {
                    var performer = plist[i];
                    CreateSubProcessNode(toActivity, processInstance, fromActivityInstance, transitionId, transitionType,
                       flyingType, activityResource, performer, kit);
                }
            }
            else
            {
                if (toActivity.MultiSignDetail != null
                    && toActivity.MultiSignDetail.ComplexType == ComplexTypeEnum.SignTogether)
                {
                    CreateMultipleInstance(toActivity, processInstance, fromActivityInstance,
                        transitionId, transitionType, flyingType, activityResource, kit);
                }
                else
                {
                    CreateSubProcessNode(toActivity, processInstance, fromActivityInstance, transitionId, transitionType,
                        flyingType, activityResource, null, kit);
                }
            }
        }

        /// <summary>
        /// Create sub process 
        /// 创建子流程节点数据以及子流程记录
        /// </summary>
        private void CreateSubProcessNode(Activity toActivity,
           ProcessInstanceEntity processInstance,
           ActivityInstanceEntity fromActivityInstance,
           string transitionId,
           TransitionTypeEnum transitionType,
           TransitionFlyingTypeEnum flyingType,
           ActivityResource activityResource,
           Performer performer,
           SQLBuilder kit)
        {
            WfExecutedResult startedResult = WfExecutedResult.Default();

            var toActivityInstance = CreateActivityInstanceObject(toActivity, processInstance, activityResource.AppRunner);
            toActivityInstance.ActivityState = (short)ActivityStateEnum.Ready;
            if (performer != null)
            {
                //并行容器中的子流程节点，每个人发起一个子流程
                //Sub process nodes in parallel containers, where each person initiates a sub process
                toActivityInstance.AssignedUserIds = performer.UserId;
                toActivityInstance.AssignedUserNames = performer.UserName;

                base.ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), toActivityInstance);

                this.TaskManager.Insert(SailSqlAccess.Sibling(kit), toActivityInstance, performer, activityResource.AppRunner);
            }
            else
            {
                toActivityInstance = GenerateActivityAssignedUserInfo(toActivityInstance, activityResource);

                base.ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), toActivityInstance);

                base.CreateNewTask(toActivityInstance, activityResource, kit);
            }

            var newTransitionInstanceId = InsertTransitionInstance(processInstance,
                transitionId,
                fromActivityInstance,
                toActivityInstance,
                transitionType,
                flyingType,
                activityResource.AppRunner,
                kit);

            //启动子流程
            //Startup sub process
            var subProcessNode = (SubProcessNode)toActivity.Node;
            subProcessNode.ActivityInstance = toActivityInstance;

            //复制子流程启动用户信息
            //Copy sub process to start user information
            WfAppRunner subRunner = null;
            var performerList = new PerformerList();
            if (performer != null)
            {
                subRunner = CreateSubProcessRunner(activityResource.AppRunner,
                    new Performer(performer.UserId, performer.UserName),
                    kit);
                performerList.Add(performer);
            }
            else
            {
                subRunner = CreateSubProcessRunner(activityResource.AppRunner,
                   new Performer(activityResource.AppRunner.UserId, activityResource.AppRunner.UserName),
                   kit);
                performerList = activityResource.NextActivityPerformers[toActivity.ActivityId];
            }

            var runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceStartupSub(Sail, subRunner,
                processInstance,
                subProcessNode,
                performerList,
                kit,
                ref startedResult);

            runtimeInstance.OnWfProcessExecuted += runtimeInstance_OnWfProcessStarted;
            runtimeInstance.Execute(kit);

            void runtimeInstance_OnWfProcessStarted(object sender, WfEventArgs args)
            {
                startedResult = args.WfExecutedResult;
            }
        }

        /// <summary>
        /// Create multiple instance
        /// 会签类型的主节点, 多实例节点处理
        /// </summary>
        internal new void CreateMultipleInstance(Activity toActivity,
            ProcessInstanceEntity processInstance,
            ActivityInstanceEntity fromActivityInstance,
            String transitionId,
            TransitionTypeEnum transitionType,
            TransitionFlyingTypeEnum flyingType,
            ActivityResource activityResource,
            SQLBuilder kit)
        {
            //实例化主节点Activity
            //Instantiate the main node Activity
            var toActivityInstance = CreateActivityInstanceObject(toActivity,
                processInstance, activityResource.AppRunner);

            toActivityInstance.ActivityState = (short)ActivityStateEnum.Suspended;
            toActivityInstance.ComplexType = (short)ComplexTypeEnum.SignTogether;
            if (toActivity.MultiSignDetail.MergeType == MergeTypeEnum.Parallel)
            {
                toActivityInstance.CompleteOrder = toActivity.MultiSignDetail.CompleteOrder;
            }
            toActivityInstance = GenerateActivityAssignedUserInfo(toActivityInstance, activityResource);

            base.ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), toActivityInstance);

            InsertTransitionInstance(processInstance,
                transitionId,
                fromActivityInstance,
                toActivityInstance,
                transitionType,
                flyingType,
                activityResource.AppRunner,
                kit);

            //插入会签子节点实例数据
            //Insert signature sub node instance data
            var plist = activityResource.NextActivityPerformers[toActivity.ActivityId];
            ActivityInstanceEntity entity = new ActivityInstanceEntity();
            for (short i = 0; i < plist.Count; i++)
            {
                entity = base.ActivityInstanceManager.CreateActivityInstanceObject(toActivityInstance);
                entity.AssignedUserIds = plist[i].UserId;
                entity.AssignedUserNames = plist[i].UserName;
                entity.MainActivityInstanceId = toActivityInstance.Id;
                entity.CompleteOrder = (short)(i + 1);

                //只有第一个节点处于运行状态，其它节点挂起
                //Only the first node is running, while the other nodes are suspended
                if ((i > 0) && (toActivity.MultiSignDetail.MergeType == MergeTypeEnum.Sequence))
                {
                    entity.ActivityState = (short)ActivityStateEnum.Suspended;
                }

                entity.Id = base.ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), entity);

                base.TaskManager.Insert(SailSqlAccess.Sibling(kit), entity, plist[i], activityResource.AppRunner);

                //启动子流程
                //Startup sub process
                var subKit = Sail.UseRunSQL();
                var subProcessNode = (SubProcessNode)toActivity.Node;
                subProcessNode.ActivityInstance = entity;   
                WfAppRunner subRunner = CreateSubProcessRunner(activityResource.AppRunner, 
                    plist[i],
                    kit);

                WfExecutedResult startedResult = WfExecutedResult.Default();
                var runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceStartupSub(Sail, subRunner,
                    processInstance,
                    subProcessNode,
                    plist,
                    kit,
                    ref startedResult);

                if (runtimeInstance.WfExecutedResult.Status == WfExecutedStatus.Exception)
                {
                    throw new WfRuntimeException(runtimeInstance.WfExecutedResult.Message);
                }
                runtimeInstance.Execute(subKit);

                //如果是串行会签，只有第一个子流程可以运行，其它子流程处于挂起状态
                //If it is a serial countersignature,
                //only the first subprocess can run, and the other subprocesses are in a suspended state
                if ((i > 0) && (toActivity.MultiSignDetail.MergeType == MergeTypeEnum.Sequence))
                {
                    base.ProcessInstanceManager.Suspend(subKit,
                        startedResult.ProcessInstanceIdStarted, subRunner);
                }
            }
        }

        /// <summary>
        /// When creating a subprocess, regenerate the runner information
        /// 创建子流程时，重新生成runner信息
        /// </summary>
        /// <returns></returns>
        private WfAppRunner CreateSubProcessRunner(WfAppRunner runner,
            Performer performer,
            SQLBuilder kit)
        {
            WfAppRunner subRunner = new WfAppRunner();
            subRunner.ProcessId = runner.ProcessId;
            subRunner.AppInstanceCode = runner.AppInstanceCode;
            subRunner.AppInstanceId = runner.AppInstanceId;
            subRunner.AppName = runner.AppName;
            subRunner.Version = runner.Version;
            subRunner.UserId = performer.UserId;
            subRunner.UserName = performer.UserName;

            #region Dynamic sub process called
            //如果是动态调用子流程，则需要获取具体子流程实体对象
            //var isSubProcessNotExisted = false;
            //if (subProcessNode.SubProcessType == SubProcessTypeEnum.Dynamic)
            //{
            //    var subProcessId = runner.DynamicVariables[subProcessNode.SubVarName];
            //    if (!string.IsNullOrEmpty(subProcessId))
            //    {
            //        int processId = 0;
            //        int.TryParse(subProcessId, out processId);
            //        if (processId > 0)
            //        {
            //            var pm = new ProcessManager(Sail);
            //            var process = pm.GetById(SailSqlAccess.Sibling(kit), processId);
            //            if (process != null)
            //            {
            //                subProcessNode.SubProcessId = process.ProcessId;
            //            }
            //            else
            //            {
            //                isSubProcessNotExisted = true;
            //            }
            //        }
            //        else
            //        {
            //            isSubProcessNotExisted = true;
            //        }
            //    }
            //    else
            //    {
            //        isSubProcessNotExisted = true;
            //    }
            //}

            //if (isSubProcessNotExisted == true)
            //{
            //    throw new WfRuntimeException(Sail.Messages.GetEngineMessage("nodemediatorsubprocess.CreateSubProcessRunner.nonevariableparamsvalue.warn"));
            //}
            #endregion

            return subRunner;
        }
    }
}
