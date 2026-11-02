using System;
using System.Collections.Generic;
using System.Linq;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.hub.Localize;
using saiflow.hub.Resource;
using saiflow.engine.Xpdl;
using saiflow.engine.Xpdl.Entity;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.engine.Event;
using saiflow.engine.Core.Runtime;
using saiflow.engine.Core.Result;
using saiflow.engine.Core.Event;

using saiflow.engine.Client;
namespace saiflow.engine.Core.Pattern.Event
{
    /// <summary>
    /// 结束节点处理类
    /// </summary>
    internal class NodeMediatorEnd : NodeMediator
    {
        internal NodeMediatorEnd(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
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

            //如果当前流程是子流程，则子流程完成，主流程流转到下一节点
            //若当前为子流程，则完成子流程并推动主流程进入下一节点
            if (pim.IsSubProcess(processInstance) == true)
            {
                ContinueMainProcessRunning(processInstance, this.Kit);
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
            var runner = FillNextActivityPerformersByRoleList(processInstance.InvokedActivityInstanceId.Value, 
                processInstance.InvokedActivityId, 
                kit);

            //继续下一步
            var runAppResult = WfExecutedResult.Default();
            var runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceAppRunning(Sail, runner, kit, ref runAppResult);
            if (runAppResult.Status == WfExecutedStatus.Exception)
            {
                throw new WfRuntimeException(Sail.Messages.GetEngineMessage("nodemediatorend.ContinueMainProcessRunning.warn", runAppResult.Message));
            }

            //注册并触发事件
            WfRuntimeManagerFactory.RegisterEvent(runtimeInstance,
                runtimeInstance_OnWfProcessRunning,
                runtimeInstance_OnWfProcessContinued);
            bool isRun = runtimeInstance.Execute(kit);

            void runtimeInstance_OnWfProcessRunning(object sender, WfEventArgs args)
            {
                EventExecutor.InvokeExternalEvent(Sail, kit,
                    EventFireTypeEnum.OnProcessRunning,
                    runner.EventSubscriptionList,
                    runtimeInstance.ProcessInstanceId);
            }

            void runtimeInstance_OnWfProcessContinued(object sender, WfEventArgs args)
            {
                runAppResult = args.WfExecutedResult;
                if (runAppResult.Status == WfExecutedStatus.Success)
                {
                    EventExecutor.InvokeExternalEvent(Sail, kit,
                        EventFireTypeEnum.OnProcessContinued,
                        runner.EventSubscriptionList,
                        runtimeInstance.ProcessInstanceId);
                }
            }
        }

        /// <summary>
        /// 使用流程定义资源添加角色用户
        /// </summary>
        private WfAppRunner FillNextActivityPerformersByRoleList(int mainActivityInstanceId,
            string mainActivityId,
            SQLBuilder kit)
        {
            var pm = new ProcessInstanceManager(Sail);
            var mainProcessInstance = pm.GetByActivity(kit, mainActivityInstanceId);
            var processModel = ProcessModelFactory.CreateByProcessInstance(Sail, kit, mainProcessInstance);
            var nextSteps = processModel.GetNextActivityTreeView(mainActivityId);

            //获取主流程任务
            var task = (new TaskManager(Sail)).GetTaskByActivity(SailSqlAccess.Sibling(kit), mainProcessInstance.Id.Value, mainActivityInstanceId);
            var runner = new WfAppRunner
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
                    //此处无模拟测试用户，返回异常信息
                    throw new WfRuntimeException(Sail.Messages.GetEngineMessage("nodemediatorend.FillNextActivityPerformersByRoleList.warn", node.ActivityName));
                }
                dict.Add(node.ActivityId, performerList);

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
            var toActivityInstance = base.CreateActivityInstanceObject(toActivity,
                processInstance, activityResource.AppRunner);

            base.ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), toActivityInstance);

            base.ActivityInstanceManager.Complete(SailSqlAccess.Sibling(kit), toActivityInstance.Id.Value, activityResource.AppRunner);

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
