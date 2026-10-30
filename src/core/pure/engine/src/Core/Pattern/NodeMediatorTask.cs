using System;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Common;
using saiflow.engine.Event;
using saiflow.engine.Xpdl;
using saiflow.engine.Xpdl.Entity;
using saiflow.engine.Business.Entity;


namespace saiflow.engine.Core.Pattern
{
    /// <summary>
    /// Node Mediator Task
    /// ����ڵ�ִ����
    /// </summary>
    internal class NodeMediatorTask : NodeMediator
    {
        internal NodeMediatorTask(ActivityForwardContext forwardContext, SQLBuilder kit)
            : base(forwardContext, kit)
        {

        }

        internal NodeMediatorTask(SQLBuilder kit)
            : base(kit)
        {

        }

        /// <summary>
        /// Execute work item
        /// </summary>
        internal override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            try
            {
                OnBeforeExecuteWorkItem();

                bool canContinueForwardCurrentNode = CompleteWorkItem(ActivityForwardContext.TaskId,
                    ActivityForwardContext.ActivityResource,
                    this.Kit);

                OnAfterExecuteWorkItem();

                //��ȡ��һ���ڵ��б���������ִ��
                //Get the next node list: and continue execution
                if (canContinueForwardCurrentNode)
                {
                    ContinueForwardCurrentNode(ActivityForwardContext.IsNotParsedByTransition, this.Kit);
                }
            }
            catch (System.Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Complete work item
        /// ��ɽڵ�ʵ��
        /// </summary>   
        internal bool CompleteWorkItem(int? taskId,
            ActivityResource activityResource,
            SQLBuilder kit)
        {
            WfAppRunner runner = new WfAppRunner
            {
                UserId = activityResource.AppRunner.UserId,         
                UserName = activityResource.AppRunner.UserName
            };

            if (taskId != null)
            {
                //��ɱ����񣬷��������Ѿ�ת�Ƶ���һ����ǩ���񣬲�����ִ�������ڵ�
                //Complete this task, return that the task has been transferred to the next co signing task,
                //and do not continue to execute other nodes
                base.TaskManager.Complete(SailSqlAccess.Sibling(kit), taskId.Value, activityResource.AppRunner);
            }

            //���û�ڵ��״̬Ϊ���״̬
            //Set the status of the activity node to complete status
            base.ActivityInstanceManager.Complete(SailSqlAccess.Sibling(kit), base.LinkContext.FromActivityInstance.Id, activityResource.AppRunner);

            base.LinkContext.FromActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
            Boolean canContinueForwardCurrentNode = base.LinkContext.FromActivityInstance.CanNotRenewInstance == 0;

            return canContinueForwardCurrentNode;
        }

        /// <summary>
        /// Create activity task transition instance
        /// ���������ת��ʵ������
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
            Boolean isParallel = false;
            if (fromActivityInstance.ActivityType == (short)ActivityTypeEnum.GatewayNode)
            {
                //������ʵ����֧�ж�(AndSplit Multiple)
                //Concurrent multi instance branch judgment (AndSplit Multiple)
                var processModel = ProcessModelFactory.CreateByProcessInstance(SailSqlAccess.Sibling(kit), processInstance);
                var activityNode = processModel.GetActivity(fromActivityInstance.ActivityId);
                isParallel = processModel.IsAndSplitMI(activityNode);
            }

            if (isParallel)
            {
                //���ж�ʵ������
                //Parallel mutiple instance container
                ActivityInstanceEntity entity = null;
                var plist = activityResource.NextActivityPerformers[toActivity.ActivityId];

                //�������ж�ʵ����֧
                //Create parallel multi instance branches
                for (var i = 0; i < plist.Count; i++)
                {
                    entity = base.CreateActivityInstanceObject(toActivity, processInstance, activityResource.AppRunner);
                    entity.AssignedUserIds = plist[i].UserId;
                    entity.AssignedUserNames = plist[i].UserName;
                    entity.ActivityState = (short)ActivityStateEnum.Ready;

                    entity.Id = base.ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), entity);

                    base.TaskManager.Insert(SailSqlAccess.Sibling(kit), entity, plist[i], activityResource.AppRunner);

                    InsertTransitionInstance(processInstance,
                        transitionId,
                        fromActivityInstance,
                        entity,
                        transitionType,
                        flyingType,
                        activityResource.AppRunner,
                        kit);
                }
            }
            else
            {
                //��ͨ����ڵ�
                //Normal Task Node
                var toActivityInstance = base.CreateActivityInstanceObject(toActivity, processInstance, activityResource.AppRunner);

                //��������˻غ�ķ���
                //Handling returns after multiple returns
                WriteBackSrcOrgInformation(toActivityInstance, fromActivityInstance, kit);

                toActivityInstance.ActivityState = (short)ActivityStateEnum.Ready;
                toActivityInstance = GenerateActivityAssignedUserInfo(toActivityInstance, activityResource);
                
                base.ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), toActivityInstance);

                base.CreateNewTask(toActivityInstance, activityResource, kit);

                InsertTransitionInstance(processInstance,
                    transitionId,
                    fromActivityInstance,
                    toActivityInstance,
                    transitionType,
                    flyingType,
                    activityResource.AppRunner,
                    kit);

                //�����ⲿ�¼���ע�᷽��
                //Call the registration method for external events
                var EventContext = new EventContext
                {
                    AppInstanceId = processInstance.AppInstanceId,
                    ProcessId = processInstance.ProcessId,
                    ProcessInstanceId = processInstance.Id,
                    ActivityId = toActivity.ActivityId,
                    ActivityCode = toActivity.ActivityCode,
                    ActivityResource = activityResource
                };
                EventExecutor.InvokeExternalEvent(kit,
                    EventFireTypeEnum.OnActivityCreated,
                    activityResource.AppRunner.EventSubscriptionList,
                    EventContext);
            }
        }

        /// <summary>
        /// Maintain source node information for multiple returns
        /// ά������˻�ʱ��Դ�ڵ���Ϣ
        /// </summary>
        /// <param name="toActivityInstance"></param>
        /// <param name="fromActivityInstance"></param>
        /// <param name="kit"></param>
        private void WriteBackSrcOrgInformation(ActivityInstanceEntity toActivityInstance, 
            ActivityInstanceEntity fromActivityInstance,
            SQLBuilder kit)
        {
            if (fromActivityInstance.BackSrcActivityInstanceId != null)
            {
                var backSrcActivityInstance = base.ActivityInstanceManager.GetById(SailSqlAccess.Sibling(kit), fromActivityInstance.BackSrcActivityInstanceId.Value);
                toActivityInstance.BackSrcActivityInstanceId = backSrcActivityInstance.BackSrcActivityInstanceId;
                toActivityInstance.BackOrgActivityInstanceId = backSrcActivityInstance.BackOrgActivityInstanceId;
            }
        }
    }
}
