using sailflow.engine.NextStep;

namespace sailflow.external.NextStep;

/// <summary>
/// 文档样例：Client 级 Prepare 补金额键（演示用；正式宿主应读单据）。
/// 注册：
/// <c>UseNextStepRoute(r => r.IfCondition&lt;SampleAmountPrepare&gt;(o => { o.ProcessCodes = new[] { "EXPENSE" }; }))</c>
/// </summary>
public sealed class SampleAmountPrepare : NextStepPrepare
{
    public SampleAmountPrepare()
    {
        // 静态兜底；真实环境请在 Contribute 读单据覆盖
        IfCondition("amount", "0");
    }

    protected override void Contribute(NextStepRouteContext ctx)
    {
        // 样例：若请求级已有 amount 则不覆盖；否则保持构造期字面量
        if (ctx?.Conditions == null)
            return;
        if (!ctx.Conditions.ContainsKey("amount"))
            ctx.Conditions["amount"] = "0";
    }
}
