using saiflow.data;
using mooSQL.data;
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using saiflow.engine.Result;
using saiflow.engine.Pattern;
using saiflow.engine.Xpdl;
using saiflow.engine.Core;

using saiflow.engine.Factories;
namespace saiflow.engine.Runtime
{ 
    /// <summary>
    /// Runtime Manager Startup Sub Process
    /// 流程启动运行时
    /// </summary>
    internal class WfRuntimeManagerStartupSub : WfRuntimeManager
    {
        /// <summary>
        /// The processing logic of startup subprocess
        /// 启动执行方法
        /// </summary>
        /// <param name="kit"></param>
        internal override void ExecuteInstanceImp(SQLBuilder kit)
        {
            var subProcessNode = (SubProcessNode)base.InvokedSubProcessNode;
            var pim = Sail.ProcessInstances;
            var processInstance = pim.CreateNewProcessInstanceObject(base.AppRunner,
                base.ProcessModel.ProcessEntity,
                subProcessNode);

            var subStartactivity = base.ProcessModel.GetStartActivity();
            var startExecutionContext = ActivityForwardContext.CreateStartupContext(base.ProcessModel,
                processInstance,
                subStartactivity,
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
