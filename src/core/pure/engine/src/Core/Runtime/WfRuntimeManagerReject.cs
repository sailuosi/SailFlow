using sailflow.data;
using mooSQL.data;
using sailflow.engine.Manager;
using sailflow.engine.Common;
using sailflow.engine.Result;
using sailflow.engine.Pattern;
using sailflow.engine.Core;

namespace sailflow.engine.Runtime
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
            tm.Complete(kit.useSQL(), this.TaskView.TaskId, base.CmdContext);

            //取消当前其它处于办理状态的节点
            //Cancel other nodes currently in processing status
            var aim = Sail.ManagerFactory.Activities;
            var runningList = aim.GetRunningActivityInstanceList(kit.useSQL(), base.CmdContext.BizPK, base.CmdContext.ProcessId, base.CmdContext.Version);
            foreach (var ai in runningList)
            {
                if (ai.ActivityInstanceId != base.BackwardContext.BackwardFromActivityInstance.ActivityInstanceId)
                {
                    aim.Cancel(kit.useSQL(), ai.ActivityInstanceId, base.CmdContext);
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

            aim.SendBack(kit.useSQL(), base.BackwardContext.BackwardFromActivityInstance.ActivityInstanceId, base.ActivityResource.AppRunner);

            result.BackwardTaskReceiver = base.BackwardContext.BackwardTaskReceiver;
            result.Status = WfExecutedStatus.Success;
        }
    }
}
