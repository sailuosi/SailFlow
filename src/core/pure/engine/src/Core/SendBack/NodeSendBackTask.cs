using sailflow.hosting;

using System.Linq;

using sailflow.data;
using mooSQL.data;
using sailflow.utils;
using sailflow.xml;


namespace sailflow.run{
    /// <summary>
    /// Sendback processing for task types
    /// 任务类型的退回处理器
    /// </summary>
    internal class NodeSendBackTask : NodeSendBack
    {
        internal NodeSendBackTask(SendBackOperation sendbackOperation, SQLBuilder kit, SailClient client) 
            : base(sendbackOperation, kit, client)
        {

        }

        /// <summary>
        /// Execute method
        /// </summary>
        public override void Execute()
        {
            if (base.SendBackOperation.IsCancellingBrothersNode == true)
            {
                //取消相邻并行分支上的其它并行节点
                //Cancel other parallel nodes on adjacent parallel branches
                CancelBrothersNode(base.SendBackOperation.BackwardToTaskActivity, 
                    base.SendBackOperation.BackwardFromActivityInstance,
                    base.SendBackOperation.ActivityResource.AppRunner,
                    base.Kit);
            }

            base.CreateBackwardActivityTaskTransitionInstance(base.SendBackOperation.ProcessInstance,
                base.SendBackOperation.BackwardFromActivityInstance,
                base.SendBackOperation.BackwardToTaskActivity,
                base.SendBackOperation.BackwardType,
                TransitionTypeEnum.Sendback,
                TransitionFlyingTypeEnum.NotFlying,
                base.SendBackOperation.ActivityResource,
                base.Kit);
        }

        /// <summary>
        /// Cancel adjacent branch nodes
        /// 取消相邻分支节点
        /// </summary>
        private void CancelBrothersNode(Activity toActivity,
            ActivityInstanceEntity fromActivityInstance,
            FlowCommandContext context,
            SQLBuilder kit)
        {
            var processModel = this.SendBackOperation.ProcessModel;
            var activityList = processModel.GetNextActivityListWithoutCondition(toActivity.ActivityId);
            var brothersList = activityList.Where(a => a.ActivityId != fromActivityInstance.ActivityId).ToList();

            foreach (var activity in brothersList)
            {
                var activityInstance = this.IActivityInstanceManager.GetActivityInstanceLatest(kit.useSQL(), fromActivityInstance.ProcessInstanceId, activity.ActivityId);
                if (activityInstance != null 
                    && this.IActivityInstanceManager.IncludeRunningState(activityInstance) == true)
                {
                    this.IActivityInstanceManager.Cancel(kit.useSQL(), activityInstance.ActivityInstanceId, context);
                }
            }
        }
    }
}
