using System;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.engine.Xpdl;

namespace saiflow.engine.Core
{
    /// <summary>
    /// Activity node execution context object
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
        internal SailClient Sail { get; set; }

        #endregion

        #region ActivityForwardContext Constructor
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

        private ActivityForwardContext(TaskViewEntity taskView,
            IProcessModel processModel,
            ActivityResource activityResource,
            Boolean isNotParsedByTransition,
            SQLBuilder kit,
            SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
            TaskId = taskView.Id;

            FromActivityInstance = Sail.ManagerFactory.Activities.GetById(kit.useSQL(), taskView.ActivityInstanceId.Value);
            ProcessInstance = Sail.ProcessInstances.GetById(kit, taskView.ProcessInstanceId.Value);
            CurrentActivity = processModel.GetActivity(taskView.ActivityId);
            ProcessModel = processModel;
            ActivityResource = activityResource;
            IsNotParsedByTransition = isNotParsedByTransition;
        }

        private ActivityForwardContext(ActivityInstanceEntity activityInstance,
            IProcessModel processModel,
            ActivityResource activityResource,
            Boolean isNotParsedByTransition,
            SQLBuilder kit,
            SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));

            FromActivityInstance = Sail.ManagerFactory.Activities.GetById(kit.useSQL(), activityInstance.Id.Value);
            ProcessInstance = Sail.ProcessInstances.GetById(kit, activityInstance.ProcessInstanceId.Value);
            CurrentActivity = processModel.GetActivity(activityInstance.ActivityId);
            ProcessModel = processModel;
            ActivityResource = activityResource;
            IsNotParsedByTransition = isNotParsedByTransition;
        }

        internal static ActivityForwardContext CreateStartupContext(IProcessModel processModel,
            ProcessInstanceEntity processInstance,
            Activity currentActivity,
            ActivityResource activityResource)
        {
            return new ActivityForwardContext(processModel, processInstance, currentActivity, activityResource, false);
        }

        internal static ActivityForwardContext CreateRunningContextByTask(TaskViewEntity taskView,
            IProcessModel processModel,
            ActivityResource activityResource,
            Boolean isNotParsedForward,
            SQLBuilder kit,
            SailClient client)
        {
            return new ActivityForwardContext(taskView, processModel, activityResource, isNotParsedForward, kit, client);
        }

        internal static ActivityForwardContext CreateRunningContextByActivity(ActivityInstanceEntity activityInstance,
            IProcessModel processModel,
            ActivityResource activityResource,
            Boolean isNotParsedForward,
            SQLBuilder kit,
            SailClient client)
        {
            return new ActivityForwardContext(activityInstance, processModel, activityResource, isNotParsedForward, kit, client);
        }

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
