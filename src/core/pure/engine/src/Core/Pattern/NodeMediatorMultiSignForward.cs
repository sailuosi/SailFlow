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
    /// 加签子节点执行
    /// </summary>
    internal class NodeMediatorMultiSignForward : NodeMediator
    {
        internal NodeMediatorMultiSignForward(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
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

            //如果是非正常流转模式，不用判断通过率，直接跳转
            ////If it is an abnormal circulation mode, there is no need to judge the pass rate, just jump directly
            if (base.ActivityForwardContext.IsNotParsedByTransition == true)
            {
                canContinueForwardCurrentNode = true;
                return canContinueForwardCurrentNode;
            }

            //多实例会签和加签处理
            //先判断是否是会签和加签类型
            //主节点不为空时不发起加签可以正常运行
            //多实例会签与加签
            //先判断是否为会签/加签类型
            //主节点非空时可正常运行（未发起加签）
            var signforwardType = (SignForwardTypeEnum)Enum.Parse(typeof(SignForwardTypeEnum),
                base.ActivityForwardContext.FromActivityInstance.SignForwardType.Value.ToString());
            if (base.LinkContext.FromActivityInstance.MainActivityInstanceId != null
                && signforwardType != SignForwardTypeEnum.None)
            {
                //获取主节点信息
                var mainNodeIndex = base.LinkContext.FromActivityInstance.MainActivityInstanceId.Value;
                var mainActivityInstance = base.ActivityInstanceManager.GetById(mainNodeIndex);

                //检查加签是否全部完成；完成则进入下一步，否则等待
                if (signforwardType == SignForwardTypeEnum.SignForwardBehind
                    || signforwardType == SignForwardTypeEnum.SignForwardBefore)
                {
                    //取出处于挂起状态的多实例节点列表
                    //获取挂起状态的多实例节点列表
                    var sqList = base.ActivityInstanceManager.GetActivityMulitipleInstanceWithState(
                        kit.useSQL(),mainNodeIndex,base.LinkContext.FromActivityInstance.ProcessInstanceId.Value,(short)ActivityStateEnum.Suspended).ToList<ActivityInstanceEntity>();

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
                        //加签通过率
                        if (mainActivityInstance.CompleteOrder != null && mainActivityInstance.CompleteOrder <= maxOrder)
                        {
                            maxOrder = (short)mainActivityInstance.CompleteOrder;
                        }

                        if (base.LinkContext.FromActivityInstance.CompleteOrder < maxOrder)
                        {
                            //设置下一个节点进入等待办理状态
                            var currentNodeIndex = (short)base.LinkContext.FromActivityInstance.CompleteOrder.Value;
                            var nextActivityInstance = sqList[0];
                            nextActivityInstance.ActivityState = (short)ActivityStateEnum.Ready;
                            base.ActivityInstanceManager.Update(kit.useSQL(), nextActivityInstance);

                            //将下一节点设为待处理
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
                                //最后一个节点执行完，主节点进入完成状态，整个流程向下执行
                                //末节点执行完毕后主节点进入完成态，流程继续向下
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
                else if (signforwardType == SignForwardTypeEnum.SignForwardParallel)
                {
                    //取出多实例节点列表
                    //获取多实例节点列表
                    var sqList = base.ActivityInstanceManager.GetActivityMulitipleInstanceWithState(
                        kit.useSQL(),mainNodeIndex,base.LinkContext.FromActivityInstance.ProcessInstanceId.Value,null).ToList<ActivityInstanceEntity>();

                    //并行会签：按通过率判定当前节点是否完成
                    var allCount = sqList.Where(x => x.ActivityState != (short)ActivityStateEnum.Withdrawed).ToList().Count();
                    var completedCount = sqList.Where<ActivityInstanceEntity>(w => w.ActivityState == (short)ActivityStateEnum.Completed)
                        .ToList<ActivityInstanceEntity>()
                        .Count();
                    if (mainActivityInstance.CompareType == null || mainActivityInstance.CompareType == (short)CompareTypeEnum.Percentage)
                    {
                        //并行加签通过率判定
                        if (mainActivityInstance.CompleteOrder > 1)  
                            mainActivityInstance.CompleteOrder = 1;

                        if ((completedCount * 0.01) / (allCount * 0.01) >= mainActivityInstance.CompleteOrder)
                        {
                            var passed = base.ActivityInstanceManager.GetMiApprovalThresholdStatus(kit.useSQL(), base.LinkContext.FromActivityInstance,
                                    mainActivityInstance);
                            if (passed)
                            {
                                base.ActivityForwardContext.FromActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
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
                        //串行会签通过率（按人数）
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
                                base.ActivityForwardContext.FromActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
                                mainActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
                                base.ActivityInstanceManager.Update(Kit.useSQL(), mainActivityInstance);
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
            return canContinueForwardCurrentNode;
        }
    }
}
