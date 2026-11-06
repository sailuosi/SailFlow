using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using saiflow.engine.Xpdl;
using saiflow.engine.Client;
using saiflow.engine.Core;

namespace saiflow.engine.Pattern
{
    /// <summary>
    /// B · 网关节点执行编排袋（L-Cmd）。对 A Managers 只读转发，不缓存字段。
    /// </summary>
    internal class NodeMediatorGateway
    {
        #region Property and constructor
        private Activity _gatewayActivity;
        internal Activity GatewayActivity
        {
            get { return _gatewayActivity; }
        }

        private IProcessModel _processModel;
        internal IProcessModel ProcessModel
        {
            get { return _processModel; }
        }

        private SQLBuilder _kit;
        internal SQLBuilder Kit
        {
            get { return _kit; }
        }

        internal ActivityInstanceEntity GatewayActivityInstance
        {
            get;
            set;
        }

        /// <summary>A · 经 ManagerFactory；不缓存字段。</summary>
        internal ActivityInstanceManager ActivityInstanceManager => Sail.ManagerFactory.Activities;
        
        protected SailClient Sail { get; }

        internal NodeMediatorGateway(Activity gActivity, IProcessModel processModel, SQLBuilder kit, SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
            _gatewayActivity = gActivity;
            _processModel = processModel;
            _kit = kit;
        }
        #endregion

        /// <summary>
        /// 创建活动实例
        /// </summary>
        protected ActivityInstanceEntity CreateActivityInstanceObject(Activity activity,
            ProcessInstanceEntity processInstance,
            WfAppRunner runner)
        {
            var aim = Sail.ManagerFactory.Activities;
            this.GatewayActivityInstance = aim.CreateActivityInstanceObject(processInstance.AppName,
                processInstance.AppInstanceId,
                processInstance.AppInstanceCode,
                processInstance.ProcessId,
                processInstance.Id.Value,
                activity,
                runner);

            return this.GatewayActivityInstance;
        }

        /// <summary>
        /// 插入活动实例
        /// </summary>
        /// <param name="activityInstance"></param>
        /// <param name="kit"></param>
        internal virtual void InsertActivityInstance(ActivityInstanceEntity activityInstance,
            SQLBuilder kit)
        {
            ActivityInstanceManager.Insert(kit.useSQL(), activityInstance);
        }

        /// <summary>
        /// 插入转移实例
        /// </summary>
        internal virtual int InsertTransitionInstance(ProcessInstanceEntity processInstance,
            string transitionId,
            ActivityInstanceEntity fromActivityInstance,
            ActivityInstanceEntity toActivityInstance,
            TransitionTypeEnum transitionType,
            TransitionFlyingTypeEnum flyingType,
            WfAppRunner runner,
            SQLBuilder kit)
        {
            var tim = Sail.ManagerFactory.Transitions;
            var transitionInstanceObject = tim.CreateTransitionInstanceObject(processInstance,
                transitionId,
                fromActivityInstance,
                toActivityInstance,
                transitionType,
                flyingType,
                runner,
                (byte)ConditionParseResultEnum.Passed);
            var newId = tim.Insert(kit.useSQL(), transitionInstanceObject);

            return newId;
        }

        /// <summary>
        /// 完成活动实例
        /// </summary>
        internal virtual void CompleteActivityInstance(int ActivityInstanceId,
            WfAppRunner runner,
            SQLBuilder kit)
        {
            ActivityInstanceManager.Complete(kit.useSQL(), ActivityInstanceId, runner);
        }
    }
}
