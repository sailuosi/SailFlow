using saiflow.engine.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.engine.Xpdl;
using saiflow.engine.Xpdl.Entity;

namespace saiflow.engine.Core.Pattern.Gateway
{
    /// <summary>
    /// AndJoinMI Node Mediator
    /// AndJoinMI 节点处理类
    /// </summary>
    internal class NodeMediatorAndJoinMI : NodeMediatorGateway, ICompleteGatewayAutomaticlly
    {
        internal NodeMediatorAndJoinMI(Activity activity, IProcessModel processModel, SQLBuilder kit, SailClient client)
            : base(activity, processModel, kit, client)
        {

        }

        /// <summary>
        /// Obtain the required number of tokens
        /// 获取需要的Token数目
        /// </summary>
        internal int GetTokensRequired(string appInstanceId, int processInstanceId, Activity joinNode)
        {
            int joinCount = 0, splitCount = 0;
            var gatewayNode = base.ProcessModel.GetBackwardGatewayActivity(joinNode, ref joinCount, ref splitCount);

            var gatewayActivityInstance = base.ActivityInstanceManager.GetActivityInstanceLatest(SailSqlAccess.Sibling(Kit), processInstanceId, gatewayNode.ActivityId);
            var splitedActivityInstanceList = base.ActivityInstanceManager.GetValidSplitedActivityInstanceList(SailSqlAccess.Sibling(Kit), processInstanceId, gatewayActivityInstance.Id);
            int tokensCount = splitedActivityInstanceList.Count;

            return tokensCount;
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
            ActivityInstanceEntity joinNode = base.ActivityInstanceManager.GetActivityRunning(SailSqlAccess.Sibling(kit), processInstance.Id, base.GatewayActivity.ActivityId);

            int tokensRequired = 0;
            int tokensHad = 0;

            if (joinNode == null)
            {
                var joinActivityInstance = base.CreateActivityInstanceObject(base.GatewayActivity,
                    processInstance, runner);

                //计算总需要的Token数目
                //Calculate the total number of tokens required
                joinActivityInstance.TokensRequired = GetTokensRequired(processInstance.AppInstanceId, processInstance.Id, base.GatewayActivity);
                joinActivityInstance.TokensHad = 1;
                tokensRequired = joinActivityInstance.TokensRequired;

                joinActivityInstance.ActivityState = (short)ActivityStateEnum.Running;
                joinActivityInstance.GatewayDirectionTypeId = (short)GatewayDirectionEnum.AndJoinMI;

                //写入默认第一次的预选步骤用户列表
                //Write the default user list for the first pre selection step
                joinActivityInstance.NextStepPerformers = NextStepUtility.SerializeNextStepPerformers(runner.NextActivityPerformers);

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
                tokensRequired = base.GatewayActivityInstance.TokensRequired;
                tokensHad = base.GatewayActivityInstance.TokensHad;

                //更新Token数目
                //Update tokens count
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
            if ((tokensHad + 1) == tokensRequired)
            {
                //如果达到完成节点的Token数，则设置该节点状态为完成
                //If the number of tokens for the completion node is reached,
                //set the node status to complete
                base.CompleteActivityInstance(base.GatewayActivityInstance.Id,
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
