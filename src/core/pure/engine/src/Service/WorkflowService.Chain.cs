namespace saiflow.engine.Service
{
    /// <summary>
    /// 链式建造已迁出：请使用 <c>SailClient.CreateRunBuilder(...)</c>（B），再 <c>XxxProcess(runner)</c>。
    /// <see cref="WorkflowService"/> 仅为无状态执行器（H），不持 Runner / 链式会话。
    /// </summary>
    public partial class WorkflowService : IWorkflowService
    {
    }
}
