using System;
using System.Collections.Generic;
using System.Linq;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.hub.Localize;
using saiflow.hub.Resource;
using saiflow.engine.Xpdl;
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using saiflow.engine.Event;
using saiflow.engine.Runtime;
using saiflow.engine.Result;
using saiflow.engine.Runtime;

using saiflow.engine.Client;
using saiflow.engine.Hooks;
using saiflow.engine.Core;
using saiflow.engine.Factories;
namespace saiflow.engine.Pattern
{
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
        internal override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            OnBeforeExecuteWorkItem();

            //将流程实例设为完成
            var pim = Sail.ProcessInstances;
            var kit = Kit.useSQL();
            var processInstance = pim.Complete(kit, ActivityForwardContext.ProcessInstance.Id,
                ActivityForwardContext.ActivityResource.AppRunner);

            //如果当前流程是子流程，则子流程完成，主流程流转到下一节点
            //若当前为子流程，则完成子流程并推动主流程进入下一节点
            if (pim.IsSubProcess(processInstance) == true)
            {
                ContinueMainProcessRunning(processInstance, Kit);
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
                    runtimeInstance.TaskView?.Id,
                    EventFireTypeEnum.OnProcessContinued,
                    FlowOperation.Run);
                FlowCapabilityPipeline.AfterSuccess(
                    Sail, kit, hookCtx, EventFireTypeEnum.OnProcessContinued);
            }
        }

        /// <summary>
        /// 使用流程定义资源添加角色用户
        /// </summary>
        private FlowCommandContext FillNextActivityPerformersByRoleList(string mainActivityInstanceId,
            string mainActivityId,
            SQLBuilder kit)
        {
            var pm = Sail.ProcessInstances;
            var mainProcessInstance = pm.GetByActivity(kit, mainActivityInstanceId);
            var processModel = Sail.ProcessModelFactory.CreateByProcessInstance(kit.useSQL(), mainProcessInstance);
            var nextSteps = processModel.GetNextActivityTreeView(mainActivityId);

            //获取主流程任务
            var task = Sail.ManagerFactory.Tasks.GetTaskByActivity(kit.useSQL(), mainProcessInstance.Id, mainActivityInstanceId);
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
        /// 结束节点活动及转移实例化，没有任务数据
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

            ActivityInstanceManager.Complete(kit.useSQL(), toActivityInstance.Id, activityResource.AppRunner);

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
