
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;

using saiflow.engine.Client;
namespace saiflow.engine.Core.Pattern.Event.Signal
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

            ProcessInstanceManager pim = new ProcessInstanceManager(Sail);
            var kit = SailSqlAccess.Sibling(Kit);
            var processInstance = pim.Complete(kit, ActivityForwardContext.ProcessInstance.Id.Value,
                ActivityForwardContext.ActivityResource.AppRunner);

            OnAfterExecuteWorkItem();
        }
    }
}
