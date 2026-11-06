using saiflow.engine.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Entity;
using saiflow.engine.Xpdl;
using saiflow.engine.Core;

namespace saiflow.engine.Pattern
{
    /// <summary>
    /// OrJoin 节点处理类
    /// </summary>
    internal class NodeMediatorOrJoin : NodeMediatorGateway, ICompleteGatewayAutomaticlly
    {
        internal NodeMediatorOrJoin(Activity activity, IProcessModel processModel, SQLBuilder kit, SailClient client)
            : base(activity, processModel, kit, client)
        {

        }

        /// <summary>
        /// 计算需要的汇合Token数目
        /// </summary>
		internal int GetTokensRequired(int processInstanceId,
            Activity gatewayActivity,
            SQLBuilder kit)
        {
            int splitCount = 0;
            int joinCount = 0;
            Activity splitActivity = this.ProcessModel.GetBackwardGatewayActivity(gatewayActivity, ref joinCount, ref splitCount);
            ActivityInstanceEntity splitActivityInstance = base.ActivityInstanceManager.GetActivityInstanceLatest(kit.useSQL(), processInstanceId, splitActivity.ActivityId);

            return base.ActivityInstanceManager.GetGatewayInstanceCountByTransition(kit.useSQL(), splitActivity.ActivityId, splitActivityInstance.Id.Value, processInstanceId);
        }

        /// <summary>
        /// OrJoin合并时的节点完成方法
        /// 1. 如果有满足条件的OrJoin前驱转移，则会重新生成新的OrJoin节点实例
        /// </summary>
        public NodeAutoExecutedResult CompleteAutomaticlly(ProcessInstanceEntity processInstance,
            string transitionId,
            Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            WfAppRunner runner,
            SQLBuilder kit)
        {
            NodeAutoExecutedResult result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.Unknown);

            //检查是否已有运行中的合并节点实例
            ActivityInstanceEntity joinNode = base.ActivityInstanceManager.GetActivityRunning(kit.useSQL(), processInstance.Id.Value, base.GatewayActivity.ActivityId);

            if (joinNode == null)
            {
                joinNode = base.CreateActivityInstanceObject(base.GatewayActivity,
                    processInstance, runner);

                //计算所需令牌总数
                joinNode.TokensRequired = GetTokensRequired(processInstance.Id.Value, base.GatewayActivity, kit);
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
