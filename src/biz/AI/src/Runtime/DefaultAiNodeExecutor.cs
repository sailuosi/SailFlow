using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using sailflow.AI.Abstractions;
using sailflow.AI.Entity;
using sailflow.AI.Service;
using sailflow.WebUtility;
using sailflow.engine.Client;

namespace sailflow.AI.Runtime
{
    /// <summary>
    /// Default business implementation of <see cref="IAiNodeExecutor"/>.
    /// Owns LLM/RAG/Agent invocation and chat_history / ai_response variable policies.
    /// </summary>
    public sealed class DefaultAiNodeExecutor : IAiNodeExecutor
    {
        public DefaultAiNodeExecutor(SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
        }

        private readonly SailClient Sail;

        public async Task<AiNodeExecutionResult> ExecuteAsync(
            AiNodeExecutionRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.ProcessId) || string.IsNullOrWhiteSpace(request.Version))
                throw new InvalidOperationException("ProcessId and Version are required for AI service execution.");
            if (string.IsNullOrWhiteSpace(request.ActivityId))
                throw new InvalidOperationException("ActivityId is required for AI service execution.");

            var variables = request.Variables ?? new Dictionary<string, string>();
            var mediaFileList = variables
                .Select(kv => new MultiMediaFile
                {
                    Name = kv.Key,
                    Content = kv.Value ?? string.Empty,
                    MeidaType = MultiMediaTypeEnum.Text
                })
                .ToList();

            var aiModelDataService = new AiModelDataService(Sail);
            var axConfigEntity = aiModelDataService.GetAiActivityConfigByProcessVersionActivity(
                request.ProcessId, request.Version, request.ActivityId);
            if (axConfigEntity == null)
            {
                throw new InvalidOperationException(
                    $"AI activity config not found for ProcessId={request.ProcessId}, Version={request.Version}, ActivityId={request.ActivityId}.");
            }

            variables.TryGetValue("industry_name", out var industryName);
            if (!string.IsNullOrEmpty(axConfigEntity.SystemPrompt))
                axConfigEntity.SystemPrompt = axConfigEntity.SystemPrompt.Replace("{{industry_name}}", industryName ?? string.Empty);

            var history = ParseChatHistory(variables);
            var serviceType = (request.ServiceType ?? string.Empty).Trim();

            string aiResponseMessage;
            if (string.Equals(serviceType, "RAG", StringComparison.OrdinalIgnoreCase))
            {
                aiResponseMessage = await new RagMultiTurnService(Sail)
                    .InvokeWithHistoryAsync(axConfigEntity, mediaFileList, history)
                    .ConfigureAwait(false);
            }
            else if (string.Equals(serviceType, "LLM", StringComparison.OrdinalIgnoreCase))
            {
                aiResponseMessage = await new LlmMultiTurnService(Sail)
                    .InvokeWithHistoryAsync(axConfigEntity, mediaFileList, history)
                    .ConfigureAwait(false);
            }
            else if (string.Equals(serviceType, "Agent", StringComparison.OrdinalIgnoreCase))
            {
                aiResponseMessage = await new AgentMultiTurnService(Sail)
                    .InvokeWithHistoryAsync(axConfigEntity, mediaFileList, history, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                aiResponseMessage = await new AiFastCallingService(Sail)
                    .InvokeAIServiceAsync(axConfigEntity, mediaFileList)
                    .ConfigureAwait(false);
            }

            var result = new AiNodeExecutionResult
            {
                ResponseText = aiResponseMessage ?? string.Empty
            };

            variables.TryGetValue("user_message", out var userMessage);
            var isUserFacing = string.Equals(serviceType, "RAG", StringComparison.OrdinalIgnoreCase)
                || string.Equals(serviceType, "Agent", StringComparison.OrdinalIgnoreCase);

            try
            {
                var historyJson = AppendChatHistory(
                    history,
                    userMessage,
                    isUserFacing ? aiResponseMessage : null);
                if (!string.IsNullOrEmpty(historyJson))
                    result.VariableUpdates["chat_history"] = historyJson;
            }
            catch
            {
                // non-critical
            }

            if (isUserFacing)
                result.VariableUpdates["ai_response"] = aiResponseMessage ?? string.Empty;

            if (isUserFacing && request.NotifyClientEnabled)
            {
                var nodeTypeLabel = string.Equals(serviceType, "Agent", StringComparison.OrdinalIgnoreCase) ? "Agent" : "RAG";
                var completedMsg = string.Equals(serviceType, "Agent", StringComparison.OrdinalIgnoreCase)
                    ? "Agent node execution completed"
                    : "RAG node execution completed";
                result.NotifyClientPayload = new
                {
                    type = "node_response",
                    data = new
                    {
                        NodeId = request.ActivityId,
                        NodeType = nodeTypeLabel,
                        ResponseData = new { reply = aiResponseMessage },
                        Timestamp = DateTime.UtcNow,
                        Message = completedMsg
                    }
                };
            }

            return result;
        }

        private static IList<ChatHistoryMessage>? ParseChatHistory(IDictionary<string, string> variables)
        {
            if (!variables.TryGetValue("chat_history", out var json) || string.IsNullOrWhiteSpace(json))
                return null;
            try
            {
                var list = JsonSerializer.Deserialize<List<ChatHistoryMessage>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return list?.Count > 0 ? list : null;
            }
            catch
            {
                return null;
            }
        }

        private static string? AppendChatHistory(
            IList<ChatHistoryMessage>? existing,
            string? userMessage,
            string? aiResponse)
        {
            var history = existing?.ToList() ?? new List<ChatHistoryMessage>();

            if (!string.IsNullOrWhiteSpace(userMessage))
                history.Add(new ChatHistoryMessage { Role = "user", Content = userMessage });

            if (!string.IsNullOrWhiteSpace(aiResponse))
                history.Add(new ChatHistoryMessage { Role = "assistant", Content = aiResponse });

            if (history.Count == 0)
                return null;

            return JsonSerializer.Serialize(history, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }
}
