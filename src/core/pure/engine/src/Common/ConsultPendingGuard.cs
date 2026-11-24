using System.Collections.Generic;
using mooSQL.data;
using sailflow.engine.Client;
using sailflow.hooks;
using sailflow.log;
using sailflow.engine.Result;

namespace sailflow.utils
{
    /// <summary>
    /// 沟通等待协议硬锁：源审批 Task 下仍有打开参与者时，禁止推进/改责任类动词。
    /// </summary>
    public static class ConsultPendingGuard
    {
        static readonly HashSet<FlowOperation> LockedOperations = new()
        {
            FlowOperation.Run,
            FlowOperation.Reject,
            FlowOperation.SendBack,
            FlowOperation.SignForward,
            FlowOperation.Transfer,
            FlowOperation.Delegate,
            FlowOperation.ChangeAssignee,
            FlowOperation.RemoveAssignee,
            FlowOperation.AddAssignee,
            FlowOperation.Revise,
            FlowOperation.Resend,
            FlowOperation.Jump,
            FlowOperation.Close,
        };

        /// <summary>
        /// 若应拦截则写入 <paramref name="result"/> 并返回 false；否则返回 true 继续。
        /// </summary>
        public static bool TryAllow(
            SailClient sail,
            SQLBuilder kit,
            string taskId,
            FlowOperation operation,
            WfExecutedResult result)
        {
            if (!LockedOperations.Contains(operation))
                return true;
            if (string.IsNullOrWhiteSpace(taskId) || sail == null || kit == null)
                return true;

            if (!sail.ManagerFactory.Tasks.HasOpenParticipants(kit, taskId))
                return true;

            result.Status = WfExecutedStatus.Failed;
            result.ReasonCode = WfExceptionType.ConsultPending;
            result.Message = "Consult pending: wait for participants to acknowledge or recall consult.";
            SailFailLog.Interrupt(
                sail,
                SailLogCategories.Guard,
                "ConsultPending",
                "op={0} task={1} msg={2}",
                operation,
                taskId,
                result.Message);
            return false;
        }
    }
}
