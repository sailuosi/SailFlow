namespace saiflow.engine.Assignee
{
    /// <summary>R4：办理人管道可识别拒绝码（写入 <c>WfExecutedResult.ReasonCode</c>）。</summary>
    public static class AssigneeReasonCodes
    {
        /// <summary>节点无显式 Resolve ConfigPair（且无可用 Legacy 推断结果）。</summary>
        public const string AssigneeConfigMissing = "AssigneeConfigMissing";

        /// <summary>Resolve Type 已配置但算人为空（且无显式 Fallback）。</summary>
        public const string AssigneeEmpty = "AssigneeEmpty";

        /// <summary>会签/多实例子节点流出时无 LeaveJudge 配置且管道未处理。</summary>
        public const string LeaveConfigMissing = "LeaveConfigMissing";

        /// <summary>LeaveJudge Type 未注册。</summary>
        public const string LeaveTypeUnregistered = "LeaveTypeUnregistered";
    }
}
