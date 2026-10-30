using System;
using mooSQL.data;

namespace saiflow.AI.Entity
{
    [SooTable(Name = "ai_activity_config", Caption = "AI 活动配置", Type = DBTableType.Table)]
    public class AiActivityConfigEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        [SooColumn(Name = "process_id")]
        public string ProcessId { get; set; }
        [SooColumn(Name = "version")]
        public string Version { get; set; }
        [SooColumn(Name = "activity_id")]
        public string ActivityId { get; set; }
        [SooColumn(Name = "service_type")]
        public string ServiceType { get; set; }
        [SooColumn(Name = "model_provider_id")]
        public int? ModelProviderId { get; set; }
        [SooColumn(Name = "model_name")]
        public string ModelName { get; set; }
        [SooColumn(Name = "description")]
        public string Description { get; set; }
        [SooColumn(Name = "temperature")]
        public decimal Temperature { get; set; }
        [SooColumn(Name = "max_tokens")]
        public int MaxTokens { get; set; }
        [SooColumn(Name = "system_prompt")]
        public string SystemPrompt { get; set; }
        [SooColumn(Name = "user_message")]
        public string UserMessage { get; set; }
        [SooColumn(Name = "response_format")]
        public string ResponseFormat { get; set; }
        [SooColumn(Name = "time_out")]
        public int Timeout { get; set; }
        [SooColumn(Name = "max_retries")]
        public int MaxRetries { get; set; }
        [SooColumn(Name = "error_handling")]
        public string ErrorHandling { get; set; }
        [SooColumn(Name = "fallback_agent")]
        public string FallbackAgent { get; set; }
        [SooColumn(Name = "log_level")]
        public string LogLevel { get; set; }
        [SooColumn(Name = "custom_instructions")]
        public string CustomInstructions { get; set; }
        [SooColumn(Name = "rag_search_strategy")]
        public string RagSearchStrategy { get; set; }
        [SooColumn(Name = "rag_search_count")]
        public int? RagSearchCount { get; set; }
        [SooColumn(Name = "rag_similarity_threshold")]
        public decimal? RagSimilarityThreshold { get; set; }
        [SooColumn(Name = "rag_search_mode")]
        public string RagSearchMode { get; set; }
        [SooColumn(Name = "rag_function")]
        public string RagFunction { get; set; }
        [SooColumn(Name = "rag_embedding_model_id")]
        public int? RagEmbeddingModelId { get; set; }
        [SooColumn(Name = "rag_embedding_model")]
        public string RagEmbeddingModel { get; set; }
        [SooColumn(Name = "rag_embedding_dimensions")]
        public int? RagEmbeddingDimensions { get; set; } = 1536;
        [SooColumn(Name = "memory_turns")]
        public int MemoryTurns { get; set; } = 10;
        [SooColumn(Name = "created_datetime")]
        public DateTime CreatedDateTime { get; set; }
        [SooColumn(Name = "updated_datetime")]
        public DateTime UpdatedDateTime { get; set; }
    }
}
