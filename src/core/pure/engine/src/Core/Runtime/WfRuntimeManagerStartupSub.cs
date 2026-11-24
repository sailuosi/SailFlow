using sailflow.data;
using mooSQL.data;
using sailflow.engine.Result;
using sailflow.run;
using sailflow.xml;

using sailflow.factory;
namespace sailflow.run{ 
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
            var processInstance = pim.CreateNewProcessInstanceObject(base.CmdContext,
                base.ProcessModel.ProcessEntity,
                subProcessNode,
                kit);

            var subStartactivity = base.ProcessModel.GetStartActivity();
            var startExecutionContext = ActivityForwardContext.CreateStartupContext(base.ProcessModel,
                processInstance,
                subStartactivity,
                base.ActivityResource);

            INodeMediator mediator = Sail.MediatorFactory.CreateNodeMediator(startExecutionContext, kit);
            mediator.LinkContext.FromActivityInstance = RunningActivityInstance;
            mediator.ExecuteWorkItem(null);

            WfExecutedResult result = base.WfExecutedResult;
            result.ProcessInstanceIdStarted = processInstance.ProcessInstanceId;
            result.Status = WfExecutedStatus.Success;
        }
    }
}
