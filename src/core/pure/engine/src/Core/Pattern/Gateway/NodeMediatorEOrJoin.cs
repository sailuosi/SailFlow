using saiflow.engine.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using saiflow.data;
using mooSQL.data;
using saiflow.hub.Localize;
using saiflow.engine.Common;
using saiflow.engine.Business.Entity;
using saiflow.engine.Xpdl;
using saiflow.engine.Xpdl.Entity;

namespace saiflow.engine.Core.Pattern.Gateway
{
    /// <summary>
    /// EOrJoin Node Mediator
    /// EOrJoin 节点处理类
    /// </summary>
    internal class NodeMediatorEOrJoin : NodeMediatorGateway, ICompleteGatewayAutomaticlly
    {
        internal NodeMediatorEOrJoin(Activity activity, IProcessModel processModel, SQLBuilder kit, SailClient client)
            : base(activity, processModel, kit, client)
        {

        }

        /// <summary>
        /// Complete automatically
        /// </summary>
        public NodeAutoExecutedResult CompleteAutomaticlly(ProcessInstanceEntity processInstance,
            string transitionId,
            Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            WfAppRunner runner,
            SQLBuilder kit)
        {
            NodeAutoExecutedResult result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.Unknown);

            if (GatewayActivity.GatewayDetail.JoinPassType == GatewayJoinPassEnum.Count)
            {
                result = CompleteAutomaticallyByTokensCount(processInstance, transitionId, fromActivity, fromActivityInstance, runner, kit);
            }
            else if (GatewayActivity.GatewayDetail.JoinPassType == GatewayJoinPassEnum.Forced)
            {
                result = CompleteAutomaticallyByForcedBranchesCount(processInstance, transitionId, fromActivity, fromActivityInstance, runner, kit);
            }
            return result;
        }

        /// <summary>
        /// Node completion method for EORJoin merging
        /// The front-end dynamically passes the number of tokens to be merged
        /// EOrJoin合并时的节点完成方法
        /// 前端动态传递要合并的Tokens的数目
        /// </summary>
        private NodeAutoExecutedResult CompleteAutomaticallyByTokensCount(ProcessInstanceEntity processInstance,
            string transitionId,
            Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            WfAppRunner runner,
            SQLBuilder kit)
        {
            NodeAutoExecutedResult result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.Unknown);
            if (runner.ControlParameterSheet == null || runner.ControlParameterSheet.EOrJoinTokenPassCount <= 0)
            {
                result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.Failed);
                throw new WfXpdlException(Sail.Messages.GetEngineMessage("nodemediatoreorjoin.CompleteAutomaticallyByTokensCount.error"));
            }
            var tokensCountRequired = runner.ControlParameterSheet.EOrJoinTokenPassCount;
            result = CompleteAutomaticallyInternal(processInstance, transitionId, fromActivity, fromActivityInstance, tokensCountRequired, runner, kit);
            return result;
        }

        /// <summary>
        /// Node completion method for EORJoin merging
        /// Determine the completion of merging nodes based on whether to force branching as defined on the pre transfer
        /// EOrJoin合并时的节点完成方法
        /// 根据前置转移上定义的是否强制分支来判断完成合并节点
        /// </summary>
        private NodeAutoExecutedResult CompleteAutomaticallyByForcedBranchesCount(ProcessInstanceEntity processInstance,
            string transitionId,
            Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            WfAppRunner runner,
            SQLBuilder kit)
        {
            NodeAutoExecutedResult result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.Unknown);

            IList<Transition> forcedTransitionList = null;
            var tokensCountRequired = base.ProcessModel.GetForcedBranchesCountBeforeEOrJoin(GatewayActivity, out forcedTransitionList);
            if (tokensCountRequired == 0)
            {
                result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.Failed);
                throw new WfXpdlException(Sail.Messages.GetEngineMessage("nodemediatoreorjoin.CompleteAutomaticallyByForcedBranchesCount.error"));
            }

            //根据强制分支的数目和具体分支来完成增强合并节点
            //Enhance and merge nodes based on the number of mandatory branches and specific branches
            var forcedCount = forcedTransitionList.Where(t => t.TransitionId == transitionId).Count();
            if (forcedCount == 0)
            {
                //当前执行的分支不是强制分支，直接返回就可以
                //The currently executing branch is not a mandatory branch, simply return it directly
                result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.NotForcedBrancheWhenEOrJoin);
            }
            else if (forcedCount == 1)
            {
                result = CompleteAutomaticallyInternal(processInstance, transitionId, fromActivity, fromActivityInstance, tokensCountRequired, runner, kit);
            }
            return result;
        }

        /// <summary>
        /// Node completion method for EORJoin merging
        /// Internal execution logic
        /// EOrJoin合并时的节点完成方法
        /// 内部执行逻辑
        /// </summary>
        private NodeAutoExecutedResult CompleteAutomaticallyInternal(ProcessInstanceEntity processInstance,
            string transitionId,
            Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            int tokensCountRequired,
            WfAppRunner runner,
            SQLBuilder kit)
        {
            NodeAutoExecutedResult result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.Unknown);
            //当前执行的分支就是强制分支
            //检查是否有运行中的合并节点实例
            //The current executing branch is the forced branch
            //Check if there are any running merge node instances
            ActivityInstanceEntity joinNode = base.ActivityInstanceManager.GetActivityRunning(SailSqlAccess.Sibling(kit), 
                    processInstance.Id, base.GatewayActivity.ActivityId); ;

            if (joinNode == null)
            {
                //第一个分支首次运行
                //The first branch runs for the first time
                joinNode = base.CreateActivityInstanceObject(base.GatewayActivity,
                    processInstance, runner);

                //计算总需要的Token数目
                //Calculate the total number of tokens required
                joinNode.TokensRequired = tokensCountRequired;
                joinNode.TokensHad = 1;

                joinNode.ActivityState = (short)ActivityStateEnum.Running;
                joinNode.GatewayDirectionTypeId = (short)GatewayDirectionEnum.OrJoin;

                //写入默认第一次的预选步骤用户列表
                //Write the default user list for the first pre selection step
                joinNode.NextStepPerformers = NextStepUtility.SerializeNextStepPerformers(runner.NextActivityPerformers);

                base.InsertActivityInstance(joinNode,
                    kit);
                base.InsertTransitionInstance(processInstance,
                    transitionId,
                    fromActivityInstance,
                    joinNode,
                    TransitionTypeEnum.Forward,
                    TransitionFlyingTypeEnum.NotFlying,
                    runner,
                    kit);
            }
            else
            {
                joinNode.TokensHad += 1;
                base.GatewayActivityInstance = joinNode;

                //更新Token数目
                //Increase tokens count
                base.ActivityInstanceManager.IncreaseTokensHad(SailSqlAccess.Sibling(kit), base.GatewayActivityInstance.Id, runner);
                base.InsertTransitionInstance(processInstance,
                    transitionId,
                    fromActivityInstance,
                    joinNode,
                    TransitionTypeEnum.Forward,
                    TransitionFlyingTypeEnum.NotFlying,
                    runner,
                    kit);
            }

            //判断是否到达合并节点的通过Token数目要求
            //Determine whether the required number of tokens for merging nodes has been reached
            if (joinNode.TokensHad == joinNode.TokensRequired)
            {
                //如果达到完成节点的Token数，则设置该节点状态为完成
                //If the number of tokens for the completion node is reached, set the node status to complete
                base.CompleteActivityInstance(base.GatewayActivityInstance.Id,
                    runner,
                    kit);
                base.GatewayActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;

                //更新其它分支上的待办节点为阻止状态(CanRenewInstance = 0)
                //Update pending nodes on other branches to block status(CanRenewInstance = 0)
                base.ActivityInstanceManager.UpdateActivityInstanceBlockedBetweenSplitJoin(SailSqlAccess.Sibling(kit), base.GatewayActivity, base.GatewayActivityInstance, base.ProcessModel);

                result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.Successed);
            }
            else if (joinNode.TokensHad < joinNode.TokensRequired)
            {
                result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.WaitingForOthersJoin);
            }
            return result;
        }
    }
}
