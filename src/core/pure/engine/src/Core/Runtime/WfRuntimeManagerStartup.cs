using sailflow.data;
using mooSQL.data;
using sailflow.engine.Manager;
using sailflow.engine.Result;
using sailflow.engine.Pattern;
using sailflow.engine.Core;

using sailflow.engine.Factories;
namespace sailflow.engine.Runtime
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
                .CreateNewProcessInstanceObject(base.CmdContext, base.ProcessModel.ProcessEntity, kit: kit);

            var startActivity = base.ProcessModel.GetStartActivity();
            var startExecutionContext = ActivityForwardContext.CreateStartupContext(base.ProcessModel,
                processInstance,
                startActivity,
                base.ActivityResource);

            NodeMediator mediator = SailFactoryAccess.Mediator(Sail).CreateNodeMediator(startExecutionContext, kit);
            mediator.LinkContext.FromActivityInstance = RunningActivityInstance;
            mediator.ExecuteWorkItem(null);

            WfExecutedResult result = base.WfExecutedResult;
            result.ProcessInstanceIdStarted = processInstance.ProcessInstanceId;
            result.Status = WfExecutedStatus.Success;
        }
    }
}
