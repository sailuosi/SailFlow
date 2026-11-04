namespace saiflow.engine.Assignee
{
    /// <summary>R4：办理人管道可识别拒绝码（写入 <c>WfExecutedResult.ReasonCode</c>）。</summary>
    public static class AssigneeReasonCodes
    {
        /// <summary>节点无显式 Resolve ConfigPair（且无可用 Legacy 推断结果）。</summary>
        public const string AssigneeConfigMissing = "AssigneeConfigMissing";

        /// <summary>Resolve Type 已配置但算人为空（且无显式 Fallback / NodePolicy 换人失败）。</summary>
        public const string AssigneeEmpty = "AssigneeEmpty";

        /// <summary>NodePolicy 空人=转管理员，但宿主未配置 ProcessAdminUserIds。</summary>
        public const string ProcessAdminNotConfigured = "ProcessAdminNotConfigured";

        /// <summary>NodePolicy 空人=指定人员，但 assignEmptyUserIds 为空。</summary>
        public const string AssignEmptyUsersMissing = "AssignEmptyUsersMissing";

        /// <summary>会签/多实例子节点流出时无 LeaveJudge 配置且管道未处理。</summary>
        public const string LeaveConfigMissing = "LeaveConfigMissing";

        /// <summary>LeaveJudge Type 未注册。</summary>
        public const string LeaveTypeUnregistered = "LeaveTypeUnregistered";

        /// <summary>R10：自动审批锚点复核未过（不代办，回落手动）。</summary>
        public const string AutoApproveSkipped = "AutoApproveSkipped";

        /// <summary>R10：自动审批已尝试 Run 失败（回落手动）。</summary>
        public const string AutoApproveFailed = "AutoApproveFailed";

        /// <summary>R10：会签 SignTogether 不支持零 Performer 自动过（须有真人子实例）。</summary>
        public const string AssigneeAutoApproveNotForMultiSign = "AssigneeAutoApproveNotForMultiSign";
    }
}
