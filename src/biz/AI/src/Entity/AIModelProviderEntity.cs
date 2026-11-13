using System;
using mooSQL.data;

namespace sailflow.AI.Entity
{
    [SooTable(Name = "ai_model_provider", Caption = "AI 模型提供方", Type = DBTableType.Table)]
    public class AiModelProviderEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64, Tag = "sail.id")]
        public string Id { get; set; }
        [SooColumn(Name = "model_provider", Length = 128)]
        public string ModelProvider { get; set; }
        [SooColumn(Name = "model_name", Length = 255)]
        public string ModelName { get; set; }
        [SooColumn(Name = "model_type", Length = 64)]
        public string ModelType { get; set; }
        [SooColumn(Name = "description", Length = 500)]
        public string Description { get; set; }
        [SooColumn(Name = "base_url", Length = 512)]
        public string BaseUrl { get; set; }
        [SooColumn(Name = "api_uuid", Length = 128)]
        public string ApiUUID { get; set; }
        [SooColumn(Name = "api_key", Length = 512)]
        public string ApiKey { get; set; }
        [SooColumn(Name = "is_active")]
        public Boolean IsActive { get; set; }
        [SooColumn(Name = "created_datetime")]
        public DateTime? CreatedDateTime { get; set; }
        [SooColumn(Name = "updated_datetime")]
        public DateTime? UpdatedDateTime { get; set; }
    }
}
