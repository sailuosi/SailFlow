using sailflow.engine.Hooks;

namespace sailflow.external.Hooks;

/// <summary>
/// 文档示例：发送/启动前 Gate（白名单流程定义 Code）。
/// 注册：<c>UseFlowGate(new FinancePreSendGate(), o =&gt; o.ProcessCodes = new[] { "EXPENSE" })</c>
/// </summary>
public sealed class FinancePreSendGate : IFlowGate
{
    public string Name => "finance.pre-send";

    public HookResult Validate(FlowHookContext ctx)
    {
        // 示例：按 Operation / ActivityCode 做校验，失败返回 Reject
        return HookResult.Ok();
    }
}
