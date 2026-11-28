using System.Collections.Generic;
using sailflow.utils;

namespace sailflow.assignee
{
    /// <summary>
    /// 办理人运行时管道（薄契约）。实现由 <c>sailflow.assignee</c> 经 <c>UseAssignees</c> 注入。
    /// engine 只调用，不包含 Type 分发与策略实现。
    /// </summary>
    public interface IAssigneePipeline
    {
        /// <summary>
        /// 创建待办前解析办理人。
        /// 返回 false：无 ConfigPair / 空 ActivityId 等「未处理」。
        /// Type 未注册或 EmptyAction 拒绝：抛 <c>WfAssigneeException</c>（ReasonCode 可识别），不返回 false。
        /// 宿主已挂管道时 Mediator 按 R2/R4 拒绝推进，不再回落 Definition。
        /// </summary>
        bool TryResolveForTaskCreate(AssigneeInvokeRequest request, out PerformerList performers);

        /// <summary>
        /// 判断当前节点是否可流出。
        /// 返回 false：无 ConfigPair 等「未处理」；Leave Type 未注册抛 <c>WfAssigneeException</c>。
        /// 宿主已挂管道时 AIM 按 R2 拒绝回落旧会签通过率。
        /// </summary>
        bool TryCanLeave(LeaveInvokeRequest request, out LeaveJudgeResult result);

        /// <summary>
        /// 按 Scope 注册表加载选人候选（工具栏 / 选人器预览快捷方法）。
        /// 语义：强制走 Fixed 展开；空 type 或 <c>AnyMan</c> → <see cref="ScopeLoadMeta.Unconstrained"/>；
        /// 未注册 type → 返回 false 且 <see cref="ScopeLoadMeta.UnregisteredType"/>。
        /// 不跑 EmptyAction / Filter（预览用原始 Scope 结果）。
        /// <paramref name="scopeParams"/> 与 BPMN <c>params</c> / 按钮 Scope 参数同形。
        /// </summary>
        bool TryLoadScopeCandidates(
            string scopeType,
            IDictionary<string, object> scopeParams,
            AssigneeInvokeRequest request,
            out PerformerList performers,
            out ScopeLoadMeta meta);
    }
}
