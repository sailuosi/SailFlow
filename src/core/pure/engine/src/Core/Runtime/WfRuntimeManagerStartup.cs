using saiflow.data;
using mooSQL.data;
using saiflow.engine.Business.Manager;
using saiflow.engine.Core.Result;
using saiflow.engine.Core.Pattern;

namespace saiflow.engine.Core.Runtime
{
    /// <summary>
    /// Runtime Manager Startup
    /// 流程启动运行时
    /// </summary>
    internal class WfRuntimeManagerStartup : WfRuntimeManager
    {
        /// <summary>
        /// The processing logic of startup
        /// 启动执行方法
        /// </summary>
        /// <param name="kit"></param>
        internal override void ExecuteInstanceImp(SQLBuilder kit)
        {
            var processInstance = new ProcessInstanceManager(Sail)
                .CreateNewProcessInstanceObject(base.AppRunner, base.ProcessModel.ProcessEntity);

            var startActivity = base.ProcessModel.GetStartActivity();
            var startExecutionContext = ActivityForwardContext.CreateStartupContext(base.ProcessModel,
                processInstance,
                startActivity,
                base.ActivityResource);

            NodeMediator mediator = NodeMediatorFactory.CreateNodeMediator(startExecutionContext, kit, Sail);
            mediator.LinkContext.FromActivityInstance = RunningActivityInstance;
            mediator.ExecuteWorkItem(null);

            WfExecutedResult result = base.WfExecutedResult;
            result.ProcessInstanceIdStarted = processInstance.Id.Value;
            result.Status = WfExecutedStatus.Success;
        }
    }
}
