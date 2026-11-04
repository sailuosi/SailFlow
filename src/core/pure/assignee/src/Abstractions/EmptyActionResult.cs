using saiflow.engine.Assignee;
using saiflow.engine.Common;

namespace saiflow.assignee;

/// <summary>R10：空集 EmptyAction 结果。</summary>
public sealed class EmptyActionResult
{
    public bool IsReject { get; init; }
    public PerformerList? Performers { get; init; }
    public string? ReasonCode { get; init; }
    public string? Message { get; init; }

    public static EmptyActionResult Reject(
        string reasonCode = AssigneeReasonCodes.AssigneeEmpty,
        string? message = null) =>
        new()
        {
            IsReject = true,
            ReasonCode = reasonCode,
            Message = message ?? "Assignee resolve empty (EmptyAction reject / R4)."
        };

    public static EmptyActionResult WithPerformers(PerformerList performers) =>
        new()
        {
            IsReject = false,
            Performers = performers ?? new PerformerList()
        };
}
