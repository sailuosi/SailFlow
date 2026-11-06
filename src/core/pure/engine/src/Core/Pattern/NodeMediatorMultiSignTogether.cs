using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Xpdl;
using saiflow.engine.Entity;
using saiflow.engine.Result;

using saiflow.engine.Client;
using saiflow.engine.Core;
namespace saiflow.engine.Pattern
{
    /// <summary>
    /// 会签子节点执行
    /// </summary>
    internal class NodeMediatorMultiSignTogether : NodeMediator
    {
        internal NodeMediatorMultiSignTogether(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
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
                OnBeforeExecuteWorkItem();

                bool canContinueForwardCurrentNode = CompleteWorkItem(ActivityForwardContext.TaskId,
                    ActivityForwardContext.ActivityResource,
                    this.Kit);

                OnAfterExecuteWorkItem();

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

            WfAppRunner runner = new WfAppRunner
            {
                UserId = activityResource.AppRunner.UserId,        
                UserName = activityResource.AppRunner.UserName
            };

            if (taskId != null)
            {
                //完成本任务，返回任务已经转移到下一个会签任务，不继续执行其它节点
                //完成本任务后转入下一会签任务，不再继续执行其它节点
                base.TaskManager.Complete(kit.useSQL(), taskId.Value, activityResource.AppRunner);
            }

            //设置活动节点的状态为完成状态
            //将活动节点状态设为完成
            base.ActivityInstanceManager.Complete(kit.useSQL(), base.LinkContext.FromActivityInstance.Id.Value, activityResource.AppRunner);

            base.LinkContext.FromActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;

            //多实例会签和加签处理
            //先判断是否是会签和加签类型
            //主节点不为空时不发起加签可以正常运行
            //多实例会签与加签
            //先判断是否为会签/加签类型
            //主节点非空时可正常运行（未发起加签）
            var complexType = base.LinkContext.FromActivity.MultiSignDetail.ComplexType;
            if (complexType == ComplexTypeEnum.SignTogether
                || complexType == ComplexTypeEnum.SignForward && base.LinkContext.FromActivityInstance.MainActivityInstanceId != null)
            {
                //取出主节点信息
                //获取主活动实例
                var mainNodeIndex = base.LinkContext.FromActivityInstance.MainActivityInstanceId.Value;
                var mainActivityInstance = base.ActivityInstanceManager.GetById(mainNodeIndex);

                //串行会签和并行会签的处理
                //处理串行/并行会签
                if (complexType == ComplexTypeEnum.SignTogether)
                {
                    if (base.LinkContext.FromActivity.MultiSignDetail.MergeType == MergeTypeEnum.Sequence)   
                    {
                        //取出处于挂起状态多实例节点列表
                        //获取挂起状态的多实例节点列表
                        var childList = base.ActivityInstanceManager.GetValidActivityInstanceListOfMI(kit.useSQL(), mainActivityInstance.Id.Value, mainActivityInstance.ProcessInstanceId.Value);
                        var sqList = childList.Where(a => a.ActivityState == (short)ActivityStateEnum.Suspended).ToList();
                        short maxOrder = 0;
                        if (sqList != null && sqList.Count > 0)
                        {
                            //取出最大执行节点
                            //获取最大执行序号节点
                            maxOrder = (short)sqList.Max<ActivityInstanceEntity>(t => t.CompleteOrder.Value);
                        }
                        else
                        {
                            //最后一个执行节点
                            maxOrder = (short)base.LinkContext.FromActivityInstance.CompleteOrder.Value;
                        }

                        if (mainActivityInstance.CompareType == (short)CompareTypeEnum.Count || mainActivityInstance.CompareType == null)
                        {
                            //串行会签通过率（按人数判断）
                            //串行会签通过率（按人数判定）
                            if (base.LinkContext.FromActivityInstance.CompleteOrder < maxOrder)
                            {
                                //设置下一个任务进入准备状态
                                //将下一任务设为就绪
                                var nextActivityInstance = sqList[0];     //Always take the first suspended instance
                                nextActivityInstance.ActivityState = (short)ActivityStateEnum.Ready;
                                base.ActivityInstanceManager.Update(kit.useSQL(), nextActivityInstance);

                                //更新主节点执行人员列表
                                mainActivityInstance.NextStepPerformers = NextStepUtility.SerializeNextStepPerformers(
                                    base.ActivityForwardContext.ActivityResource.AppRunner.NextActivityPerformers,
                                    Sail.JsonSerializer);
                                base.ActivityInstanceManager.Update(kit.useSQL(), mainActivityInstance);

                                canContinueForwardCurrentNode = false;
                                base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.ForwardToNextSequenceTask;
                            }
                            else if (base.LinkContext.FromActivityInstance.CompleteOrder == maxOrder)
                            {
                                var passed = base.ActivityInstanceManager.GetMiApprovalThresholdStatus(kit.useSQL(), base.LinkContext.FromActivityInstance, 
                                    mainActivityInstance);
                                if (passed)
                                {
                                    //完成最后一个会签任务，会签主节点状态由挂起设置为完成状态
                                    //完成最后一会签任务，并将会签主节点从挂起改为完成
                                    mainActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
                                    base.ActivityInstanceManager.Update(kit.useSQL(), mainActivityInstance);

                                    //将未完成节点状态更新为取消
                                    base.ActivityInstanceManager.CancelUnCompletedMultipleInstance(kit.useSQL(), mainActivityInstance.Id.Value, runner);
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
                            //串行会签未设通过率时的判定
                            if (mainActivityInstance.CompleteOrder == null || mainActivityInstance.CompleteOrder > 1)
                                mainActivityInstance.CompleteOrder = 1;

                            if ((base.LinkContext.FromActivityInstance.CompleteOrder * 0.01) / (maxOrder * 0.01) >= mainActivityInstance.CompleteOrder)
                            {
                                var passed = base.ActivityInstanceManager.GetMiApprovalThresholdStatus(kit.useSQL(), base.LinkContext.FromActivityInstance,
                                    mainActivityInstance);
                                if (passed)
                                {
                                    //完成最后一个会签任务，会签主节点状态由挂起设置为完成状态
                                    //完成最后一会签任务，并将会签主节点从挂起改为完成
                                    mainActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
                                    base.ActivityInstanceManager.Update(kit.useSQL(), mainActivityInstance);

                                    //将未完成节点状态更新为取消
                                    base.ActivityInstanceManager.CancelUnCompletedMultipleInstance(kit.useSQL(), mainActivityInstance.Id.Value, runner);
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
                                //将下一任务设为就绪
                                var nextActivityInstance = sqList[0];     //Always take the first suspended instance
                                nextActivityInstance.ActivityState = (short)ActivityStateEnum.Ready;
                                base.ActivityInstanceManager.Update(kit.useSQL(), nextActivityInstance);

                                //更新主节点执行人员列表
                                mainActivityInstance.NextStepPerformers = NextStepUtility.SerializeNextStepPerformers(
                                    base.ActivityForwardContext.ActivityResource.AppRunner.NextActivityPerformers,
                                    Sail.JsonSerializer);
                                base.ActivityInstanceManager.Update(kit.useSQL(), mainActivityInstance);

                                canContinueForwardCurrentNode = false;
                                base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.ForwardToNextSequenceTask;
                            }
                        }
                    }
                    else if (base.LinkContext.FromActivity.MultiSignDetail.MergeType == MergeTypeEnum.Parallel)   
                    {
                        //获取多实例节点列表
                        var sqList = base.ActivityInstanceManager.GetActivityMulitipleInstanceWithStateBatch(kit.useSQL(), mainActivityInstance.ProcessInstanceId.Value, 
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
                                var passed = base.ActivityInstanceManager.GetMiApprovalThresholdStatus(kit.useSQL(), base.LinkContext.FromActivityInstance,
                                    mainActivityInstance);
                                if (passed)
                                {
                                    //如果超过约定的比例数，则执行下一步节点
                                    //超过约定通过比例则进入下一节点
                                    mainActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
                                    base.ActivityInstanceManager.Update(kit.useSQL(), mainActivityInstance);
                                    //将未完成节点状态更新为取消
                                    base.ActivityInstanceManager.CancelUnCompletedMultipleInstance(kit.useSQL(), mainActivityInstance.Id.Value, runner);
                                }
                                else
                                {
                                    canContinueForwardCurrentNode = false;
                                    base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.NotEnoughApprovalBranchesCount;
                                }
                            }
                            else
                            {
                                //更新主节点执行人员列表
                                mainActivityInstance.NextStepPerformers = NextStepUtility.SerializeNextStepPerformers(
                                    base.ActivityForwardContext.ActivityResource.AppRunner.NextActivityPerformers,
                                    Sail.JsonSerializer);
                                base.ActivityInstanceManager.Update(kit.useSQL(), mainActivityInstance);

                                canContinueForwardCurrentNode = false;
                                base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.WaitingForCompletedMore;
                            }
                        }
                        else
                        {
                            //并行会签通过率（按办理人数）
                            if (mainActivityInstance.CompleteOrder != null && mainActivityInstance.CompleteOrder > allCount)
                            {
                                mainActivityInstance.CompleteOrder = allCount;
                            }

                            if (mainActivityInstance.CompleteOrder > completedCount)
                            {
                                //更新主节点执行人员列表
                                mainActivityInstance.NextStepPerformers = NextStepUtility.SerializeNextStepPerformers(
                                    base.ActivityForwardContext.ActivityResource.AppRunner.NextActivityPerformers,
                                    Sail.JsonSerializer);
                                base.ActivityInstanceManager.Update(kit.useSQL(), mainActivityInstance);

                                canContinueForwardCurrentNode = false;
                                base.WfNodeMediatedResult.Feedback = WfNodeMediatedFeedback.WaitingForCompletedMore;
                            }
                            else if (mainActivityInstance.CompleteOrder == completedCount)
                            {
                                var passed = base.ActivityInstanceManager.GetMiApprovalThresholdStatus(kit.useSQL(), base.LinkContext.FromActivityInstance, 
                                    mainActivityInstance);
                                if (passed)
                                {
                                    //如果超过约定的比例数，则执行下一步节点
                                    //超过约定通过比例则进入下一节点
                                    mainActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
                                    base.ActivityInstanceManager.Update(kit.useSQL(), mainActivityInstance);
                                    //将未完成节点状态更新为取消
                                    base.ActivityInstanceManager.CancelUnCompletedMultipleInstance(kit.useSQL(), mainActivityInstance.Id.Value, runner);
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