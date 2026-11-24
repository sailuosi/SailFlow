using System;
using System.Collections.Generic;
using System.Linq;
using sailflow.engine.Common;
using sailflow.data;
using mooSQL.data;
using sailflow.hub.Localize;
using sailflow.hub.Resource;
using sailflow.engine.Xpdl;
using sailflow.events;
using sailflow.run;
using sailflow.engine.Result;

using sailflow.engine.Client;
using sailflow.hooks;
using sailflow.engine.Factories;
namespace sailflow.run{
    /// <summary>
    /// 结束节点处理类
    /// </summary>
    internal class NodeMediatorEndMsgCatch : NodeMediator
    {
        internal NodeMediatorEndMsgCatch(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        /// <summary>
        /// 执行工作项
        /// </summary>
        public override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            OnBeforeExecuteWorkItem();

            //将流程实例设为完成
            var pim = Sail.ProcessInstances;
            var kit = this.Kit;
            var processInstance = pim.Complete(kit, ActivityForwardContext.ProcessInstance.ProcessInstanceId,
                ActivityForwardContext.ActivityResource.AppRunner);

            //如果当前流程是子流程，则子流程完成，主流程流转到下一节点
            //若当前为子流程，则完成子流程并推动主流程进入下一节点
            if (pim.IsSubProcess(processInstance) == true)
            {
                ContinueMainProcessRunning(processInstance, kit);
            }

            OnAfterExecuteWorkItem();
        }

        /// <summary>
        /// 继续执行主流程
        /// </summary>
        /// <param name="processInstance"></param>
        /// <param name="kit"></param>
        private void ContinueMainProcessRunning(ProcessInstanceEntity processInstance,
            SQLBuilder kit)
        {
            var runner = SubProcessMainContinueHelper.FillNextActivityPerformers(
                Sail,
                processInstance.InvokedActInstanceId,
                processInstance.InvokedActivityId,
                kit,
                processInstance.ProcessInstanceId);

            //开始执行下一步
            var runAppResult = WfExecutedResult.Default();
            var runtimeInstance = Sail.RuntimeFactory.CreateRuntimeInstanceAppRunning(runner, kit, ref runAppResult);
            if (runAppResult.Status == WfExecutedStatus.Exception)
            {
                throw new WfRuntimeException(Sail.Messages.GetEngineMessage("nodemediatorend.ContinueMainProcessRunning.warn", runAppResult.Message));
            }

            runtimeInstance.Execute(kit);
            runAppResult = runtimeInstance.WfExecutedResult;
            if (runAppResult.Status == WfExecutedStatus.Success)
            {
                var hookCtx = FlowCapabilityPipeline.BuildHookContext(
                    Sail, kit, runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.TaskId,
                    EventFireTypeEnum.OnProcessContinued,
                    FlowOperation.Run);
                FlowCapabilityPipeline.AfterSuccess(
                    Sail, kit, hookCtx, EventFireTypeEnum.OnProcessContinued);
            }
        }

        /// <summary>
        /// 结束节点活动及转移实例化，没有任务数据
        /// </summary>
        public override void CreateActivityTaskTransitionInstance(Activity toActivity,
            ProcessInstanceEntity processInstance,
            ActivityInstanceEntity fromActivityInstance,
            string transitionId,
            TransitionTypeEnum transitionType,
            TransitionFlyingTypeEnum flyingType,
            ActivityResource activityResource,
            SQLBuilder kit)
        {
            var toActivityInstance = CreateActivityInstanceObject(toActivity,
                processInstance, activityResource.AppRunner);

            IActivityInstanceManager.Insert(kit.useSQL(), toActivityInstance);

            IActivityInstanceManager.Complete(kit.useSQL(), toActivityInstance.ActivityInstanceId, activityResource.AppRunner);

            base.InsertTransitionInstance(processInstance,
                transitionId,
                fromActivityInstance,
                toActivityInstance,
                transitionType,
                flyingType,
                activityResource.AppRunner,
                kit);
        }
    }
}
