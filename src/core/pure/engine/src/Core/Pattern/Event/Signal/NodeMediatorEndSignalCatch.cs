
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Entity;
using saiflow.engine.Manager;

using saiflow.engine.Client;
using saiflow.engine.Core;
namespace saiflow.engine.Pattern
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
            var processInstance = pim.Complete(kit, ActivityForwardContext.ProcessInstance.Id.Value,
                ActivityForwardContext.ActivityResource.AppRunner);

            OnAfterExecuteWorkItem();
        }
    }
}
