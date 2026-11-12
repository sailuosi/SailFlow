using System.Collections.Generic;
using System.Linq;

namespace sailflow.engine.Hooks
{
    /// <summary>
    /// Gate 校验结果；Reject 映射到 <see cref="Result.WfExecutedResult"/>，不当事故异常。
    /// </summary>
    public sealed class HookResult
    {
        public bool IsOk { get; init; }
        public string Message { get; init; }
        public string Code { get; init; }
        public IReadOnlyList<HookValidationItem> Details { get; init; }

        public static HookResult Ok() => new() { IsOk = true };

        public static HookResult Reject(
            string message,
            string code = null,
            IEnumerable<HookValidationItem> details = null) =>
            new()
            {
                IsOk = false,
                Message = message,
                Code = code,
                Details = details?.ToList()
            };
    }

    /// <summary>结构化校验明细（可选表单字段）。</summary>
    public sealed class HookValidationItem
    {
        public string Field { get; init; }
        public string Message { get; init; }
    }
}
