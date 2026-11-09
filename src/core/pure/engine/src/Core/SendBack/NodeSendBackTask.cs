using saiflow.engine.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Common;
using saiflow.engine.Entity;
using saiflow.engine.Xpdl;
using saiflow.engine.Core;

namespace saiflow.engine.Runtime
{
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
        internal override void Execute()
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
            FlowCommandContext runner,
            SQLBuilder kit)
        {
            var processModel = this.SendBackOperation.ProcessModel;
            var activityList = processModel.GetNextActivityListWithoutCondition(toActivity.ActivityId);
            var brothersList = activityList.Where(a => a.ActivityId != fromActivityInstance.ActivityId).ToList();

            foreach (var activity in brothersList)
            {
                var activityInstance = this.ActivityInstanceManager.GetActivityInstanceLatest(kit.useSQL(), fromActivityInstance.ProcessInstanceId, activity.ActivityId);
                if (activityInstance != null 
                    && this.ActivityInstanceManager.IncludeRunningState(activityInstance) == true)
                {
                    this.ActivityInstanceManager.Cancel(kit.useSQL(), activityInstance.Id, runner);
                }
            }
        }
    }
}
