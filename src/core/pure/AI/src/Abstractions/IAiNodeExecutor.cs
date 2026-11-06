using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace saiflow.AI.Abstractions
{
    /// <summary>
    /// Contract for executing an AI workflow node.
    /// Engine only schedules; concrete LLM/RAG/Agent logic lives in business implementations (e.g. sailflow.biz.AI).
    /// </summary>
    public interface IAiNodeExecutor
    {
        Task<AiNodeExecutionResult> ExecuteAsync(AiNodeExecutionRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Input assembled by the workflow engine for an AIServiceNode.
    /// </summary>
    public sealed class AiNodeExecutionRequest
    {
        /// <summary>Logical type: LLM / RAG / Agent（见 <see cref="AiServiceTypeEnum"/> 名称）。</summary>
        public string ServiceType { get; set; } = string.Empty;

        public string ProcessId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string ActivityId { get; set; } = string.Empty;

        /// <summary>Optional tool-set name from BPMN Agent node properties.</summary>
        public string? ToolSetName { get; set; }

        /// <summary>Current process/activity variables (name → string content).</summary>
        public IDictionary<string, string> Variables { get; set; }
            = new Dictionary<string, string>();

        /// <summary>Whether the designer enabled client notification for this node.</summary>
        public bool NotifyClientEnabled { get; set; }
    }

    /// <summary>
    /// Result returned to the engine for variable write-back and optional client notify.
    /// </summary>
    public sealed class AiNodeExecutionResult
    {
        public string ResponseText { get; set; } = string.Empty;

        /// <summary>
        /// Extra variable updates (e.g. chat_history, ai_response). Merged into process variables by the engine.
        /// </summary>
        public IDictionary<string, string> VariableUpdates { get; set; }
            = new Dictionary<string, string>();

        /// <summary>Optional payload for host notify-client callback; engine passes through when present.</summary>
        public object? NotifyClientPayload { get; set; }
    }
}
