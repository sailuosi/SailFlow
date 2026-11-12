using System;
using System.Threading.Tasks;
using sailflow.engine.Common;
using sailflow.engine.Result;
using sailflow.engine.Xpdl;

namespace sailflow.engine.Executor
{
    /// <summary>
    /// H · 无状态自动编排执行器：不持 Runner / 建造态；入口只吃参数。
    /// 链式建造见 <c>SailClient.CreateAutoRunBuilder</c>。
    /// </summary>
    public interface IWorkflowExecutor
    {
        Task<WfExecutedResult> Run(
            FlowCommandContext runner,
            IProcessModel processModel,
            AutoExecutionContext executionContext = null,
            string notifyClientSessionId = null,
            Action<string, object> notifyClientCallback = null);
    }
}
