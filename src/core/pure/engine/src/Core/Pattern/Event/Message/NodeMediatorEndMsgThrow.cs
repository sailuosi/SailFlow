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
using saiflow.engine.Core.Runtime;
using saiflow.engine.Core.Result;
using saiflow.engine.Core.Event;
using saiflow.engine.Event;
using saiflow.engine.Message;

using saiflow.engine.Client;
namespace saiflow.engine.Core.Pattern.Event.Message
{
    /// <summary>
    /// End node mediator message throw
    /// �����ڵ㴦����
    /// </summary>
    internal class NodeMediatorEndMsgThrow : NodeMediator
    {
        internal NodeMediatorEndMsgThrow(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        /// <summary>
        /// Execute work item
        /// </summary>
        internal override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            //ִ��ǰAction�б�
            //On before execute work item
            OnBeforeExecuteWorkItem();

            //�����������
            //Set process instance completed
            ProcessInstanceManager pim = new ProcessInstanceManager(Sail);
            var kit = SailSqlAccess.Sibling(Kit);
            var processInstance = pim.Complete(kit, ActivityForwardContext.ProcessInstance.Id.Value,
                ActivityForwardContext.ActivityResource.AppRunner);

            //ִ�нڵ��ϵ���Ϣ����
            //Publish messages on the execution node
            var msgDelegateService = new MessageDelegateService(Sail);
            msgDelegateService.PublishMessage(processInstance, LinkContext.CurrentActivity, ActivityForwardContext.FromActivityInstance);

            //�����ǰ�����������̣�����������ɣ���������ת����һ�ڵ�
            //If the current process is a subprocess,
            //then the subprocess is completed and the main process flows to the next node
            if (pim.IsSubProcess(processInstance) == true)
            {
                ContinueMainProcessRunning(processInstance, Kit);
            }

            //ִ�к�Action�б�
            //On after execute work item
            OnAfterExecuteWorkItem();
        }

        /// <summary>
        /// Continue to execute the main process
        /// ����ִ��������
        /// </summary>
        /// <param name="processInstance"></param>
        /// <param name="kit"></param>
        private void ContinueMainProcessRunning(ProcessInstanceEntity processInstance,
            SQLBuilder kit)
        {
            //��ȡ������һ��������Ա�б���Ϣ
            //Read the personnel list information for the next step in the process
            var runner = FillNextActivityPerformersByRoleList(processInstance.InvokedActivityInstanceId.Value,
                processInstance.InvokedActivityId,
                kit);

            //��ʼִ����һ��
            //Start executing the next step
            var runAppResult = WfExecutedResult.Default();
            var runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceAppRunning(Sail, runner, kit, ref runAppResult);
            if (runAppResult.Status == WfExecutedStatus.Exception)
            {
                throw new WfRuntimeException(Sail.Messages.GetEngineMessage("nodemediatorend.ContinueMainProcessRunning.warn", runAppResult.Message));
            }

            //ע���¼�������
            //Register the event and run it
            WfRuntimeManagerFactory.RegisterEvent(runtimeInstance,
                runtimeInstance_OnWfProcessRunning,
                runtimeInstance_OnWfProcessContinued);
            bool isRun = runtimeInstance.Execute(kit);

            void runtimeInstance_OnWfProcessRunning(object sender, WfEventArgs args)
            {
                saiflow.engine.Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                    EventFireTypeEnum.OnProcessRunning,
                    runner.EventSubscriptionList,
                    runtimeInstance.ProcessInstanceId);
            }

            void runtimeInstance_OnWfProcessContinued(object sender, WfEventArgs args)
            {
                runAppResult = args.WfExecutedResult;
                if (runAppResult.Status == WfExecutedStatus.Success)
                {
                    saiflow.engine.Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                        EventFireTypeEnum.OnProcessContinued,
                        runner.EventSubscriptionList,
                        runtimeInstance.ProcessInstanceId);
                }
            }
        }

        /// <summary>
        /// Add role users using process defined resources
        /// ʹ�����̶�����Դ���ӽ�ɫ�û�
        /// </summary>
        private WfAppRunner FillNextActivityPerformersByRoleList(int mainActivityInstanceId,
            string mainActivityId,
            SQLBuilder kit)
        {
            var pm = new ProcessInstanceManager(Sail);
            var mainProcessInstance = pm.GetByActivity(kit, mainActivityInstanceId);
            var processModel = ProcessModelFactory.CreateByProcessInstance(Sail, kit, mainProcessInstance);
            var nextSteps = processModel.GetNextActivityTreeView(mainActivityId);

            //��ȡ�����̵�����
            //The task of obtaining the mainstream process
            var task = new TaskManager(Sail).GetTaskByActivity(SailSqlAccess.Sibling(kit), mainProcessInstance.Id.Value, mainActivityInstanceId);
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
        /// End node activity and transfer instantiation, no task data available
        /// �����ڵ���ת��ʵ������û����������
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

            ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), toActivityInstance);

            ActivityInstanceManager.Complete(SailSqlAccess.Sibling(kit), toActivityInstance.Id.Value, activityResource.AppRunner);

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
