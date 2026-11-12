
using sailflow.data;
using mooSQL.data;
using sailflow.engine.Entity;
using sailflow.engine.Manager;

using sailflow.engine.Client;
using sailflow.engine.Core;
namespace sailflow.engine.Pattern
{
    /// <summary>
    /// 信号结束节点的Catch类
    /// </summary>
    internal class NodeMediatorEndSignalCatch : NodeMediator
    {
        internal NodeMediatorEndSignalCatch(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        /// <summary>
        /// 执行工作项
        /// </summary>
        internal override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            OnBeforeExecuteWorkItem();

            var pim = Sail.ProcessInstances;
            var kit = Kit.useSQL();
            var processInstance = pim.Complete(kit, ActivityForwardContext.ProcessInstance.ProcessInstanceId,
                ActivityForwardContext.ActivityResource.AppRunner);

            OnAfterExecuteWorkItem();
        }
    }
}
