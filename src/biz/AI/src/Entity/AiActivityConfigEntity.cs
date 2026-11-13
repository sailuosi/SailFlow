using System;
using mooSQL.data;

namespace sailflow.AI.Entity
{
    [SooTable(Name = "ai_activity_config", Caption = "AI 活动配置", Type = DBTableType.Table)]
    public class AiActivityConfigEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64, Tag = "sail.id")]
        public string Id { get; set; }
        [SooColumn(Name = "process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }
        [SooColumn(Name = "version", Length = 32)]
        public string Version { get; set; }
        [SooColumn(Name = "activity_id", Length = 64)]
        public string ActivityId { get; set; }
        [SooColumn(Name = "service_type", Length = 64)]
        public string ServiceType { get; set; }
        [SooColumn(Name = "model_provider_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ModelProviderId { get; set; }
        [SooColumn(Name = "model_name", Length = 255)]
        public string ModelName { get; set; }
        [SooColumn(Name = "description", Length = 500)]
        public string Description { get; set; }
        [SooColumn(Name = "temperature")]
        public decimal? Temperature { get; set; }
        [SooColumn(Name = "max_tokens")]
        public int? MaxTokens { get; set; }
        [SooColumn(Name = "system_prompt", DataType = DataFam.Json, Tag = "sail.xml")]
        public string SystemPrompt { get; set; }
        [SooColumn(Name = "user_message", DataType = DataFam.Json, Tag = "sail.xml")]
        public string UserMessage { get; set; }
        [SooColumn(Name = "response_format", Length = 64)]
        public string ResponseFormat { get; set; }
        [SooColumn(Name = "time_out")]
        public int? Timeout { get; set; }
        [SooColumn(Name = "max_retries")]
        public int? MaxRetries { get; set; }
        [SooColumn(Name = "error_handling", Length = 64)]
        public string ErrorHandling { get; set; }
        [SooColumn(Name = "fallback_agent", Length = 128)]
        public string FallbackAgent { get; set; }
        [SooColumn(Name = "log_level", Length = 32)]
        public string LogLevel { get; set; }
        [SooColumn(Name = "custom_instructions", DataType = DataFam.Json, Tag = "sail.xml")]
        public string CustomInstructions { get; set; }
        [SooColumn(Name = "rag_search_strategy", Length = 64)]
        public string RagSearchStrategy { get; set; }
        [SooColumn(Name = "rag_search_count")]
        public int? RagSearchCount { get; set; }
        [SooColumn(Name = "rag_similarity_threshold")]
        public decimal? RagSimilarityThreshold { get; set; }
        [SooColumn(Name = "rag_search_mode", Length = 64)]
        public string RagSearchMode { get; set; }
        [SooColumn(Name = "rag_function", Length = 128)]
        public string RagFunction { get; set; }
        [SooColumn(Name = "rag_embedding_model_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string RagEmbeddingModelId { get; set; }
        [SooColumn(Name = "rag_embedding_model", Length = 255)]
        public string RagEmbeddingModel { get; set; }
        [SooColumn(Name = "rag_embedding_dimensions")]
        public int? RagEmbeddingDimensions { get; set; } = 1536;
        [SooColumn(Name = "memory_turns")]
        public int? MemoryTurns { get; set; } = 10;
        [SooColumn(Name = "created_datetime")]
        public DateTime? CreatedDateTime { get; set; }
        [SooColumn(Name = "updated_datetime")]
        public DateTime? UpdatedDateTime { get; set; }
    }
}
