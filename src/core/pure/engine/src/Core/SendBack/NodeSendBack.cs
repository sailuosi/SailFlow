using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sailflow.data;
using mooSQL.data;
using sailflow.hub.Localize;
using sailflow.utils;
using sailflow.xml;
using sailflow.engine.Client;
using sailflow.run;

namespace sailflow.run{
    /// <summary>
    /// B · 退回节点执行编排袋（L-Cmd）。对 A Managers 只读转发，不缓存字段。
    /// </summary>
    internal abstract class NodeSendBack : INodeSendBack
    {
        #region Property and Constructor
        internal NodeSendBack(SendBackOperation sendbackOperation, SQLBuilder kit, SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
            _sendBackOperation = sendbackOperation;
            _kit = kit;
        }

        private SQLBuilder _kit;
        protected SQLBuilder Kit {  get { return _kit; } }

        private SendBackOperation _sendBackOperation;
        protected SendBackOperation SendBackOperation { get { return _sendBackOperation; } }

        protected SailClient Sail { get; }

        protected IActivityInstanceManager IActivityInstanceManager => Sail.ManagerFactory.Activities;

        protected ITaskManager ITaskManager => Sail.ManagerFactory.Tasks;

        protected ITransitionInstanceManager ITransitionInstanceManager => Sail.ManagerFactory.Transitions;
        #endregion

        #region Abstract Method
        public abstract void Execute();
        #endregion

        /// <summary>
        /// Create Backward activity instance
        /// 创建退回类型的活动实例对象
        /// </summary>
        protected ActivityInstanceEntity CreateBackwardToActivityInstanceObject(ProcessInstanceEntity processInstance,
            BackwardTypeEnum backwardType,
            string backSrcActivityInstanceId,
            string backOrgActivityInstanceId,
            FlowCommandContext runner)
        {
            ActivityInstanceEntity entity = IActivityInstanceManager.CreateBackwardActivityInstanceObject(
                processInstance.BizChannel,
                processInstance.BizPK,
                processInstance.BizCode,
                processInstance.ProcessInstanceId,
                processInstance.ProcessId,
                this.SendBackOperation.BackwardToTaskActivity,
                backwardType,
                backSrcActivityInstanceId,
                backOrgActivityInstanceId,
                runner);

            return entity;
        }


        /// <summary>
        /// Insert transition instance
        /// 插入连线实例的方法
        /// </summary>
        internal virtual string InsertTransitionInstance(ProcessInstanceEntity processInstance,
            ActivityInstanceEntity fromActivityInstance,
            ActivityInstanceEntity toActivityInstance,
            TransitionTypeEnum transitionType,
            TransitionFlyingTypeEnum flyingType,
            FlowCommandContext runner,
            SQLBuilder kit)
        {
            var transition = SendBackOperation.ProcessModel.GetForwardTransition(toActivityInstance.ActivityId,
                fromActivityInstance.ActivityId);
            var transitionId = transition != null ? transition.TransitionId : WfDefine.WF_XPDL_SEND_BACK_UNKNOWN_GUID;
            var transitionInstanceObject = this.ITransitionInstanceManager.CreateTransitionInstanceObject(processInstance,
                transitionId,
                fromActivityInstance,
                toActivityInstance,
                transitionType,
                flyingType,
                runner,
                (byte)ConditionParseResultEnum.Passed);
            var newId = this.ITransitionInstanceManager.Insert(kit.useSQL(), transitionInstanceObject);

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
            var previousActivityInstance = this.IActivityInstanceManager.GetPreviousActivityInstanceSimple(kit.useSQL(), fromActivityInstance, backwardToTaskActivity.ActivityId);

            if (previousActivityInstance == null)
            {
                throw new WorkflowException(Sail.Messages.GetEngineMessage("nodesendback.CreateBackwardActivityTaskTransitionInstance.error"));
            }

            var toActivityInstance = this.IActivityInstanceManager.CreateBackwardActivityInstanceObject(
               processInstance.BizChannel,
               processInstance.BizPK,
               processInstance.BizCode,
               processInstance.ProcessInstanceId,
               processInstance.ProcessId,
               backwardToTaskActivity,
               backwardType,
               fromActivityInstance.ActivityInstanceId,
               previousActivityInstance.ActivityInstanceId,
               activityResource.AppRunner);

            toActivityInstance.ActivityState = (short)ActivityStateEnum.Ready;
            //人员来自步骤列表的用户数据
            //User data of personnel from the step list
            toActivityInstance.SignIds = SendBackOperation.BackwardToTaskPerformer.UserId;
            toActivityInstance.SignNames = SendBackOperation.BackwardToTaskPerformer.UserName;

            this.IActivityInstanceManager.Insert(kit.useSQL(), toActivityInstance);

            this.ITaskManager.Insert(kit.useSQL(), toActivityInstance, SendBackOperation.BackwardToTaskPerformer, activityResource.AppRunner);

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
