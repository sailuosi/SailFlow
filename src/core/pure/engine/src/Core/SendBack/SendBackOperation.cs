using System;
using saiflow.engine.Business.Entity;
using saiflow.engine.Common;
using saiflow.engine.Xpdl;
using saiflow.engine.Xpdl.Entity;

namespace saiflow.engine.Core.SendBack
{
    /// <summary>
    /// Sendback Operation Data
    /// </summary>
    internal class SendBackOperation
    {
        internal SendBackOperationTypeEnum CurrentNodeOperationType { get; set; }
        internal SendBackOperationTypeEnum CurrentMultipleInstanceDetailType { get; set; }
        internal SendBackOperationTypeEnum PreviousNodeOperationType { get; set; }
        internal BackwardTypeEnum BackwardType { get; set; }
        internal Boolean HasGatewayPassed { get; set; }
        internal ProcessInstanceEntity ProcessInstance { get; set; }
        internal ActivityInstanceEntity BackwardFromActivityInstance { get; set; }
        internal Activity BackwardToTaskActivity { get; set; }
        internal Performer BackwardToTaskPerformer { get; set; }
        internal ActivityResource ActivityResource { get; set; }
        internal IProcessModel ProcessModel { get; set; }
        internal Boolean IsCancellingBrothersNode { get; set; }
    }
}
