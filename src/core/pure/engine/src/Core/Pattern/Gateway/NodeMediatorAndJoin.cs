using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Entity;
using saiflow.engine.Xpdl;
using saiflow.engine.Core;

namespace saiflow.engine.Pattern
{
    /// <summary>
    /// 与合并节点处理类
    /// </summary>
    internal class NodeMediatorAndJoin : NodeMediatorGateway, ICompleteGatewayAutomaticlly
    {
        internal NodeMediatorAndJoin(Activity activity, IProcessModel processModel, SQLBuilder kit, SailClient client)
            : base(activity, processModel, kit, client)
        {

        }

        /// <summary>
        /// 计算汇合Token数目
        /// </summary>
        /// <returns></returns>
        internal int GetTokensRequired()
        {
            int tokensRequired = this.ProcessModel.GetBackwardTransitionListCount(GatewayActivity.ActivityId);
            return tokensRequired;
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
            ActivityInstanceEntity joinNode = base.ActivityInstanceManager.GetActivityRunning(SailSqlAccess.Sibling(kit), processInstance.Id.Value, base.GatewayActivity.ActivityId);

            int tokensRequired = 0;
            int tokensHad = 0;

            if (joinNode == null)
            {
                var joinActivityInstance = base.CreateActivityInstanceObject(base.GatewayActivity, 
                    processInstance, runner);

                //计算所需令牌总数
                joinActivityInstance.TokensRequired = GetTokensRequired();
                joinActivityInstance.TokensHad = 1;
                tokensRequired = joinActivityInstance.TokensRequired.Value;

                joinActivityInstance.ActivityState = (short)ActivityStateEnum.Running;
                joinActivityInstance.GatewayDirectionTypeId = (short)GatewayDirectionEnum.AndJoin;

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

                //累加已到达令牌数
                base.ActivityInstanceManager.IncreaseTokensHad(SailSqlAccess.Sibling(kit), base.GatewayActivityInstance.Id.Value, runner);
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
