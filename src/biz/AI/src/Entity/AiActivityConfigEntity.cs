using System;
using mooSQL.data;

namespace sailflow.AI.Entity
{
    [SooTable(Name = "ai_act_config", Caption = "AI 活动配置", Type = DBTableType.Table)]
    public class AiActivityConfigEntity
    {
        [SooColumn(Name = "ac_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string Id { get; set; }
        [SooColumn(Name = "ac_process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }
        [SooColumn(Name = "ac_version", Length = 32)]
        public string Version { get; set; }
        [SooColumn(Name = "ac_activity_id", Length = 64)]
        public string ActivityId { get; set; }
        [SooColumn(Name = "ac_svc_type", Length = 64)]
        public string ServiceType { get; set; }
        /// <summary>对话/主模型提供方（ai_model_provider）；同表双 FK，含义 l=llm。</summary>
        [SooColumn(Name = "ac_l_mdl_provider_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ModelProviderId { get; set; }
        [SooColumn(Name = "ac_model_name", Length = 255)]
        public string ModelName { get; set; }
        [SooColumn(Name = "ac_description", Length = 500)]
        public string Description { get; set; }
        [SooColumn(Name = "ac_temperature")]
        public decimal? Temperature { get; set; }
        [SooColumn(Name = "ac_max_tokens")]
        public int? MaxTokens { get; set; }
        [SooColumn(Name = "ac_sys_prompt", DataType = DataFam.Json, Tag = "sail.xml")]
        public string SystemPrompt { get; set; }
        [SooColumn(Name = "ac_usr_message", DataType = DataFam.Json, Tag = "sail.xml")]
        public string UserMessage { get; set; }
        [SooColumn(Name = "ac_resp_format", Length = 64)]
        public string ResponseFormat { get; set; }
        [SooColumn(Name = "ac_timeout")]
        public int? Timeout { get; set; }
        [SooColumn(Name = "ac_max_retries")]
        public int? MaxRetries { get; set; }
        [SooColumn(Name = "ac_err_handler", Length = 64)]
        public string ErrorHandling { get; set; }
        [SooColumn(Name = "ac_fb_agent", Length = 128)]
        public string FallbackAgent { get; set; }
        [SooColumn(Name = "ac_log_level", Length = 32)]
        public string LogLevel { get; set; }
        [SooColumn(Name = "ac_cstm_instr", DataType = DataFam.Json, Tag = "sail.xml")]
        public string CustomInstructions { get; set; }
        [SooColumn(Name = "ac_rag_strategy", Length = 64)]
        public string RagSearchStrategy { get; set; }
        [SooColumn(Name = "ac_rag_count")]
        public int? RagSearchCount { get; set; }
        [SooColumn(Name = "ac_rag_sim_thr")]
        public decimal? RagSimilarityThreshold { get; set; }
        [SooColumn(Name = "ac_rag_mode", Length = 64)]
        public string RagSearchMode { get; set; }
        [SooColumn(Name = "ac_rag_function", Length = 128)]
        public string RagFunction { get; set; }
        /// <summary>RAG 向量模型提供方（ai_model_provider）；同表双 FK，含义 e=embed。</summary>
        [SooColumn(Name = "ac_e_mdl_provider_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string RagEmbeddingModelId { get; set; }
        [SooColumn(Name = "ac_emb_model_name", Length = 255)]
        public string RagEmbeddingModel { get; set; }
        [SooColumn(Name = "ac_emb_dims")]
        public int? RagEmbeddingDimensions { get; set; } = 1536;
        [SooColumn(Name = "ac_mem_turns")]
        public int? MemoryTurns { get; set; } = 10;
        [SooColumn(Name = "ac_crt_time")]
        public DateTime? CreatedDateTime { get; set; }
        [SooColumn(Name = "ac_upd_time")]
        public DateTime? UpdatedDateTime { get; set; }
    }
}
