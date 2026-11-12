using System;
using System.Threading.Tasks;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.engine.Manager;
using sailflow.engine.Result;
using sailflow.engine.Xpdl;

namespace sailflow.engine.Executor
{
    /// <summary>
    /// H · 无状态自动编排执行器：图遍历委托 <see cref="WorkflowNodeRunner"/>，节点执行委托 <see cref="WorkflowActivityExecutor"/>。
    /// 不持 Runner / ProcessModel / 变量袋；建造见 <c>Sail.CreateAutoRunBuilder</c>。
    /// </summary>
    public class WorkflowExecutor : IWorkflowExecutor
    {
        public WorkflowExecutor(SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        public Task<WfExecutedResult> Run(
            FlowCommandContext runner,
            IProcessModel processModel,
            AutoExecutionContext executionContext = null,
            string notifyClientSessionId = null,
            Action<string, object> notifyClientCallback = null)
        {
            ArgumentNullException.ThrowIfNull(runner);
            ArgumentNullException.ThrowIfNull(processModel);

            var result = new WfExecutedResult
            {
                Status = WfExecutedStatus.Success
            };

            var ctx = executionContext ?? new AutoExecutionContext();
            ctx.Sail = Sail;
            ctx.ProcessModel = processModel;
            ctx.Runner = runner;
            ctx.Variables ??= new System.Collections.Generic.Dictionary<string, string>();

            try
            {
                var startActivity = processModel.GetStartActivity();
                if (startActivity == null)
                {
                    result.Status = WfExecutedStatus.Failed;
                    result.Message = "There isnt start activity in the workflow diagram";
                    return Task.FromResult(result);
                }

                var activityExecutor = new WorkflowActivityExecutor(Sail, notifyClientSessionId, notifyClientCallback);
                var nodeRunner = new WorkflowNodeRunner(processModel, activityExecutor);
                nodeRunner.Run(startActivity, ctx, result);

                if (result.Status == WfExecutedStatus.Success)
                {
                    result.Message = "Workflow executed successfully";
                    if (ctx.Variables != null
                        && ctx.Variables.TryGetValue("ai_response", out var aiResp)
                        && aiResp != null
                        && !string.IsNullOrWhiteSpace(aiResp.ToString()))
                        result.AiResponse = aiResp.ToString();
                }
            }
            catch (Exception ex)
            {
                result.Status = WfExecutedStatus.Failed;
                result.Message = $"Auto orchestration execution failed: {ex.Message}";
                LogManager.RecordLog(Sail, WfDefine.WF_WORKFLOW_EXECUTOR_RUN_ERROR, LogEventType.Error, LogPriority.High, runner, ex);
            }

            return Task.FromResult(result);
        }
    }
}
