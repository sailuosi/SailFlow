using saiflow.engine.Assignee;

namespace saiflow.engine.Core.Runtime
{
    /// <summary>R2/R4：办理人管道拒绝推进（缺配置 / 空办理人等）。</summary>
    public class WfAssigneeException : WfRuntimeException
    {
        public string ReasonCode { get; }

        public WfAssigneeException(string reasonCode, string message)
            : base(message)
        {
            ReasonCode = string.IsNullOrWhiteSpace(reasonCode)
                ? AssigneeReasonCodes.AssigneeConfigMissing
                : reasonCode;
        }
    }
}
