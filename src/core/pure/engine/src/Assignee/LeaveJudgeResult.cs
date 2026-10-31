namespace saiflow.engine.Assignee
{
    /// <summary>流出判定结果。Decision 使用 <see cref="LeaveDecisions"/> 字符串常量。</summary>
    public class LeaveJudgeResult
    {
        /// <summary><see cref="LeaveDecisions"/>：Passed / NotPassed / Waiting。</summary>
        public string Decision { get; set; }

        public string ReasonCode { get; set; }
        public string Message { get; set; }

        public bool Allow => string.Equals(Decision, LeaveDecisions.Passed, System.StringComparison.OrdinalIgnoreCase);

        public static LeaveJudgeResult Passed(string reasonCode = null, string message = null) =>
            new LeaveJudgeResult
            {
                Decision = LeaveDecisions.Passed,
                ReasonCode = reasonCode,
                Message = message
            };

        public static LeaveJudgeResult NotPassed(string reasonCode = null, string message = null) =>
            new LeaveJudgeResult
            {
                Decision = LeaveDecisions.NotPassed,
                ReasonCode = reasonCode,
                Message = message
            };

        public static LeaveJudgeResult Waiting(string reasonCode = null, string message = null) =>
            new LeaveJudgeResult
            {
                Decision = LeaveDecisions.Waiting,
                ReasonCode = reasonCode,
                Message = message
            };
    }

    /// <summary>流出决策码（string，禁止 enum 分发）。</summary>
    public static class LeaveDecisions
    {
        public const string Passed = "Passed";
        public const string NotPassed = "NotPassed";
        public const string Waiting = "Waiting";
    }
}
