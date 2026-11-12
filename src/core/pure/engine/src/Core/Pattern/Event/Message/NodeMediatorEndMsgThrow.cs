using System;
using System.Collections.Generic;
using System.Linq;
using sailflow.engine.Common;
using sailflow.data;
using mooSQL.data;
using sailflow.hub.Localize;
using sailflow.hub.Resource;
using sailflow.engine.Xpdl;
using sailflow.engine.Entity;
using sailflow.engine.Manager;
using sailflow.engine.Runtime;
using sailflow.engine.Result;
using sailflow.engine.Runtime;
using sailflow.engine.Event;
using sailflow.engine.Message;

using sailflow.engine.Client;
using sailflow.engine.Hooks;
using sailflow.engine.Core;
using sailflow.engine.Factories;
namespace sailflow.engine.Pattern
{
    /// <summary>
    /// 结束消息抛出节点执行器
    /// </summary>
    internal class NodeMediatorEndMsgThrow : NodeMediator
    {
        internal NodeMediatorEndMsgThrow(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        /// <summary>
        /// 执行工作项
        /// </summary>
        internal override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            //工作项执行前
            OnBeforeExecuteWorkItem();

            //将流程实例设为完成
            var pim = Sail.ProcessInstances;
            var kit = Kit.useSQL();
            var processInstance = pim.Complete(kit, ActivityForwardContext.ProcessInstance.ProcessInstanceId,
                ActivityForwardContext.ActivityResource.AppRunner);

            //在执行节点发布消息
            var msgDelegateService = new MessageDelegateService(Sail);
            msgDelegateService.PublishMessage(processInstance, LinkContext.CurrentActivity, ActivityForwardContext.FromActivityInstance);

            //若当前为子流程，则完成子流程并推动主流程进入下一节点
            if (pim.IsSubProcess(processInstance) == true)
            {
                ContinueMainProcessRunning(processInstance, Kit);
            }

            //工作项执行后
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
            //读取流程下一步人员列表
            var runner = FillNextActivityPerformersByRoleList(processInstance.InvokedActivityInstanceId,
                processInstance.InvokedActivityId,
                kit);

            //开始执行下一步
            var runAppResult = WfExecutedResult.Default();
            var runtimeInstance = SailFactoryAccess.Runtime(Sail).CreateRuntimeInstanceAppRunning(runner, kit, ref runAppResult);
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
        /// 按流程定义资源添加角色用户
        /// </summary>
        private FlowCommandContext FillNextActivityPerformersByRoleList(string mainActivityInstanceId,
            string mainActivityId,
            SQLBuilder kit)
        {
            var pm = Sail.ProcessInstances;
            var mainProcessInstance = pm.GetByActivity(kit, mainActivityInstanceId);
            var processModel = Sail.ProcessModelFactory.CreateByProcessInstance(kit, mainProcessInstance);
            var nextSteps = processModel.GetNextActivityTreeView(mainActivityId);

            //获取主流程任务
            var task = Sail.ManagerFactory.Tasks.GetTaskByActivity(kit.useSQL(), mainProcessInstance.ProcessInstanceId, mainActivityInstanceId);
            var runner = new FlowCommandContext
            {
                AppName = mainProcessInstance.AppName,
                AppInstanceId = mainProcessInstance.AppInstanceId,
                AppInstanceCode = mainProcessInstance.AppInstanceCode,
                ProcessId = mainProcessInstance.ProcessId,
                Version = mainProcessInstance.Version,
                UserId = task.AssignedUserId,
                UserName = task.AssignedUserName
            };

            foreach (var node in nextSteps)
            {
                Dictionary<string, PerformerList> dict = new Dictionary<string, PerformerList>();
                var performerList = PerformerBuilder.CreatePerformerList(node.Roles);      
                if (node.ActivityType != ActivityTypeEnum.EndNode
                    && performerList.Count == 0)
                {
                    throw new WfRuntimeException(Sail.Messages.GetEngineMessage("nodemediatorend.FillNextActivityPerformersByRoleList.warn", node.ActivityName));
                }
                else
                {
                    dict.Add(node.ActivityId, performerList);
                }
                runner.NextActivityPerformers = dict;
            }
            return runner;
        }

        /// <summary>
        /// 结束节点活动与转移实例化（无任务数据）
        /// </summary>
        internal override void CreateActivityTaskTransitionInstance(Activity toActivity,
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

            ActivityInstanceManager.Insert(kit.useSQL(), toActivityInstance);

            ActivityInstanceManager.Complete(kit.useSQL(), toActivityInstance.ActivityInstanceId, activityResource.AppRunner);

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
