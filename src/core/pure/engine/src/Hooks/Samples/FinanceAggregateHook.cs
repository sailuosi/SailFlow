using saiflow.engine.Event;
using saiflow.engine.Hooks;

namespace saiflow.engine.Samples
{
    /// <summary>
    /// 文档示例：流程继续后 Async Outbox 汇总（财务出单等）。
    /// </summary>
    public sealed class FinanceAggregateHook : IFlowHook
    {
        public string Name => "finance.aggregate";

        public void Handle(FlowHookContext ctx)
        {
            if (ctx.EventType != EventFireTypeEnum.OnProcessContinued)
                return;
            // Worker 消费 Outbox 时执行：写业务汇总表 / 调用外部系统等
        }
    }
}
