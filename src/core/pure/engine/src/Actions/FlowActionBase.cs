using mooSQL.data;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.data;
using sailflow.events;
using sailflow.engine.Hooks;
using sailflow.engine.Result;

namespace sailflow.engine.Actions
{
    /// <summary>
    /// 动作骨架共同父类：横切工具箱（Gate/Hook/诊断能力），不排统一时序。
    /// 事务与实例锁由 <see cref="Service.WorkflowService"/> 外壳独占。
    /// </summary>
    public abstract class FlowActionBase
    {
        protected FlowActionBase(SailClient sail, SQLBuilder kit, FlowCommandContext context)
        {
            Sail = sail ?? throw new System.ArgumentNullException(nameof(sail));
            Kit = kit ?? throw new System.ArgumentNullException(nameof(kit));
            Context = context;
        }

        protected SailClient Sail { get; }
        protected SQLBuilder Kit { get; }
        protected FlowCommandContext Context { get; }

        /// <summary>Gate / Hook / 审计身份。</summary>
        public abstract FlowOperation Operation { get; }

        protected FlowHookContext BuildGateContext(
            string processInstanceId,
            ActivityInstanceEntity activityInstance,
            string taskId) =>
            FlowCapabilityPipeline.BuildGateContext(
                Sail, Kit, Context, processInstanceId, activityInstance, taskId, Operation);

        protected FlowHookContext BuildHookContext(
            string processInstanceId,
            ActivityInstanceEntity activityInstance,
            string taskId,
            EventFireTypeEnum eventType) =>
            FlowCapabilityPipeline.BuildHookContext(
                Sail, Kit, Context, processInstanceId, activityInstance, taskId, eventType, Operation);

        /// <summary>适配现有 TryGate；Reject 时写入 <paramref name="result"/>。</summary>
        protected bool TryGate(FlowHookContext gateCtx, WfExecutedResult result) =>
            FlowCapabilityPipeline.TryGate(Sail, Kit, gateCtx, result);

        /// <summary>成功后同步 + 异步 Hook（对齐 AfterSuccess）。</summary>
        protected void OnFireFlowHook(
            FlowHookContext hookCtx,
            EventFireTypeEnum successEvent) =>
            FlowCapabilityPipeline.AfterSuccess(Sail, Kit, hookCtx, successEvent);
    }
}
