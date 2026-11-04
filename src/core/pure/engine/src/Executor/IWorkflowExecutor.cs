using saiflow.engine.Result;
using saiflow.engine.Entity;
using System.Threading.Tasks;
using System.Linq;
using System;


namespace saiflow.engine.Executor
{
    public interface IWorkflowExecutor
    {
        IWorkflowExecutor UseApp(string appInstanceId, string appName, string appCode = null);
        IWorkflowExecutor UseProcess(string processId, string version = null);
        IWorkflowExecutor UseProcess(ProcessEntity processEntity);
        IWorkflowExecutor AddVariable(string name, string value);
        IWorkflowExecutor SetNotifyClient(string sessionId, Action<string, object> callback);
        IWorkflowExecutor WithDelegateRegistry(IServiceTaskDelegateRegistry registry);
        Task<WfExecutedResult> Run();
    }
}
