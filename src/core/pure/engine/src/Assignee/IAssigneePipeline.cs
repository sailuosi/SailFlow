using saiflow.engine.Common;

namespace saiflow.engine.Assignee
{
    /// <summary>
    /// 办理人运行时管道（薄契约）。实现由 <c>sailflow.assignee</c> 经 <c>UseAssignees</c> 注入。
    /// engine 只调用，不包含 Type 分发与策略实现。
    /// </summary>
    public interface IAssigneePipeline
    {
        /// <summary>
        /// 创建待办前解析办理人。返回 false 表示管道未处理（无 ConfigPair / Type 未注册 / 空结果）。
        /// 宿主已挂管道时 Mediator 按 R2/R4 拒绝推进，不再回落 Definition。
        /// </summary>
        bool TryResolveForTaskCreate(AssigneeInvokeRequest request, out PerformerList performers);

        /// <summary>
        /// 判断当前节点是否可流出。返回 false 表示未由管道处理。
        /// 宿主已挂管道时 AIM 按 R2 拒绝回落旧会签通过率。
        /// </summary>
        bool TryCanLeave(LeaveInvokeRequest request, out LeaveJudgeResult result);
    }
}
