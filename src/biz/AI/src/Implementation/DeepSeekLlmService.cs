using sailflow.AI.Entity;
using sailflow.WebUtility;
using sailflow.engine.Client;

namespace sailflow.AI.Implementation
{
    /// <summary>
    /// DeepSeek LLM 服务。
    /// DeepSeek API 与 OpenAI 完全兼容，直接委托给 OpenAILlmService。
    /// 端点：https://api.deepseek.com/v1/chat/completions
    /// 模型：deepseek-chat / deepseek-reasoner
    /// </summary>
    public class DeepSeekLlmService : IAiLlmService
    {
        private readonly OpenAILlmService _inner = new();

        public Task<ResponseResult<AIResponse>> InvokeAIChatServiceAsync(
            string baseUrl,
            string apiKey,
            IList<MultiMediaFile> mediaFileList,
            AiActivityConfigEntity axConfig,
            SailClient sail = null)
            => _inner.InvokeAIChatServiceAsync(baseUrl, apiKey, mediaFileList, axConfig, sail);

        public Task<ResponseResult<AIResponse>> InvokeAIChatServiceWithMessagesAsync(
            string baseUrl,
            string apiKey,
            IList<CustomApiMessage> messages,
            AiActivityConfigEntity axConfig)
            => _inner.InvokeAIChatServiceWithMessagesAsync(baseUrl, apiKey, messages, axConfig);

        public Task<string> TestConnectionAsync(string baseUrl, string apiKey, string modelName)
            => _inner.TestConnectionAsync(baseUrl, apiKey, modelName);
    }
}
