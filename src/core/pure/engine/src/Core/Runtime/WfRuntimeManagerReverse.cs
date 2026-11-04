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
    /// Handling of reverse methods
    /// 流程返签时的运行时
    /// </summary>
    internal class WfRuntimeManagerReverse : WfRuntimeManager
    {
        /// <summary>
        /// Reverse execute method
        /// 返签执行方法
        /// </summary>
        /// <param name="kit"></param>
        internal override void ExecuteInstanceImp(SQLBuilder kit)
        {
            //修改流程实例为返签状态
            //Modify the process instance to a reverse status
            var pim = new ProcessInstanceManager(Sail);
            pim.Reverse(kit, base.BackwardContext.ProcessInstance.Id.Value, base.AppRunner);

            var nodeMediatorBackward = new NodeMediatorBackward(base.BackwardContext, kit, Sail);
            nodeMediatorBackward.CreateBackwardActivityTaskTransitionInstance(base.BackwardContext.ProcessInstance,
                base.BackwardContext.BackwardFromActivityInstance,
                BackwardTypeEnum.Reversed,
                base.BackwardContext.BackwardToTargetTransitionId,
                TransitionTypeEnum.Backward,
                TransitionFlyingTypeEnum.NotFlying,
                base.ActivityResource,
                kit);

            WfExecutedResult result = base.WfExecutedResult;
            result.BackwardTaskReceiver = base.BackwardContext.BackwardTaskReceiver;
            result.Status = WfExecutedStatus.Success;
        }
    }
}
