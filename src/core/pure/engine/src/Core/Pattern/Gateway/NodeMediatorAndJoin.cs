using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Xpdl;
using saiflow.engine.Xpdl.Entity;

namespace saiflow.engine.Core.Pattern.Gateway
{
    /// <summary>
    /// AndJoin Node Mediator
    /// 与合并节点处理类
    /// </summary>
    internal class NodeMediatorAndJoin : NodeMediatorGateway, ICompleteGatewayAutomaticlly
    {
        internal NodeMediatorAndJoin(Activity activity, IProcessModel processModel, SQLBuilder kit, SailClient client)
            : base(activity, processModel, kit, client)
        {

        }

        /// <summary>
        /// Calculate the number of converged tokens
        /// 计算汇合Token数目
        /// </summary>
        /// <returns></returns>
        internal int GetTokensRequired()
        {
            int tokensRequired = this.ProcessModel.GetBackwardTransitionListCount(GatewayActivity.ActivityId);
            return tokensRequired;
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
            //检查是否有运行中的合并节点实例
            //Check if there are any running merge node instances
            ActivityInstanceEntity joinNode = base.ActivityInstanceManager.GetActivityRunning(SailSqlAccess.Sibling(kit), processInstance.Id.Value, base.GatewayActivity.ActivityId);

            int tokensRequired = 0;
            int tokensHad = 0;

            if (joinNode == null)
            {
                var joinActivityInstance = base.CreateActivityInstanceObject(base.GatewayActivity, 
                    processInstance, runner);

                //计算总需要的Token数目
                //Calculate the total number of tokens required
                joinActivityInstance.TokensRequired = GetTokensRequired();
                joinActivityInstance.TokensHad = 1;
                tokensRequired = joinActivityInstance.TokensRequired.Value;

                joinActivityInstance.ActivityState = (short)ActivityStateEnum.Running;
                joinActivityInstance.GatewayDirectionTypeId = (short)GatewayDirectionEnum.AndJoin;

                //写入默认第一次的预选步骤用户列表
                //Write the default user list for the first pre selection step
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

                //更新Token数目
                //Increase the tokens had
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
                //如果达到完成节点的Token数，则设置该节点状态为完成
                //If the number of tokens for the completion node is reached, set the node status to complete
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
