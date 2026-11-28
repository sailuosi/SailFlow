using sailflow.hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sailflow.utils;
using sailflow.data;
using mooSQL.data;
using sailflow.xml;
using sailflow.run;

namespace sailflow.run{
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
        internal int GetTokensRequired(string bizPK, string processInstanceId, Activity joinNode)
        {
            int joinCount = 0, splitCount = 0;
            var gatewayNode = base.ProcessModel.GetBackwardGatewayActivity(joinNode, ref joinCount, ref splitCount);

            var gatewayActivityInstance = base.IActivityInstanceManager.GetActivityInstanceLatest(Kit.useSQL(), processInstanceId, gatewayNode.ActivityId);
            var splitedActivityInstanceList = base.IActivityInstanceManager.GetValidSplitedActivityInstanceList(Kit.useSQL(), processInstanceId, gatewayActivityInstance.ActivityInstanceId);
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
            FlowCommandContext context,
            SQLBuilder kit)
        {
            //检查是否已有运行中的合并节点实例
            ActivityInstanceEntity joinNode = base.IActivityInstanceManager.GetActivityRunning(kit.useSQL(), processInstance.ProcessInstanceId, base.GatewayActivity.ActivityId);

            int tokensRequired = 0;
            int tokensHad = 0;

            if (joinNode == null)
            {
                var joinActivityInstance = base.CreateActivityInstanceObject(base.GatewayActivity,
                    processInstance, context);

                //计算所需令牌总数
                joinActivityInstance.TokensRequired = GetTokensRequired(processInstance.BizPK, processInstance.ProcessInstanceId, base.GatewayActivity);
                joinActivityInstance.TokensHad = 1;
                tokensRequired = joinActivityInstance.TokensRequired.Value;

                joinActivityInstance.ActivityState = (short)ActivityStateEnum.Running;
                joinActivityInstance.GateWayTypeId = (short)GatewayDirectionEnum.AndJoinMI;

                //写入首步预选默认用户列表
                joinActivityInstance.NextStepPerformers = NextStepUtility.SerializeNextStepPerformers(context.NextActivityPerformers, Sail.JsonSerializer);

                base.InsertActivityInstance(joinActivityInstance,
                    kit);
                base.InsertTransitionInstance(processInstance,
                    transitionId,
                    fromActivityInstance,
                    joinActivityInstance,
                    TransitionTypeEnum.Forward,
                    TransitionFlyingTypeEnum.NotFlying,
                    context,
                    kit);
            }
            else
            {
                base.GatewayActivityInstance = joinNode;
                tokensRequired = base.GatewayActivityInstance.TokensRequired.Value;
                tokensHad = base.GatewayActivityInstance.TokensHad.Value;

                //更新令牌计数
                base.IActivityInstanceManager.IncreaseTokensHad(kit.useSQL(), base.GatewayActivityInstance.ActivityInstanceId, context);
                base.InsertTransitionInstance(processInstance,
                    transitionId,
                    fromActivityInstance,
                    joinNode,
                    TransitionTypeEnum.Forward,
                    TransitionFlyingTypeEnum.NotFlying,
                    context,
                    kit);
            }
            if ((tokensHad + 1) == tokensRequired)
            {
                //如果达到完成节点的Token数，则设置该节点状态为完成
                //达到完成所需令牌数时，将节点状态设为完成
                base.CompleteActivityInstance(base.GatewayActivityInstance.ActivityInstanceId,
                    context,
                    kit);
                base.GatewayActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
            }

            NodeAutoExecutedResult result = NodeAutoExecutedResult.CreateGatewayExecutedResult(
                NodeAutoExecutedStatus.Successed);
            return result;
        }
    }
}
