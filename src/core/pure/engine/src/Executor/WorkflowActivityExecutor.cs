using saiflow.AI.Abstractions;
using saiflow.data;
using saiflow.engine.Entity;
using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.engine.Core;
using saiflow.engine.Result;
using saiflow.engine.Event;
using saiflow.engine.External;
using saiflow.engine.Utility;
using saiflow.engine.Xpdl;
using saiflow.hub.Localize;
using saiflow.WebUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace saiflow.engine.Executor
{
    /// <summary>
    /// Executes a single workflow activity: service, AI service, script, end.
    /// </summary>
    public class WorkflowActivityExecutor : IWorkflowActivityExecutor
    {
        public WorkflowActivityExecutor(SailClient client, string notifyClientSessionId = null, Action<string, object> notifyClientCallback = null)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
            _notifyClientSessionId = notifyClientSessionId;
            _notifyClientCallback = notifyClientCallback;
        }

        protected SailClient Sail { get; }

        private readonly string _notifyClientSessionId;
        private readonly Action<string, object> _notifyClientCallback;

        /// <summary>
        /// Execute a single activity (service, AI, script, or end node).
        /// </summary>
        public void ExecuteActivity(Activity currentActivity, AutoExecutionContext context, WfExecutedResult result)
        {
            try
            {
                if (currentActivity.ActivityType == ActivityTypeEnum.ServiceNode)
                {
                    var serviceList = currentActivity.ServiceList;
                    if (serviceList != null && serviceList.Count > 0)
                        ExecuteServiceNode(currentActivity, serviceList, context);
                }

                if (currentActivity.ActivityType == ActivityTypeEnum.AIServiceNode)
                {
                    var aiServiceList = currentActivity.AIServiceList;
                    if (aiServiceList != null && aiServiceList.Count > 0)
                        ExecuteAIServiceNode(currentActivity, aiServiceList, context);
                }

                if (currentActivity.ScriptList != null && currentActivity.ScriptList.Count > 0)
                {
                    var scriptList = currentActivity.ScriptList;
                    if (scriptList != null && scriptList.Count > 0)
                        ExecuteScriptNode(currentActivity, scriptList, context);
                }
            }
            catch (Exception ex)
            {
                result.Status = WfExecutedStatus.Failed;
                result.Message = $"Failed to execute activity {currentActivity.ActivityId}: {ex.Message}";
            }

            Console.WriteLine($"The activity:{currentActivity.ActivityName} has been executed successed");
        }

        /// <summary>
        /// Execute service list (aligned with Engine Core.Pattern.Auto.ServiceExecutor.ExecuteServiceList).
        /// </summary>
        public void ExecuteServiceNode(Activity activity, List<ServiceDetail> serviceDetail, AutoExecutionContext context)
        {
            if (serviceDetail == null || serviceDetail.Count == 0)
                return;

            foreach (var service in serviceDetail)
            {
                if (service.Method == ServiceMethodEnum.None)
                    continue;
                ExecuteService(service, context);
            }
        }

        public void ExecuteService(ServiceDetail service, AutoExecutionContext context)
        {
            if (service.Method == ServiceMethodEnum.LocalService)
                ExecuteLocalServiceInExecutor(service, context);
            else if (service.Method == ServiceMethodEnum.CSharpLibrary)
                ExecuteCSharpLibraryInExecutor(service, context);
            else if (service.Method == ServiceMethodEnum.WebApi)
                ExecuteWebApiInExecutor(service, context);
            else if (service.Method == ServiceMethodEnum.StoreProcedure)
                ExecuteStoreProcedureInExecutor(service, context);
            else if (service.Method == ServiceMethodEnum.LocalMethod)
                ExecuteLocalMethodInExecutor(service, context);
            else
                throw new WorkflowException(Sail.Messages.GetEngineMessage("actionexecutor.Execute.exception", service.Method.ToString()));
        }

        public string GetVariableFromContext(AutoExecutionContext context, string name)
        {
            if (context?.Variables == null || string.IsNullOrWhiteSpace(name))
                return string.Empty;
            return context.Variables.TryGetValue(name.Trim(), out var v) ? (v ?? string.Empty) : string.Empty;
        }

        public string CompositeJsonValueFromContext(string arguments, AutoExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(arguments))
                return "{}";
            var sb = new StringBuilder(256);
            var arguList = arguments.Split(',');
            foreach (var name in arguList)
            {
                var n = name?.Trim();
                if (string.IsNullOrEmpty(n)) continue;
                if (sb.Length > 0) sb.Append(",");
                var arguValue = GetVariableFromContext(context, n);
                arguValue = FormatJsonStringIfSimple(arguValue);
                sb.AppendFormat("{0}:{1}", n, arguValue);
            }
            return sb.Length > 0 ? "{" + sb + "}" : "{}";
        }

        public string FormatJsonStringIfSimple(string jsonValue)
        {
            if (string.IsNullOrEmpty(jsonValue)) return "\"\"";
            jsonValue = jsonValue.Trim();
            if ((jsonValue.StartsWith("{") && jsonValue.EndsWith("}")) || (jsonValue.StartsWith("\"") && jsonValue.EndsWith("\"")))
                return jsonValue;
            return "\"" + jsonValue + "\"";
        }

        public object[] CompositeParameterValuesFromContext(string arguments, AutoExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(arguments))
                return null;
            var arguList = arguments.Split(',');
            var valueArray = new object[arguList.Length];
            for (var i = 0; i < arguList.Length; i++)
                valueArray[i] = GetVariableFromContext(context, arguList[i]);
            return valueArray;
        }

        public IDictionary<string, object> CompositeSqlParametersValueFromContext(string arguments, AutoExecutionContext context)
        {
            var parameters = new Dictionary<string, object>();
            if (string.IsNullOrWhiteSpace(arguments))
                return parameters;
            var arguList = arguments.Split(',');
            foreach (var name in arguList)
            {
                var n = name?.Trim();
                if (string.IsNullOrEmpty(n)) continue;
                var arguValue = GetVariableFromContext(context, n);
                parameters["@" + n] = arguValue;
            }
            return parameters;
        }

        public void ExecuteLocalServiceInExecutor(ServiceDetail service, AutoExecutionContext context)
        {
            try
            {
                context.Sail ??= Sail;
                var adapter = new AutoRunEventServiceAdapter(context);
                var instance = ReflectionHelper.GetSpecialInstance<IExternalService>(service.Expression);
                if (instance is IExecutable executableInstance)
                    executableInstance.Executable(adapter);
            }
            catch (Exception ex)
            {
                throw new WorkflowException(Sail.Messages.GetEngineMessage("actionexecutor.ExecuteLocalService.exception", ex.Message));
            }
        }

        /// <summary>
        /// Execute LocalMethod ServiceTask: resolve delegate from registry by key and invoke.
        /// </summary>
        public void ExecuteLocalMethodInExecutor(ServiceDetail service, AutoExecutionContext context)
        {
            var key = service.Expression;
            if (string.IsNullOrWhiteSpace(key))
                throw new WorkflowException("LocalMethod requires Expression (registry key).");

            var registry = context.DelegateRegistry ?? ServiceTaskDelegateRegistry.Global;
            if (!registry.TryResolve(key, out var d))
                throw new WorkflowException($"Delegate not found for key: {key}. Register before Run with ServiceTaskDelegateRegistry.Global.Register(key, delegate).");

            context.Sail ??= Sail;
            var adapter = new AutoRunEventServiceAdapter(context);
            if (d is Action<IEventService> action)
            {
                action(adapter);
            }
            else if (d is Func<IEventService, object> func)
            {
                var result = func(adapter);
                if (result == null || context.Variables == null) return;

                string varName;
                string varValue;
                if (result is ServiceTaskResult sr)
                {
                    varName = sr.VariableName;
                    varValue = sr.Value?.ToString() ?? "";
                }
                else
                {
                    varName = ServiceTaskDelegateRegistry.DefaultResultVariableName;
                    varValue = result.ToString();
                }
                if (!string.IsNullOrEmpty(varName))
                    context.Variables[varName] = varValue;
            }
            else
            {
                throw new WorkflowException($"Delegate for key '{key}' must be Action<IEventService> or Func<IEventService, object>.");
            }
        }

        public void ExecuteWebApiInExecutor(ServiceDetail service, AutoExecutionContext context)
        {
            try
            {
                if (service.SubMethod == SubMethodEnum.HttpGet)
                {
                    var jsonGetValue = GetVariableFromContext(context, service.Arguments ?? "");
                    var url = string.Format("{0}/{1}", service.Expression, jsonGetValue);
                    var httpGetClient = HttpClientHelper.CreateHelper(url);
                    httpGetClient.Get();
                }
                else if (service.SubMethod == SubMethodEnum.HttpPost)
                {
                    var url = service.Expression;
                    var httpClientHelper = HttpClientHelper.CreateHelper(url);
                    var jsonValue = CompositeJsonValueFromContext(service.Arguments ?? "", context);
                    httpClientHelper.Post(jsonValue);
                }
                else if (service.SubMethod == SubMethodEnum.HttpPut)
                {
                    var url = service.Expression;
                    var httpClientHelper = HttpClientHelper.CreateHelper(url);
                    var jsonValue = CompositeJsonValueFromContext(service.Arguments ?? "", context);
                    httpClientHelper.Put(jsonValue);
                }
                else if (service.SubMethod == SubMethodEnum.HttpDelete)
                {
                    var jsonGetValue = GetVariableFromContext(context, service.Arguments ?? "");
                    var url = string.Format("{0}/{1}", service.Expression, jsonGetValue);
                    var httpGetClient = HttpClientHelper.CreateHelper(url);
                    httpGetClient.Delete();
                }
            }
            catch (Exception ex)
            {
                throw new WorkflowException(Sail.Messages.GetEngineMessage("actionexecutor.ExecuteWebApi.exception", ex.Message));
            }
        }

        public void ExecuteCSharpLibraryInExecutor(ServiceDetail service, AutoExecutionContext context)
        {
            try
            {
                var methodInfo = service.MethodInfo;
                if (methodInfo == null)
                    throw new WorkflowException("Service MethodInfo is not configured.");
                var assemblyFullName = methodInfo.AssemblyFullName;
                var methodName = methodInfo.MethodName;
                var executingPath = ConfigHelper.GetExecutingDirectory();
                var pluginAssemblyName = string.Format("{0}\\{1}\\{2}.dll", executingPath, "plugin", assemblyFullName);
                var pluginAssemblyTypes = Assembly.LoadFile(pluginAssemblyName).GetTypes();
                var outerClass = pluginAssemblyTypes.Single(t => !t.IsInterface && t.FullName == methodInfo.TypeFullName);
                var instance = Activator.CreateInstance(outerClass);
                var mi = outerClass.GetMethod(methodName);
                if (mi == null)
                    throw new WorkflowException($"Method {methodName} not found on type {methodInfo.TypeFullName}.");
                object[] methodParams = null;
                if (!string.IsNullOrWhiteSpace(service.Arguments))
                    methodParams = CompositeParameterValuesFromContext(service.Arguments, context);
                mi.Invoke(instance, methodParams);
            }
            catch (Exception ex)
            {
                throw new WorkflowException(Sail.Messages.GetEngineMessage("actionexecutor.ExecuteCSharpLibraryMethod.exception", ex.Message));
            }
        }

        public void ExecuteStoreProcedureInExecutor(ServiceDetail service, AutoExecutionContext context)
        {
            try
            {
                var parameters = CompositeSqlParametersValueFromContext(service.Arguments ?? "", context);
                var procedureName = service.Expression;
                using var kit = Sail.UseRunSQL();
                SailCmdRunner.ExecuteProc(kit, procedureName, parameters);
            }
            catch (Exception ex)
            {
                throw new WorkflowException(Sail.Messages.GetEngineMessage("actionexecutor.ExecuteStoreProcedureMethod.exception", ex.Message));
            }
        }

        public void ExecuteAIServiceNode(Activity currentActivity, List<AiServiceDetail> aiServiceList, AutoExecutionContext context)
        {
            if (aiServiceList == null || aiServiceList.Count == 0)
                return;

            foreach (var service in aiServiceList)
            {
                if (service.AIServiceType == AiServiceTypeEnum.None)
                    continue;

                if (service.AIServiceType == AiServiceTypeEnum.LLM
                    || service.AIServiceType == AiServiceTypeEnum.RAG
                    || service.AIServiceType == AiServiceTypeEnum.Agent)
                {
                    ExecuteAiServiceViaContract(currentActivity, service, context);
                }
                else
                {
                    throw new WorkflowException($"Unsupported AI service type: {service.AIServiceType}");
                }
            }
        }

        /// <summary>
        /// Dispatches an AI node through <see cref="IAiNodeExecutor"/> (business implementation).
        /// Engine only assembles variables and writes results back.
        /// </summary>
        public void ExecuteAiServiceViaContract(Activity currentActivity, AiServiceDetail service, AutoExecutionContext context)
        {
            string processId = context.Runner?.ProcessId;
            string version = context.Runner?.Version;
            if (string.IsNullOrEmpty(processId) || string.IsNullOrEmpty(version))
                throw new WorkflowException("ProcessId and Version are required for AI service execution.");

            var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (context.Variables != null)
            {
                foreach (var kv in context.Variables)
                    variables[kv.Key] = kv.Value ?? string.Empty;
            }

            var request = new AiNodeExecutionRequest
            {
                ServiceType = service.AIServiceType.ToString(),
                ProcessId = processId,
                Version = version,
                ActivityId = currentActivity.ActivityId,
                ToolSetName = service.ToolSetName,
                Variables = variables,
                NotifyClientEnabled = IsNotifyClientEnabled(currentActivity)
            };

            AiNodeExecutionResult aiResult;
            try
            {
                aiResult = AiNodeExecutorLocator.Resolve()
                    .ExecuteAsync(request)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (InvalidOperationException ex) when (!AiNodeExecutorLocator.IsRegistered)
            {
                throw new WorkflowException(ex.Message);
            }

            var aiResponseMessage = aiResult?.ResponseText ?? string.Empty;

            if (aiResult?.VariableUpdates != null)
            {
                foreach (var kv in aiResult.VariableUpdates)
                    context.Variables[kv.Key] = kv.Value ?? string.Empty;
            }

            var currentOutputVariable = currentActivity.VariableList?
                .FirstOrDefault(a => a.DirectionType == VariableDirectionTypeEnum.Output);
            var outputVarName = currentOutputVariable?.Name;
            if (!string.IsNullOrEmpty(outputVarName))
                context.Variables[outputVarName] = aiResponseMessage;
            else
                context.Variables["AIServiceResult"] = aiResponseMessage;

            if (aiResult?.NotifyClientPayload != null
                && _notifyClientCallback != null
                && !string.IsNullOrEmpty(_notifyClientSessionId))
            {
                _notifyClientCallback(_notifyClientSessionId, aiResult.NotifyClientPayload);
            }
        }

        public bool IsNotifyClientEnabled(Activity activity)
        {
            try
            {
                var section = activity?.SectionList?.FirstOrDefault(s => string.Equals(s?.Name, "myProperties", StringComparison.Ordinal));
                if (string.IsNullOrWhiteSpace(section?.Value)) return false;
                using var doc = JsonDocument.Parse(section.Value);
                if (doc.RootElement.TryGetProperty("isNotifyClient", out var prop))
                    return prop.ValueKind == JsonValueKind.True;
                return false;
            }
            catch
            {
                return false;
            }
        }

        public void ExecuteScriptNode(Activity activity, List<ScriptDetail> scriptDetail, AutoExecutionContext context)
        {
            // Execute script logic here
        }

        /// <summary>
        /// Execute end activity (end node).
        /// </summary>
        public void ExecuteEndActivity(Activity endActivity, AutoExecutionContext context)
        {
            Console.WriteLine($"The end activity has been executed successed!");
        }
    }
}
