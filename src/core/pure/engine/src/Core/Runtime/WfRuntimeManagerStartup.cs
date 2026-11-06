using saiflow.data;
using mooSQL.data;
using saiflow.engine.Manager;
using saiflow.engine.Result;
using saiflow.engine.Pattern;
using saiflow.engine.Core;

using saiflow.engine.Factories;
namespace saiflow.engine.Runtime
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
            var processInstance = Sail.ProcessInstances
                .CreateNewProcessInstanceObject(base.AppRunner, base.ProcessModel.ProcessEntity);

            var startActivity = base.ProcessModel.GetStartActivity();
            var startExecutionContext = ActivityForwardContext.CreateStartupContext(base.ProcessModel,
                processInstance,
                startActivity,
                base.ActivityResource);

            NodeMediator mediator = SailFactoryAccess.Mediator(Sail).CreateNodeMediator(startExecutionContext, kit);
            mediator.LinkContext.FromActivityInstance = RunningActivityInstance;
            mediator.ExecuteWorkItem(null);

            WfExecutedResult result = base.WfExecutedResult;
            result.ProcessInstanceIdStarted = processInstance.Id.Value;
            result.Status = WfExecutedStatus.Success;
        }
    }
}
