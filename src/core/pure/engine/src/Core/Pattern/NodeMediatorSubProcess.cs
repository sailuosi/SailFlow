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
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using saiflow.engine.Result;
using saiflow.engine.Runtime;
using saiflow.engine.Assignee;


using saiflow.engine.Client;
using saiflow.engine.Core;
namespace saiflow.engine.Pattern
{
    /// <summary>
    /// 子流程节点执行器
    /// </summary>
    internal class NodeMediatorSubProcess : NodeMediator
    {
        internal NodeMediatorSubProcess(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        internal NodeMediatorSubProcess(SQLBuilder kit, SailClient client)
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
                if (base.LinkContext.FromActivity.ActivityType == ActivityTypeEnum.SubProcessNode)
                {
                    //检查子流程是否结束
                    //检查子流程是否已结束
                    var pim = new ProcessInstanceManager(Sail);
                    var kit = SailSqlAccess.Sibling(Kit);
                    bool isCompleted = pim.CheckSubProcessInstanceCompleted(kit,
                        base.LinkContext.FromActivityInstance.Id.Value,
                        base.LinkContext.FromActivityInstance.ActivityId);
                    if (isCompleted == false)
                    {
                        throw new WfRuntimeException(Sail.Messages.GetEngineMessage("nodemediatorsubprocess.ExecuteWorkItem.notcompleted.warn",
                            string.Format("Activity:{0}", base.LinkContext.FromActivity.ActivityName)));
                    }
                }

                //完成当前的任务节点
                //完成当前工作项
                bool canContinueForwardCurrentNode = CompleteWorkItem(ActivityForwardContext.TaskId,
                    ActivityForwardContext.ActivityResource,
                    this.Kit);

                //获取下一步节点列表：并继续执行
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
        internal bool CompleteWorkItem(int? taskId,
            ActivityResource activityResource,
            SQLBuilder kit)
        {
            bool canContinueForwardCurrentNode = true;

            if (taskId != null)
            {
                //完成本任务，返回任务已经转移到下一个会签任务，不继续执行其它节点
                //完成本任务后转入下一会签任务，不再继续执行其它节点
                base.TaskManager.Complete(SailSqlAccess.Sibling(kit), taskId.Value, activityResource.AppRunner);
            }

            //设置活动节点的状态为完成状态
            //将活动节点状态设为完成
            base.ActivityInstanceManager.Complete(SailSqlAccess.Sibling(kit), base.LinkContext.FromActivityInstance.Id.Value, activityResource.AppRunner);
            base.LinkContext.FromActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;

            //先判断是否是多实例类型的任务
            //先判断是否为多实例任务
            var miDetail = base.LinkContext.FromActivity.MultiSignDetail;
            if (miDetail != null && miDetail.ComplexType == ComplexTypeEnum.SignTogether)
            {
                //取出主节点信息
                //获取主节点信息
                var mainNodeIndex = base.LinkContext.FromActivityInstance.MainActivityInstanceId.Value;
                var mainActivityInstance = base.ActivityInstanceManager.GetById(mainNodeIndex);

                //取出多实例节点列表
                //获取多实例节点列表
                var sqList = base.ActivityInstanceManager.GetActivityMulitipleInstanceWithState(
                    SailSqlAccess.Sibling(kit),mainNodeIndex,base.LinkContext.FromActivityInstance.ProcessInstanceId.Value,(short)ActivityStateEnum.Suspended).ToList<ActivityInstanceEntity>();

                if (base.LinkContext.FromActivity.MultiSignDetail.MergeType == MergeTypeEnum.Sequence)
                {
                    //取出最大执行节点
                    //获取最大执行序号节点
                    short maxOrder = (short)sqList.Max<ActivityInstanceEntity>(t => t.CompleteOrder).Value;

                    if (base.LinkContext.FromActivityInstance.CompleteOrder < maxOrder)
                    {
                        //设置下一个任务进入准备状态
                        //将下一任务设为就绪
                        var currentNodeIndex = (short)base.LinkContext.FromActivityInstance.CompleteOrder.Value;
                        var nextActivityInstance = sqList[currentNodeIndex];
                        nextActivityInstance.ActivityState = (short)ActivityStateEnum.Ready;
                        base.ActivityInstanceManager.Update(SailSqlAccess.Sibling(kit), nextActivityInstance);

                        //设置下一个任务对应的子流程进入运行状态
                        //将下一任务对应子流程设为运行
                        base.ProcessInstanceManager.RecallSubProcess(SailSqlAccess.Sibling(Kit),
                            nextActivityInstance.Id.Value,
                            activityResource.AppRunner);

                        canContinueForwardCurrentNode = false;
                        base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.ForwardToNextSequenceTask;
                    }
                    else if (base.LinkContext.FromActivityInstance.CompleteOrder == maxOrder)
                    {
                        //完成最后一个会签任务，会签主节点状态由挂起设置为准备状态
                        //完成最后一会签任务，并将会签主节点从挂起改为就绪
                        mainActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
                        base.ActivityInstanceManager.Update(SailSqlAccess.Sibling(kit), mainActivityInstance);
                        //将执行权责交由会签主节点
                        //将执行权移交会签主节点
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
                        //超过约定通过比例则进入下一节点
                        mainActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
                        base.ActivityInstanceManager.Update(SailSqlAccess.Sibling(kit), mainActivityInstance);
                        //将执行权责交由会签主节点
                        //将执行权移交会签主节点
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
                //并行容器中的子流程节点，每个办理人发起一个子流程
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
            var subProcessNode = (SubProcessNode)toActivity.Node;
            subProcessNode.ActivityInstance = toActivityInstance;

            //复制子流程启动用户信息
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
            // 实例化主节点 Activity
            var toActivityInstance = CreateActivityInstanceObject(toActivity,
                processInstance, activityResource.AppRunner);
            toActivityInstance.ActivityState = (short)ActivityStateEnum.Suspended;
            toActivityInstance.ComplexType = (short)ComplexTypeEnum.SignTogether;
            if (toActivity.MultiSignDetail.MergeType == MergeTypeEnum.Parallel)
            {
                toActivityInstance.CompleteOrder = toActivity.MultiSignDetail.CompleteOrder;
            }
            toActivityInstance = GenerateActivityAssignedUserInfo(toActivityInstance, activityResource);

            if (activityResource.AutoApprovePending)
            {
                throw new WfAssigneeException(
                    AssigneeReasonCodes.AssigneeAutoApproveNotForMultiSign,
                    "SignTogether (sub-process) cannot auto-approve with zero performers. ReasonCode="
                    + AssigneeReasonCodes.AssigneeAutoApproveNotForMultiSign);
            }

            activityResource.NextActivityPerformers ??= new Dictionary<string, PerformerList>();
            if (!activityResource.NextActivityPerformers.TryGetValue(toActivity.ActivityId, out var plist)
                || plist == null
                || plist.Count == 0)
            {
                throw new WfAssigneeException(
                    AssigneeReasonCodes.AssigneeEmpty,
                    "SignTogether requires performers. ReasonCode=" + AssigneeReasonCodes.AssigneeEmpty);
            }

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
            ActivityInstanceEntity entity = new ActivityInstanceEntity();
            for (short i = 0; i < plist.Count; i++)
            {
                entity = base.ActivityInstanceManager.CreateActivityInstanceObject(toActivityInstance);
                entity.AssignedUserIds = plist[i].UserId;
                entity.AssignedUserNames = plist[i].UserName;
                entity.GroupId = plist[i].GroupId;
                entity.GroupName = plist[i].GroupName;
                entity.GroupRole = plist[i].GroupRole;
                entity.UnitId = plist[i].UnitId;
                entity.UnitCode = plist[i].UnitCode;
                entity.UnitName = plist[i].UnitName;
                entity.MainActivityInstanceId = toActivityInstance.Id;
                entity.CompleteOrder = (short)(i + 1);

                //只有第一个节点处于运行状态，其它节点挂起
                //仅第一个节点运行，其余节点挂起
                if ((i > 0) && (toActivity.MultiSignDetail.MergeType == MergeTypeEnum.Sequence))
                {
                    entity.ActivityState = (short)ActivityStateEnum.Suspended;
                }

                entity.Id = base.ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), entity);

                base.TaskManager.Insert(SailSqlAccess.Sibling(kit), entity, plist[i], activityResource.AppRunner);

                //启动子流程
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
                //串行会签时仅第一个子流程运行，其余子流程挂起
                if ((i > 0) && (toActivity.MultiSignDetail.MergeType == MergeTypeEnum.Sequence))
                {
                    base.ProcessInstanceManager.Suspend(subKit,
                        startedResult.ProcessInstanceIdStarted, subRunner);
                }
            }
        }

        /// <summary>
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
