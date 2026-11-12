using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sailflow.WebUtility;
using sailflow.AI.Entity;
using sailflow.engine.Client;

namespace sailflow.AI.Implementation
{
    public interface IAiLlmService
    {
        Task<ResponseResult<AIResponse>> InvokeAIChatServiceAsync(string baseUrl,
            string apiKey,
            IList<MultiMediaFile> mediaFileList,
            AiActivityConfigEntity axConfig,
            SailClient sail = null);

        /// <summary>
        /// Invokes the LLM with a pre-built message list (for multi-turn RAG; RagMultiTurnService passes messages with history).
        /// </summary>
        Task<ResponseResult<AIResponse>> InvokeAIChatServiceWithMessagesAsync(string baseUrl,
            string apiKey,
            IList<CustomApiMessage> messages,
            AiActivityConfigEntity axConfig);

        Task<string> TestConnectionAsync(string baseUrl, string apiKey, string modelName);
    }
}
