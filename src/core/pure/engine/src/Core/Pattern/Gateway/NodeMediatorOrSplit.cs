using sailflow.engine.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sailflow.engine.Common;
using sailflow.data;
using mooSQL.data;
using sailflow.engine.Entity;
using sailflow.engine.Xpdl;
using sailflow.engine.Core;

namespace sailflow.engine.Pattern
{
    /// <summary>
    /// OrSplit 节点处理类
    /// </summary>
    internal class NodeMediatorOrSplit : NodeMediatorGateway, ICompleteGatewayAutomaticlly
    {
        internal NodeMediatorOrSplit(Activity activity, IProcessModel processModel, SQLBuilder kit, SailClient client)
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
            var gatewayActivityInstance = base.CreateActivityInstanceObject(base.GatewayActivity, processInstance, runner);
            gatewayActivityInstance.GatewayDirectionTypeId = (short)GatewayDirectionEnum.OrSplit;

            base.InsertActivityInstance(gatewayActivityInstance,
                kit);

            base.CompleteActivityInstance(gatewayActivityInstance.ActivityInstanceId,
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
