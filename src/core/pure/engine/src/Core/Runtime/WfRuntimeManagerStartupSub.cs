using saiflow.data;
using mooSQL.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.engine.Core.Result;
using saiflow.engine.Core.Pattern;
using saiflow.engine.Xpdl.Common;
using saiflow.engine.Xpdl.Entity;
using saiflow.engine.Xpdl.Node;

namespace saiflow.engine.Core.Runtime
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
            var pim = new ProcessInstanceManager(Sail);
            var processInstance = pim.CreateNewProcessInstanceObject(base.AppRunner,
                base.ProcessModel.ProcessEntity,
                subProcessNode);

            var subStartactivity = base.ProcessModel.GetStartActivity();
            var startExecutionContext = ActivityForwardContext.CreateStartupContext(base.ProcessModel,
                processInstance,
                subStartactivity,
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
