using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Xpdl.Common;
using saiflow.engine.Xpdl.Entity;
using saiflow.engine.Business.Entity;
using saiflow.engine.Core.Result;

using saiflow.engine.Client;
namespace saiflow.engine.Core.Pattern
{
    /// <summary>
    /// Sign together child node execution
    /// 会签子节点执行
    /// </summary>
    internal class NodeMediatorMultiSignTogether : NodeMediator
    {
        internal NodeMediatorMultiSignTogether(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
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
                OnBeforeExecuteWorkItem();

                bool canContinueForwardCurrentNode = CompleteWorkItem(ActivityForwardContext.TaskId,
                    ActivityForwardContext.ActivityResource,
                    this.Kit);

                OnAfterExecuteWorkItem();

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
            }

            //设置活动节点的状态为完成状态
            //Set the status of the activity node to complete status
            base.ActivityInstanceManager.Complete(SailSqlAccess.Sibling(kit), base.LinkContext.FromActivityInstance.Id.Value, activityResource.AppRunner);

            base.LinkContext.FromActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;

            //多实例会签和加签处理
            //先判断是否是会签和加签类型
            //主节点不为空时不发起加签可以正常运行
            //Multiple practical meetings for signing and adding signatures
            //First, determine whether it is a countersignature or countersignature type
            //When the master node is not empty, it can run normally without initiating signing
            var complexType = base.LinkContext.FromActivity.MultiSignDetail.ComplexType;
            if (complexType == ComplexTypeEnum.SignTogether
                || complexType == ComplexTypeEnum.SignForward && base.LinkContext.FromActivityInstance.MainActivityInstanceId != null)
            {
                //取出主节点信息
                //Get main activityinstance
                var mainNodeIndex = base.LinkContext.FromActivityInstance.MainActivityInstanceId.Value;
                var mainActivityInstance = base.ActivityInstanceManager.GetById(mainNodeIndex);

                //串行会签和并行会签的处理
                //Processing of sequence and parallel signatures
                if (complexType == ComplexTypeEnum.SignTogether)
                {
                    if (base.LinkContext.FromActivity.MultiSignDetail.MergeType == MergeTypeEnum.Sequence)   
                    {
                        //取出处于挂起状态多实例节点列表
                        //Retrieve the list of multi instance nodes in a suspended state
                        var childList = base.ActivityInstanceManager.GetValidActivityInstanceListOfMI(SailSqlAccess.Sibling(kit), mainActivityInstance.Id.Value, mainActivityInstance.ProcessInstanceId.Value);
                        var sqList = childList.Where(a => a.ActivityState == (short)ActivityStateEnum.Suspended).ToList();
                        short maxOrder = 0;
                        if (sqList != null && sqList.Count > 0)
                        {
                            //取出最大执行节点
                            //Retrieve the maximum execution node
                            maxOrder = (short)sqList.Max<ActivityInstanceEntity>(t => t.CompleteOrder.Value);
                        }
                        else
                        {
                            //最后一个执行节点
                            //The last execution node
                            maxOrder = (short)base.LinkContext.FromActivityInstance.CompleteOrder.Value;
                        }

                        if (mainActivityInstance.CompareType == (short)CompareTypeEnum.Count || mainActivityInstance.CompareType == null)
                        {
                            //串行会签通过率（按人数判断）
                            //Sequence signature pass rate (judged by number of people)
                            if (base.LinkContext.FromActivityInstance.CompleteOrder < maxOrder)
                            {
                                //设置下一个任务进入准备状态
                                //Set the next task to enter preparation mode
                                var nextActivityInstance = sqList[0];     //Always take the first suspended instance
                                nextActivityInstance.ActivityState = (short)ActivityStateEnum.Ready;
                                base.ActivityInstanceManager.Update(SailSqlAccess.Sibling(kit), nextActivityInstance);

                                //更新主节点的执行人员列表
                                //Update the list of executing personnel for the main node
                                mainActivityInstance.NextStepPerformers = NextStepUtility.SerializeNextStepPerformers(
                                    base.ActivityForwardContext.ActivityResource.AppRunner.NextActivityPerformers,
                                    Sail.JsonSerializer);
                                base.ActivityInstanceManager.Update(SailSqlAccess.Sibling(kit), mainActivityInstance);

                                canContinueForwardCurrentNode = false;
                                base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.ForwardToNextSequenceTask;
                            }
                            else if (base.LinkContext.FromActivityInstance.CompleteOrder == maxOrder)
                            {
                                var passed = base.ActivityInstanceManager.GetMiApprovalThresholdStatus(SailSqlAccess.Sibling(kit), base.LinkContext.FromActivityInstance, 
                                    mainActivityInstance);
                                if (passed)
                                {
                                    //完成最后一个会签任务，会签主节点状态由挂起设置为完成状态
                                    //Complete the last countersignature task,
                                    //and set the status of the countersignature master node from suspended to completed
                                    mainActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
                                    base.ActivityInstanceManager.Update(SailSqlAccess.Sibling(kit), mainActivityInstance);

                                    //更新未办理完成节点状态为取消状态
                                    //Update the status of unfinished nodes to cancelled status
                                    base.ActivityInstanceManager.CancelUnCompletedMultipleInstance(SailSqlAccess.Sibling(kit), mainActivityInstance.Id.Value, runner);
                                }
                                else
                                {
                                    canContinueForwardCurrentNode = false;
                                    base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.NotEnoughApprovalBranchesCount;
                                }
                            }
                        }
                        else
                        {
                            //串行会签未设置通过率的判断
                            //Judgment of no set pass rate for sequence sign together
                            if (mainActivityInstance.CompleteOrder == null || mainActivityInstance.CompleteOrder > 1)
                                mainActivityInstance.CompleteOrder = 1;

                            if ((base.LinkContext.FromActivityInstance.CompleteOrder * 0.01) / (maxOrder * 0.01) >= mainActivityInstance.CompleteOrder)
                            {
                                var passed = base.ActivityInstanceManager.GetMiApprovalThresholdStatus(SailSqlAccess.Sibling(kit), base.LinkContext.FromActivityInstance,
                                    mainActivityInstance);
                                if (passed)
                                {
                                    //完成最后一个会签任务，会签主节点状态由挂起设置为完成状态
                                    //Complete the last countersignature task,
                                    //and set the status of the countersignature master node from suspended to completed
                                    mainActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
                                    base.ActivityInstanceManager.Update(SailSqlAccess.Sibling(kit), mainActivityInstance);

                                    //更新未办理完成节点状态为取消状态
                                    //Update the status of unfinished nodes to cancelled status
                                    base.ActivityInstanceManager.CancelUnCompletedMultipleInstance(SailSqlAccess.Sibling(kit), mainActivityInstance.Id.Value, runner);
                                }
                                else
                                {
                                    canContinueForwardCurrentNode = false;
                                    base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.NotEnoughApprovalBranchesCount;
                                }
                            }
                            else
                            {
                                //设置下一个任务进入准备状态
                                //Set the next task to enter preparation mode
                                var nextActivityInstance = sqList[0];     //Always take the first suspended instance
                                nextActivityInstance.ActivityState = (short)ActivityStateEnum.Ready;
                                base.ActivityInstanceManager.Update(SailSqlAccess.Sibling(kit), nextActivityInstance);

                                //更新主节点的执行人员列表
                                //Update the list of executing personnel for the main node
                                mainActivityInstance.NextStepPerformers = NextStepUtility.SerializeNextStepPerformers(
                                    base.ActivityForwardContext.ActivityResource.AppRunner.NextActivityPerformers,
                                    Sail.JsonSerializer);
                                base.ActivityInstanceManager.Update(SailSqlAccess.Sibling(kit), mainActivityInstance);

                                canContinueForwardCurrentNode = false;
                                base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.ForwardToNextSequenceTask;
                            }
                        }
                    }
                    else if (base.LinkContext.FromActivity.MultiSignDetail.MergeType == MergeTypeEnum.Parallel)   
                    {
                        //取出处于多实例节点列表
                        //Retrieve the list of nodes in multiple instances
                        var sqList = base.ActivityInstanceManager.GetActivityMulitipleInstanceWithStateBatch(SailSqlAccess.Sibling(kit), mainActivityInstance.ProcessInstanceId.Value, 
                            mainActivityInstance.Id.Value, null);
                        var allCount = sqList.Where(x => x.ActivityState != (short)ActivityStateEnum.Withdrawed
                             && x.ActivityState != (short)ActivityStateEnum.Sendbacked).ToList().Count();
                        var completedCount = sqList.Where<ActivityInstanceEntity>(w => w.ActivityState == (short)ActivityStateEnum.Completed)
                            .ToList<ActivityInstanceEntity>()
                            .Count();

                        if (mainActivityInstance.CompareType == null || mainActivityInstance.CompareType == (short)CompareTypeEnum.Percentage)
                        {
                            //并行会签未设置通过率的判断
                            ////Judgment of no set pass rate for parallel signature
                            if (mainActivityInstance.CompleteOrder == null || mainActivityInstance.CompleteOrder > 1)
                                mainActivityInstance.CompleteOrder = 1;

                            if ((completedCount * 0.01) / (allCount * 0.01) >= mainActivityInstance.CompleteOrder)
                            {
                                var passed = base.ActivityInstanceManager.GetMiApprovalThresholdStatus(SailSqlAccess.Sibling(kit), base.LinkContext.FromActivityInstance,
                                    mainActivityInstance);
                                if (passed)
                                {
                                    //如果超过约定的比例数，则执行下一步节点
                                    //If the agreed proportion is exceeded, proceed to the next node
                                    mainActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
                                    base.ActivityInstanceManager.Update(SailSqlAccess.Sibling(kit), mainActivityInstance);
                                    //更新未办理完成节点状态为取消状态
                                    //Update the status of unfinished nodes to cancelled status
                                    base.ActivityInstanceManager.CancelUnCompletedMultipleInstance(SailSqlAccess.Sibling(kit), mainActivityInstance.Id.Value, runner);
                                }
                                else
                                {
                                    canContinueForwardCurrentNode = false;
                                    base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.NotEnoughApprovalBranchesCount;
                                }
                            }
                            else
                            {
                                //更新主节点的执行人员列表
                                //Update the list of executing personnel for the main node
                                mainActivityInstance.NextStepPerformers = NextStepUtility.SerializeNextStepPerformers(
                                    base.ActivityForwardContext.ActivityResource.AppRunner.NextActivityPerformers,
                                    Sail.JsonSerializer);
                                base.ActivityInstanceManager.Update(SailSqlAccess.Sibling(kit), mainActivityInstance);

                                canContinueForwardCurrentNode = false;
                                base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.WaitingForCompletedMore;
                            }
                        }
                        else
                        {
                            //并行会签通过率（按人数判断）
                            //Parallel signing pass rate (judged by number of performers)
                            if (mainActivityInstance.CompleteOrder != null && mainActivityInstance.CompleteOrder > allCount)
                            {
                                mainActivityInstance.CompleteOrder = allCount;
                            }

                            if (mainActivityInstance.CompleteOrder > completedCount)
                            {
                                //更新主节点的执行人员列表
                                //Update the list of executing personnel for the main node
                                mainActivityInstance.NextStepPerformers = NextStepUtility.SerializeNextStepPerformers(
                                    base.ActivityForwardContext.ActivityResource.AppRunner.NextActivityPerformers,
                                    Sail.JsonSerializer);
                                base.ActivityInstanceManager.Update(SailSqlAccess.Sibling(kit), mainActivityInstance);

                                canContinueForwardCurrentNode = false;
                                base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.WaitingForCompletedMore;
                            }
                            else if (mainActivityInstance.CompleteOrder == completedCount)
                            {
                                var passed = base.ActivityInstanceManager.GetMiApprovalThresholdStatus(SailSqlAccess.Sibling(kit), base.LinkContext.FromActivityInstance, 
                                    mainActivityInstance);
                                if (passed)
                                {
                                    //如果超过约定的比例数，则执行下一步节点
                                    //If the agreed proportion is exceeded, proceed to the next node
                                    mainActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
                                    base.ActivityInstanceManager.Update(SailSqlAccess.Sibling(kit), mainActivityInstance);
                                    //更新未办理完成节点状态为取消状态
                                    //Update the status of unfinished nodes to cancelled status
                                    base.ActivityInstanceManager.CancelUnCompletedMultipleInstance(SailSqlAccess.Sibling(kit), mainActivityInstance.Id.Value, runner);
                                }
                                else
                                {
                                    canContinueForwardCurrentNode = false;
                                    base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.NotEnoughApprovalBranchesCount;
                                }
                            }
                        }
                    }
                }
            }
            return canContinueForwardCurrentNode;
        }
    }
}