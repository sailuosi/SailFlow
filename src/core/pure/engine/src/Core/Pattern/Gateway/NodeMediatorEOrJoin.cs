using saiflow.engine.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using saiflow.data;
using mooSQL.data;
using saiflow.hub.Localize;
using saiflow.engine.Common;
using saiflow.engine.Entity;
using saiflow.engine.Xpdl;
using saiflow.engine.Core;

namespace saiflow.engine.Pattern
{
    /// <summary>
    /// EOrJoin 节点处理类
    /// </summary>
    internal class NodeMediatorEOrJoin : NodeMediatorGateway, ICompleteGatewayAutomaticlly
    {
        internal NodeMediatorEOrJoin(Activity activity, IProcessModel processModel, SQLBuilder kit, SailClient client)
            : base(activity, processModel, kit, client)
        {

        }

        /// <summary>
        /// 自动完成
        /// </summary>
        public NodeAutoExecutedResult CompleteAutomaticlly(ProcessInstanceEntity processInstance,
            string transitionId,
            Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            FlowCommandContext runner,
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
        /// EOrJoin合并时的节点完成方法
        /// 前端动态传递要合并的Tokens的数目
        /// </summary>
        private NodeAutoExecutedResult CompleteAutomaticallyByTokensCount(ProcessInstanceEntity processInstance,
            string transitionId,
            Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            FlowCommandContext runner,
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
        /// EOrJoin合并时的节点完成方法
        /// 根据前置转移上定义的是否强制分支来判断完成合并节点
        /// </summary>
        private NodeAutoExecutedResult CompleteAutomaticallyByForcedBranchesCount(ProcessInstanceEntity processInstance,
            string transitionId,
            Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            FlowCommandContext runner,
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

            //按强制分支数与具体分支增强合并节点
            var forcedCount = forcedTransitionList.Where(t => t.TransitionId == transitionId).Count();
            if (forcedCount == 0)
            {
                //当前分支非强制分支，直接返回
                result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.NotForcedBrancheWhenEOrJoin);
            }
            else if (forcedCount == 1)
            {
                result = CompleteAutomaticallyInternal(processInstance, transitionId, fromActivity, fromActivityInstance, tokensCountRequired, runner, kit);
            }
            return result;
        }

        /// <summary>
        /// EOrJoin合并时的节点完成方法
        /// 内部执行逻辑
        /// </summary>
        private NodeAutoExecutedResult CompleteAutomaticallyInternal(ProcessInstanceEntity processInstance,
            string transitionId,
            Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            int tokensCountRequired,
            FlowCommandContext runner,
            SQLBuilder kit)
        {
            NodeAutoExecutedResult result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.Unknown);
            //当前执行的分支就是强制分支
            //检查是否有运行中的合并节点实例
            //当前为强制分支；检查是否已有运行中的合并节点实例
            ActivityInstanceEntity joinNode = base.ActivityInstanceManager.GetActivityRunning(kit.useSQL(), 
                    processInstance.Id.Value, base.GatewayActivity.ActivityId); ;

            if (joinNode == null)
            {
                //首个分支首次运行
                joinNode = base.CreateActivityInstanceObject(base.GatewayActivity,
                    processInstance, runner);

                //计算所需令牌总数
                joinNode.TokensRequired = tokensCountRequired;
                joinNode.TokensHad = 1;

                joinNode.ActivityState = (short)ActivityStateEnum.Running;
                joinNode.GatewayDirectionTypeId = (short)GatewayDirectionEnum.OrJoin;

                //写入首步预选默认用户列表
                joinNode.NextStepPerformers = NextStepUtility.SerializeNextStepPerformers(runner.NextActivityPerformers, Sail.JsonSerializer);

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

                //增加令牌计数
                base.ActivityInstanceManager.IncreaseTokensHad(kit.useSQL(), base.GatewayActivityInstance.Id.Value, runner);
                base.InsertTransitionInstance(processInstance,
                    transitionId,
                    fromActivityInstance,
                    joinNode,
                    TransitionTypeEnum.Forward,
                    TransitionFlyingTypeEnum.NotFlying,
                    runner,
                    kit);
            }

            //判断合并节点所需令牌数是否已达到
            if (joinNode.TokensHad == joinNode.TokensRequired)
            {
                //达到完成所需令牌数时，将节点状态设为完成
                base.CompleteActivityInstance(base.GatewayActivityInstance.Id.Value,
                    runner,
                    kit);
                base.GatewayActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;

                //将其余分支待办节点置为阻断（CanRenewInstance = 0）
                base.ActivityInstanceManager.UpdateActivityInstanceBlockedBetweenSplitJoin(kit.useSQL(), base.GatewayActivity, base.GatewayActivityInstance, base.ProcessModel);

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
