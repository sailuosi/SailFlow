using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using saiflow.AI.Abstractions;
using saiflow.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.engine.Common;
using saiflow.engine.Event;
using saiflow.engine.Xpdl.Common;
using saiflow.engine.Xpdl.Entity;
using saiflow.hub.Localize;
using saiflow.WebUtility;

using saiflow.engine.Client;
namespace saiflow.engine.Core.Pattern.Auto
{
    internal class AiServiceExecutor
    {
        public AiServiceExecutor(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        /// <summary>
        /// Execute AI service list on an AIServiceNode (scheduling only; AI work via IAiNodeExecutor).
        /// </summary>
        internal static void ExecuteAIServiceList(SailClient client, Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            Activity currentActivity,
            ActivityInstanceEntity currentActivityInstance,
            ActivityForwardContext activityForwardContext,
            IEventService eventService)
        {
            IList<AiServiceDetail> aiServiceList = currentActivity.AIServiceList;
            if (aiServiceList == null || aiServiceList.Count == 0)
                return;

            foreach (var service in aiServiceList)
            {
                if (service.AIServiceType != AiServiceTypeEnum.None)
                {
                    Execute(client, fromActivity, fromActivityInstance, currentActivity, service,
                        currentActivityInstance, activityForwardContext, eventService)
                        .GetAwaiter().GetResult();
                }
            }
        }

        private static async Task Execute(SailClient client, Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            Activity currentActivity,
            AiServiceDetail service,
            ActivityInstanceEntity currentActivityInstance,
            ActivityForwardContext activityForwardContext,
            IEventService eventService)
        {
            if (service.AIServiceType != AiServiceTypeEnum.LLM
                && service.AIServiceType != AiServiceTypeEnum.RAG
                && service.AIServiceType != AiServiceTypeEnum.Agent)
            {
                throw new WorkflowException(LocalizeMessagesLocator.Current.GetEngineMessage(
                    "actionexecutor.Execute.exception", service.AIServiceType.ToString()));
            }

            try
            {
                var kit = eventService.GetKit();
                var processInstanceId = eventService.GetProcessInstanceId();
                var pvm = new ProcessVariableManager(client);

                var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                var outputVariableNameList = activityForwardContext.ProcessModel
                    .GetActivityOutputVarialbeNameList(fromActivity);
                if (outputVariableNameList != null && fromActivityInstance != null)
                {
                    var inputVariableValueList = pvm.GetVariableListByActivity(
                        kit, processInstanceId, fromActivityInstance.Id, outputVariableNameList);

                    foreach (var item in inputVariableValueList)
                        variables[item.Name] = item.Value ?? string.Empty;
                }

                var processId = activityForwardContext.ProcessModel?.ProcessEntity?.ProcessId
                    ?? activityForwardContext.ProcessInstance?.ProcessId;
                var processVersion = activityForwardContext.ProcessModel?.ProcessEntity?.Version
                    ?? activityForwardContext.ProcessInstance?.Version
                    ?? "1";

                if (string.IsNullOrEmpty(processId))
                {
                    var processInstance = new ProcessInstanceManager(client).GetById(kit, processInstanceId);
                    if (processInstance == null)
                        throw new WorkflowException($"Process instance not found: id={processInstanceId}");
                    processId = processInstance.ProcessId;
                    processVersion = processInstance.Version ?? "1";
                }

                var request = new AiNodeExecutionRequest
                {
                    ServiceType = service.AIServiceType.ToString(),
                    ProcessId = processId,
                    Version = processVersion,
                    ActivityId = currentActivity.ActivityId,
                    ToolSetName = service.ToolSetName,
                    Variables = variables,
                    NotifyClientEnabled = false
                };

                var aiResult = await AiNodeExecutorLocator.Resolve()
                    .ExecuteAsync(request)
                    .ConfigureAwait(false);

                var aiResponseMessage = aiResult?.ResponseText ?? string.Empty;

                var currentOutputVariable = currentActivity.VariableList?
                    .FirstOrDefault(a => a.DirectionType == VariableDirectionTypeEnum.Output);

                var appInstanceId = activityForwardContext.ProcessInstance?.AppInstanceId ?? string.Empty;
                var processVariable = new ProcessVariableEntity
                {
                    ProcessInstanceId = processInstanceId,
                    ProcessId = processId,
                    AppInstanceId = appInstanceId,
                    ActivityInstanceId = currentActivityInstance.Id,
                    ActivityId = currentActivityInstance.ActivityId,
                    ActivityName = currentActivityInstance.ActivityName,
                    Name = currentOutputVariable?.Name
                        ?? (service.AIServiceType == AiServiceTypeEnum.Agent ? "AgentResult" : "AIServiceResult"),
                    VariableScope = ProcessVariableScopeEnum.Activity.ToString(),
                    Value = aiResponseMessage,
                    MediaType = MultiMediaTypeEnum.Text.ToString()
                };

                pvm.SaveVariable(kit, processVariable);

                // Persist extra updates (chat_history / ai_response) as process-scoped variables when present
                if (aiResult?.VariableUpdates != null)
                {
                    foreach (var kv in aiResult.VariableUpdates)
                    {
                        pvm.SaveVariable(kit, new ProcessVariableEntity
                        {
                            ProcessInstanceId = processInstanceId,
                            ProcessId = processId,
                            AppInstanceId = appInstanceId,
                            ActivityInstanceId = currentActivityInstance.Id,
                            ActivityId = currentActivityInstance.ActivityId,
                            ActivityName = currentActivityInstance.ActivityName,
                            Name = kv.Key,
                            VariableScope = ProcessVariableScopeEnum.Process.ToString(),
                            Value = kv.Value ?? string.Empty,
                            MediaType = MultiMediaTypeEnum.Text.ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new WorkflowException(LocalizeMessagesLocator.Current.GetEngineMessage(
                    "actionexecutor.ExecuteLocalService.exception", ex.Message));
            }
        }
    }
}
