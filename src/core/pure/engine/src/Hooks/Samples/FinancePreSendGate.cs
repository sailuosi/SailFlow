namespace saiflow.engine.Hooks.Samples
{
    /// <summary>
    /// 文档示例：发送/启动前 Gate（白名单流程定义 Code）。
    /// 注册：<c>UseWorkflowGate(new FinancePreSendGate(), o =&gt; o.ProcessCodes = new[] { "EXPENSE" })</c>
    /// </summary>
    public sealed class FinancePreSendGate : IWorkflowGate
    {
        public string Name => "finance.pre-send";

        public HookResult Validate(WorkflowHookContext ctx)
        {
            // 示例：按 Operation / ActivityCode 做校验，失败返回 Reject
            return HookResult.Ok();
        }
    }
}
