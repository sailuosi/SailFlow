using saiflow.engine.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Xpdl;
using saiflow.engine.Xpdl.Entity;

namespace saiflow.engine.Core.Pattern.Gateway
{
    /// <summary>
    /// Approval OrSplit Node Mediator
    /// ApprovalOrSplit 节点处理类
    /// 审批网关
    /// </summary>
    internal class NodeMediatorApprovalOrSplit : NodeMediatorGateway, ICompleteGatewayAutomaticlly
    {
        internal NodeMediatorApprovalOrSplit(Activity activity, IProcessModel processModel, SQLBuilder kit, SailClient client)
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
            var gatewayActivityInstance = base.CreateActivityInstanceObject(base.GatewayActivity, 
                processInstance, runner);
            gatewayActivityInstance.GatewayDirectionTypeId = (short)GatewayDirectionEnum.XOrSplit;

            base.InsertActivityInstance(gatewayActivityInstance,
                kit);

            base.CompleteActivityInstance(gatewayActivityInstance.Id,
                runner,
                kit);

            gatewayActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
            base.GatewayActivityInstance = gatewayActivityInstance;
            
            base.InsertTransitionInstance(processInstance,
                transitionId,
                fromActivityInstance,
                gatewayActivityInstance,
                TransitionTypeEnum.Forward,
                TransitionFlyingTypeEnum.NotFlying,
                runner,
                kit);

            NodeAutoExecutedResult result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.Successed);
            return result;
        }
    }
}
