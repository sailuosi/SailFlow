using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.engine.Xpdl;
using saiflow.engine.Xpdl.Entity;

namespace saiflow.engine.Core.Pattern.Gateway
{
    /// <summary>
    /// Gatewayt Node Mediator
    /// 网关控制节点执行器
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

        private ActivityInstanceManager activityInstanceManager;
        internal ActivityInstanceManager ActivityInstanceManager
        {
            get
            {
                if (activityInstanceManager == null)
                {
                    activityInstanceManager = new ActivityInstanceManager();
                }
                return activityInstanceManager;
            }
        }
        
        internal NodeMediatorGateway(Activity gActivity, IProcessModel processModel, SQLBuilder kit)
        {
            _gatewayActivity = gActivity;
            _processModel = processModel;
            _kit = kit;
        }
        #endregion

        /// <summary>
        /// Create activity instance
        /// </summary>
        protected ActivityInstanceEntity CreateActivityInstanceObject(Activity activity,
            ProcessInstanceEntity processInstance,
            WfAppRunner runner)
        {
            ActivityInstanceManager aim = new ActivityInstanceManager();
            this.GatewayActivityInstance = aim.CreateActivityInstanceObject(processInstance.AppName,
                processInstance.AppInstanceId,
                processInstance.AppInstanceCode,
                processInstance.ProcessId,
                processInstance.Id,
                activity,
                runner);

            return this.GatewayActivityInstance;
        }

        /// <summary>
        /// Insert activity instance
        /// </summary>
        /// <param name="activityInstance"></param>
        /// <param name="kit"></param>
        internal virtual void InsertActivityInstance(ActivityInstanceEntity activityInstance,
            SQLBuilder kit)
        {
            ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), activityInstance);
        }

        /// <summary>
        /// Insert transition instance
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
            var tim = new TransitionInstanceManager();
            var transitionInstanceObject = tim.CreateTransitionInstanceObject(processInstance,
                transitionId,
                fromActivityInstance,
                toActivityInstance,
                transitionType,
                flyingType,
                runner,
                (byte)ConditionParseResultEnum.Passed);
            var newId = tim.Insert(SailSqlAccess.Sibling(kit), transitionInstanceObject);

            return newId;
        }

        /// <summary>
        /// Compelete activity instance
        /// </summary>
        internal virtual void CompleteActivityInstance(int ActivityInstanceId,
            WfAppRunner runner,
            SQLBuilder kit)
        {
            ActivityInstanceManager.Complete(SailSqlAccess.Sibling(kit), ActivityInstanceId, runner);
        }
    }
}
