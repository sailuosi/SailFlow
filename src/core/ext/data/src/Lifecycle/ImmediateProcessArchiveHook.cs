using System;
using saiflow.data.Hosting;
using saiflow.engine.Client;
using saiflow.engine.Event;
using saiflow.engine.Hooks;

namespace saiflow.data.Lifecycle
{
    /// <summary>
    /// 办结立刻归档样例：对当前 <see cref="WorkflowHookContext.ProcessInstanceId"/> 调
    /// <c>ArchiveProcessInstance</c> 扩展（绕过保留窗批量筛选）。
    /// <b>默认不注册</b>；需立刻迁出时由宿主 <c>UseImmediateProcessArchiveHook</c> 显式挂载。
    /// 建议 AsyncOutbox，避免拉长 Complete 事务。
    /// </summary>
    public sealed class ImmediateProcessArchiveHook : IWorkflowHook
    {
        public ImmediateProcessArchiveHook(SailClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public string Name => "sail.lifecycle.archive.immediate";

        public SailClient Client { get; }

        public void Handle(WorkflowHookContext ctx)
        {
            if (ctx == null || ctx.ProcessInstanceId <= 0)
                return;
            if (ctx.EventType != EventFireTypeEnum.OnProcessCompleted
                && ctx.EventType != EventFireTypeEnum.OnProcessClosed)
                return;

            Client.ArchiveProcessInstance(ctx.ProcessInstanceId);
        }
    }
}
