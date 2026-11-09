using System;
using mooSQL.data;

namespace saiflow.AI.Entity
{
    [SooTable(Name = "ai_model_provider", Caption = "AI 模型提供方", Type = DBTableType.Table)]
    public class AiModelProviderEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64)]
        public string Id { get; set; }
        [SooColumn(Name = "model_provider")]
        public string ModelProvider { get; set; }
        [SooColumn(Name = "model_name")]
        public string ModelName { get; set; }
        [SooColumn(Name = "model_type")]
        public string ModelType { get; set; }
        [SooColumn(Name = "description")]
        public string Description { get; set; }
        [SooColumn(Name = "base_url")]
        public string BaseUrl { get; set; }
        [SooColumn(Name = "api_uuid")]
        public string ApiUUID { get; set; }
        [SooColumn(Name = "api_key")]
        public string ApiKey { get; set; }
        [SooColumn(Name = "is_active")]
        public Boolean IsActive { get; set; }
        [SooColumn(Name = "created_datetime")]
        public DateTime? CreatedDateTime { get; set; }
        [SooColumn(Name = "updated_datetime")]
        public DateTime? UpdatedDateTime { get; set; }
    }
}
