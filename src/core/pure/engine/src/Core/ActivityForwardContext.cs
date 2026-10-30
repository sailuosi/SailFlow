using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.engine.Common;
using saiflow.engine.Xpdl;
using saiflow.engine.Xpdl.Entity;

namespace saiflow.engine.Core
{
    /// <summary>
    /// Activity node execution context object
    /// 活动节点执行上下文对象
    /// </summary>
    internal class ActivityForwardContext
    {
        #region ActivityForwardContext Property

        internal IProcessModel ProcessModel { get; set; }
        internal ProcessInstanceEntity ProcessInstance { get; set; }
        internal Activity CurrentActivity { get; set; }
        internal ActivityResource ActivityResource { get; set; }
        internal ActivityInstanceEntity FromActivityInstance { get; set; }
        internal Nullable<int> TaskId { get; set; }
        internal Boolean IsNotParsedByTransition { get; set; }

        #endregion

        #region ActivityForwardContext Constructor
        /// <summary>
        /// Context object on the starting node
        /// 开始节点上的上下文对象
        /// </summary>
        private ActivityForwardContext(IProcessModel processModel,
            ProcessInstanceEntity processInstance,
            Activity currentActivity,
            ActivityResource activityResource,
            Boolean isNotParsedByTransition = false)
        {
            ProcessModel = processModel;
            ProcessInstance = processInstance;
            CurrentActivity = currentActivity;
            ActivityResource = activityResource;
            IsNotParsedByTransition = isNotParsedByTransition;
        }

        /// <summary>
        /// Context object for task node execution
        /// 任务节点执行的上下文对象
        /// </summary>
        private ActivityForwardContext(TaskViewEntity taskView,
            IProcessModel processModel,
            ActivityResource activityResource,
            Boolean isNotParsedByTransition,
            SQLBuilder kit)
        {
            TaskId = taskView.Id;

            //check task condition has load activity instance
            FromActivityInstance = (new ActivityInstanceManager()).GetById(SailSqlAccess.Sibling(kit), taskView.ActivityInstanceId);
            ProcessInstance = (new ProcessInstanceManager()).GetById(kit, taskView.ProcessInstanceId);
            CurrentActivity = processModel.GetActivity(taskView.ActivityId);
            ProcessModel = processModel;
            ActivityResource = activityResource;
            IsNotParsedByTransition = isNotParsedByTransition;  
        }

        /// <summary>
        /// Interrupt event type activity instance execution context object
        /// Interrupt事件类型活动实例执行上下文对象
        /// </summary>
        private ActivityForwardContext(ActivityInstanceEntity activityInstance,
            IProcessModel processModel,
            ActivityResource activityResource,
            Boolean isNotParsedByTransition,
            SQLBuilder kit)
        {
            //check task condition has load activity instance
            FromActivityInstance = (new ActivityInstanceManager()).GetById(SailSqlAccess.Sibling(kit), activityInstance.Id);
            ProcessInstance = (new ProcessInstanceManager()).GetById(kit, activityInstance.ProcessInstanceId);
            CurrentActivity = processModel.GetActivity(activityInstance.ActivityId);
            ProcessModel = processModel;
            ActivityResource = activityResource;
            IsNotParsedByTransition = isNotParsedByTransition;
        }

        /// <summary>
        /// Context object for initiating the process
        /// 启动流程的上下文对象
        /// </summary>
        internal static ActivityForwardContext CreateStartupContext(IProcessModel processModel,
            ProcessInstanceEntity processInstance,
            Activity currentActivity,
            ActivityResource activityResource)
        {
            return new ActivityForwardContext(processModel, processInstance, currentActivity, activityResource, false);
        }

        /// <summary>
        /// Create task execution context object
        /// 创建任务执行上下文对象
        /// </summary>
        internal static ActivityForwardContext CreateRunningContextByTask(TaskViewEntity taskView,
            IProcessModel processModel,
            ActivityResource activityResource,
            Boolean isNotParsedForward,
            SQLBuilder kit)
        {
            return new ActivityForwardContext(taskView, processModel, activityResource, isNotParsedForward, kit);
        }

        /// <summary>
        /// Interrupt event type creates activity execution context object
        /// Interrupt事件类型创建活动执行上下文对象
        /// </summary>
        internal static ActivityForwardContext CreateRunningContextByActivity(ActivityInstanceEntity activityInstance,
            IProcessModel processModel,
            ActivityResource activityResource,
            Boolean isNotParsedForward,
            SQLBuilder kit)
        {
            return new ActivityForwardContext(activityInstance, processModel, activityResource, isNotParsedForward, kit);
        }

        /// <summary>
        /// Create process jump context object
        /// 创建流程跳转上下文对象
        /// </summary>
        internal static ActivityForwardContext CreateJumpforwardContext(Activity jumpforwardActivity,
            IProcessModel processModel,
            ProcessInstanceEntity processInstance,
            ActivityResource activityResource)
        {
            return new ActivityForwardContext(processModel, processInstance, jumpforwardActivity, activityResource, true);
        }
        #endregion
    }
}
