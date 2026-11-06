using saiflow.data;
using mooSQL.data;
using saiflow.engine.Manager;
using saiflow.engine.Common;
using saiflow.engine.Result;
using saiflow.engine.Pattern;
using saiflow.engine.Core;

namespace saiflow.engine.Runtime
{
    /// <summary>
    /// Handling of reject methods 
    /// 驳回处理
    /// </summary>
    internal class WfRuntimeManagerReject : WfRuntimeManager
    {
        /// <summary>
        /// Reject Execute Method
        /// 驳回执行方法
        /// </summary>
        /// <param name="kit"></param>
        internal override void ExecuteInstanceImp(SQLBuilder kit)
        {
            WfExecutedResult result = base.WfExecutedResult;

            var tm = Sail.ManagerFactory.Tasks;
            tm.Complete(SailSqlAccess.Sibling(kit), this.TaskView.Id.Value, base.AppRunner);

            //取消当前其它处于办理状态的节点
            //Cancel other nodes currently in processing status
            var aim = Sail.ManagerFactory.Activities;
            var runningList = aim.GetRunningActivityInstanceList(SailSqlAccess.Sibling(kit), base.AppRunner.AppInstanceId, base.AppRunner.ProcessId, base.AppRunner.Version);
            foreach (var ai in runningList)
            {
                if (ai.Id != base.BackwardContext.BackwardFromActivityInstance.Id)
                {
                    aim.Cancel(SailSqlAccess.Sibling(kit), ai.Id.Value, base.AppRunner);
                }
            }

            var nodeMediatorBackward = new NodeMediatorBackward(base.BackwardContext, kit, Sail);
            nodeMediatorBackward.CreateBackwardActivityTaskTransitionInstance(base.BackwardContext.ProcessInstance,
                base.BackwardContext.BackwardFromActivityInstance,
                BackwardTypeEnum.Sendback,
                base.BackwardContext.BackwardToTargetTransitionId,
                TransitionTypeEnum.Sendback,
                TransitionFlyingTypeEnum.NotFlying,
                base.ActivityResource,
                kit);

            aim.SendBack(SailSqlAccess.Sibling(kit), base.BackwardContext.BackwardFromActivityInstance.Id.Value, base.ActivityResource.AppRunner);

            result.BackwardTaskReceiver = base.BackwardContext.BackwardTaskReceiver;
            result.Status = WfExecutedStatus.Success;
        }
    }
}
