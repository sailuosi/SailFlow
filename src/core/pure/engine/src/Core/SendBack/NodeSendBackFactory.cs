using saiflow.engine.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using saiflow.data;
using mooSQL.data;
using saiflow.hub.Localize;
using saiflow.engine.Common;

namespace saiflow.engine.Core.SendBack
{
    /// <summary>
    /// Create a sendback node processor
    /// 创建退回节点处理器
    /// </summary>
    internal class NodeSendBackFactory
    {
        /// <summary>
        /// Create method
        /// 创建方法
        /// </summary>
        /// <param name="sendbackOperation"></param>
        /// <param name="kit"></param>
        /// <returns></returns>
        internal static NodeSendBack CreateNodeReverter(SendBackOperation sendbackOperation, SQLBuilder kit, SailClient client)
        {
            NodeSendBack nodeSendBack = null;
            if (sendbackOperation.CurrentNodeOperationType == SendBackOperationTypeEnum.Normal)
            {
                if (sendbackOperation.PreviousNodeOperationType == SendBackOperationTypeEnum.Normal)
                {
                    nodeSendBack = new NodeSendBackTask(sendbackOperation, kit, client);
                }
                else if (sendbackOperation.PreviousNodeOperationType == SendBackOperationTypeEnum.MISPreviousIsLastOne)
                {
                    nodeSendBack = new NodeSendBackToMISPrevious(sendbackOperation, kit, client);
                }
                else if (sendbackOperation.PreviousNodeOperationType == SendBackOperationTypeEnum.MIPAllIsInCompletedState)
                {
                    nodeSendBack = new NodeSendBackToMIPPrevious(sendbackOperation, kit, client);
                }
                else
                {
                    //如果有其它模式，没有处理到，则直接抛出异常
                    //If there are other modes that cannot be handled, throw an exception directly
                    throw new WorkflowException(LocalizeMessagesLocator.Current.GetEngineMessage("nodesendbackfactory.CreateNodeReverter.error",
                        sendbackOperation.PreviousNodeOperationType.ToString()));
                }
            }
            else if (sendbackOperation.CurrentNodeOperationType == SendBackOperationTypeEnum.MultipleInstance)
            {
                if (sendbackOperation.CurrentMultipleInstanceDetailType == SendBackOperationTypeEnum.MISFirstOneIsRunning)
                {
                    nodeSendBack = new NodeSendBackMISReady(sendbackOperation, kit, client);
                }
                else if(sendbackOperation.CurrentMultipleInstanceDetailType == SendBackOperationTypeEnum.MISOneIsRunning)
                {
                    nodeSendBack = new NodeSendBackMIS(sendbackOperation, kit, client);
                }
                else if(sendbackOperation.CurrentMultipleInstanceDetailType == SendBackOperationTypeEnum.MIPOneIsRunning)
                {
                    nodeSendBack = new NodeSendBackMIP(sendbackOperation, kit, client);
                }
                else
                {
                    //如果有其它模式，没有处理到，则直接抛出异常
                    //If there are other modes that cannot be handled, throw an exception directly
                    throw new WorkflowException(LocalizeMessagesLocator.Current.GetEngineMessage("nodesendbackfactory.CreateNodeReverter.error",
                        sendbackOperation.CurrentMultipleInstanceDetailType.ToString()));
                }
            }
            return nodeSendBack;
        }
    }
}
