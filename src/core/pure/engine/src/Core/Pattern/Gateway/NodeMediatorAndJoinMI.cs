using saiflow.engine.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using saiflow.engine.Xpdl;
using saiflow.engine.Core;

namespace saiflow.engine.Pattern
{
    /// <summary>
    /// AndJoinMI 节点处理类
    /// </summary>
    internal class NodeMediatorAndJoinMI : NodeMediatorGateway, ICompleteGatewayAutomaticlly
    {
        internal NodeMediatorAndJoinMI(Activity activity, IProcessModel processModel, SQLBuilder kit, SailClient client)
            : base(activity, processModel, kit, client)
        {

        }

        /// <summary>
        /// 获取需要的Token数目
        /// </summary>
        internal int GetTokensRequired(string appInstanceId, int processInstanceId, Activity joinNode)
        {
            int joinCount = 0, splitCount = 0;
            var gatewayNode = base.ProcessModel.GetBackwardGatewayActivity(joinNode, ref joinCount, ref splitCount);

            var gatewayActivityInstance = base.ActivityInstanceManager.GetActivityInstanceLatest(Kit.useSQL(), processInstanceId, gatewayNode.ActivityId);
            var splitedActivityInstanceList = base.ActivityInstanceManager.GetValidSplitedActivityInstanceList(Kit.useSQL(), processInstanceId, gatewayActivityInstance.Id.Value);
            int tokensCount = splitedActivityInstanceList.Count;

            return tokensCount;
        }

        /// <summary>
        /// 自动完成
        /// </summary>
        public NodeAutoExecutedResult CompleteAutomaticlly(ProcessInstanceEntity processInstance,
            string transitionId,
            Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            WfAppRunner runner,
            SQLBuilder kit)
        {
            //检查是否已有运行中的合并节点实例
            ActivityInstanceEntity joinNode = base.ActivityInstanceManager.GetActivityRunning(kit.useSQL(), processInstance.Id.Value, base.GatewayActivity.ActivityId);

            int tokensRequired = 0;
            int tokensHad = 0;

            if (joinNode == null)
            {
                var joinActivityInstance = base.CreateActivityInstanceObject(base.GatewayActivity,
                    processInstance, runner);

                //计算所需令牌总数
                joinActivityInstance.TokensRequired = GetTokensRequired(processInstance.AppInstanceId, processInstance.Id.Value, base.GatewayActivity);
                joinActivityInstance.TokensHad = 1;
                tokensRequired = joinActivityInstance.TokensRequired.Value;

                joinActivityInstance.ActivityState = (short)ActivityStateEnum.Running;
                joinActivityInstance.GatewayDirectionTypeId = (short)GatewayDirectionEnum.AndJoinMI;

                //写入首步预选默认用户列表
                joinActivityInstance.NextStepPerformers = NextStepUtility.SerializeNextStepPerformers(runner.NextActivityPerformers, Sail.JsonSerializer);

                base.InsertActivityInstance(joinActivityInstance,
                    kit);
                base.InsertTransitionInstance(processInstance,
                    transitionId,
                    fromActivityInstance,
                    joinActivityInstance,
                    TransitionTypeEnum.Forward,
                    TransitionFlyingTypeEnum.NotFlying,
                    runner,
                    kit);
            }
            else
            {
                base.GatewayActivityInstance = joinNode;
                tokensRequired = base.GatewayActivityInstance.TokensRequired.Value;
                tokensHad = base.GatewayActivityInstance.TokensHad.Value;

                //更新令牌计数
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
            if ((tokensHad + 1) == tokensRequired)
            {
                //如果达到完成节点的Token数，则设置该节点状态为完成
                //达到完成所需令牌数时，将节点状态设为完成
                base.CompleteActivityInstance(base.GatewayActivityInstance.Id.Value,
                    runner,
                    kit);
                base.GatewayActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
            }

            NodeAutoExecutedResult result = NodeAutoExecutedResult.CreateGatewayExecutedResult(
                NodeAutoExecutedStatus.Successed);
            return result;
        }
    }
}
