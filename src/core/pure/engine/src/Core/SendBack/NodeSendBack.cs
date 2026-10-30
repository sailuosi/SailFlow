using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using saiflow.data;
using mooSQL.data;
using saiflow.hub.Localize;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.engine.Common;
using saiflow.engine.Xpdl.Entity;
using saiflow.engine.Client;

namespace saiflow.engine.Core.SendBack
{
    /// <summary>
    /// Node Send Back
    /// 节点退回器
    /// </summary>
    internal abstract class NodeSendBack
    {
        #region Property and Constructor
        internal NodeSendBack(SendBackOperation sendbackOperation, SQLBuilder kit, SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
            _sendBackOperation = sendbackOperation;
            _kit = kit;
            _activityInstanceManager = new ActivityInstanceManager(Sail);
            _taskManager = new TaskManager(Sail);
            _transitionInstanceManager = new TransitionInstanceManager(Sail);
        }

        private SQLBuilder _kit;
        protected SQLBuilder Kit {  get { return _kit; } }

        private SendBackOperation _sendBackOperation;
        protected SendBackOperation SendBackOperation { get { return _sendBackOperation; } }

        protected SailClient Sail { get; }

        private ActivityInstanceManager _activityInstanceManager;
        protected ActivityInstanceManager ActivityInstanceManager { get { return _activityInstanceManager; } }

        private TaskManager _taskManager;
        protected TaskManager TaskManager { get { return _taskManager; } }

        private TransitionInstanceManager _transitionInstanceManager;
        protected TransitionInstanceManager TransitionInstanceManager {  get { return _transitionInstanceManager; } }
        #endregion

        #region Abstract Method
        internal abstract void Execute();
        #endregion

        /// <summary>
        /// Create Backward activity instance
        /// 创建退回类型的活动实例对象
        /// </summary>
        protected ActivityInstanceEntity CreateBackwardToActivityInstanceObject(ProcessInstanceEntity processInstance,
            BackwardTypeEnum backwardType,
            int backSrcActivityInstanceId,
            int backOrgActivityInstanceId,
            WfAppRunner runner)
        {
            ActivityInstanceEntity entity = ActivityInstanceManager.CreateBackwardActivityInstanceObject(
                processInstance.AppName,
                processInstance.AppInstanceId,
                processInstance.AppInstanceCode,
                processInstance.Id,
                processInstance.ProcessId,
                this.SendBackOperation.BackwardToTaskActivity,
                backwardType,
                backSrcActivityInstanceId,
                backOrgActivityInstanceId,
                runner);

            return entity;
        }

        /// <summary>
        /// Virtual method for creating tasks
        /// 1.  For automatically executed work items, there is no need to rewrite this method
        /// 2.  For manually executed work items, the method needs to be rewritten to insert pending task data
        /// 创建任务的虚方法
        /// 1. 对于自动执行的工作项，无需重写该方法
        /// 2. 对于人工执行的工作项，需要重写该方法，插入待办的任务数据
        /// </summary>
        internal virtual void CreateNewTask(ActivityInstanceEntity toActivityInstance,
            ActivityResource activityResource,
            SQLBuilder kit)
        {
            if (activityResource.NextActivityPerformers == null)
            {
                throw new WorkflowException(LocalizeHelper.GetEngineMessage("nodesendback.ExecuteInstanceImp.error"));
            }

            TaskManager.Insert(SailSqlAccess.Sibling(kit), toActivityInstance, activityResource.NextActivityPerformers[toActivityInstance.ActivityId], activityResource.AppRunner);
        }

        /// <summary>
        /// Insert transition instance
        /// 插入连线实例的方法
        /// </summary>
        internal virtual int InsertTransitionInstance(ProcessInstanceEntity processInstance,
            ActivityInstanceEntity fromActivityInstance,
            ActivityInstanceEntity toActivityInstance,
            TransitionTypeEnum transitionType,
            TransitionFlyingTypeEnum flyingType,
            WfAppRunner runner,
            SQLBuilder kit)
        {
            var transition = SendBackOperation.ProcessModel.GetForwardTransition(toActivityInstance.ActivityId,
                fromActivityInstance.ActivityId);
            var transitionId = transition != null ? transition.TransitionId : WfDefine.WF_XPDL_SEND_BACK_UNKNOWN_GUID;
            var transitionInstanceObject = this.TransitionInstanceManager.CreateTransitionInstanceObject(processInstance,
                transitionId,
                fromActivityInstance,
                toActivityInstance,
                transitionType,
                flyingType,
                runner,
                (byte)ConditionParseResultEnum.Passed);
            var newId = this.TransitionInstanceManager.Insert(SailSqlAccess.Sibling(kit), transitionInstanceObject);

            return newId;
        }

        /// <summary>
        /// Create backward activity task transition instance
        /// 创建退回时的流转节点对象、任务和转移数据
        /// </summary>
        internal void CreateBackwardActivityTaskTransitionInstance(ProcessInstanceEntity processInstance,
            ActivityInstanceEntity fromActivityInstance,
            Activity backwardToTaskActivity,
            BackwardTypeEnum backwardType,
            TransitionTypeEnum transitionType,
            TransitionFlyingTypeEnum flyingType,
            ActivityResource activityResource,
            SQLBuilder kit)
        {
            var previousActivityInstance = this.ActivityInstanceManager.GetPreviousActivityInstanceSimple(SailSqlAccess.Sibling(kit), fromActivityInstance, backwardToTaskActivity.ActivityId);

            if (previousActivityInstance == null)
            {
                throw new WorkflowException(LocalizeHelper.GetEngineMessage("nodesendback.CreateBackwardActivityTaskTransitionInstance.error"));
            }

            var toActivityInstance = this.ActivityInstanceManager.CreateBackwardActivityInstanceObject(
               processInstance.AppName,
               processInstance.AppInstanceId,
               processInstance.AppInstanceCode,
               processInstance.Id,
               processInstance.ProcessId,
               backwardToTaskActivity,
               backwardType,
               fromActivityInstance.Id,
               previousActivityInstance.Id,
               activityResource.AppRunner);

            toActivityInstance.ActivityState = (short)ActivityStateEnum.Ready;
            //人员来自步骤列表的用户数据
            //User data of personnel from the step list
            toActivityInstance.AssignedUserIds = SendBackOperation.BackwardToTaskPerformer.UserId;
            toActivityInstance.AssignedUserNames = SendBackOperation.BackwardToTaskPerformer.UserName;

            this.ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), toActivityInstance);

            this.TaskManager.Insert(SailSqlAccess.Sibling(kit), toActivityInstance, SendBackOperation.BackwardToTaskPerformer, activityResource.AppRunner);

            InsertTransitionInstance(processInstance,
                fromActivityInstance,
                toActivityInstance,
                TransitionTypeEnum.Backward,
                TransitionFlyingTypeEnum.NotFlying,
                this.SendBackOperation.ActivityResource.AppRunner,
                kit);
        }
    }
}
