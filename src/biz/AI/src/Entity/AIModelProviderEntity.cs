using System;
using mooSQL.data;

namespace sailflow.AI.Entity
{
    [SooTable(Name = "ai_model_provider", Caption = "AI 模型提供方", Type = DBTableType.Table)]
    public class AiModelProviderEntity
    {
        [SooColumn(Name = "mp_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string Id { get; set; }
        [SooColumn(Name = "mp_prov", Length = 128)]
        public string ModelProvider { get; set; }
        [SooColumn(Name = "mp_model_name", Length = 255)]
        public string ModelName { get; set; }
        [SooColumn(Name = "mp_model_type", Length = 64)]
        public string ModelType { get; set; }
        [SooColumn(Name = "mp_description", Length = 500)]
        public string Description { get; set; }
        [SooColumn(Name = "mp_base_url", Length = 512)]
        public string BaseUrl { get; set; }
        [SooColumn(Name = "mp_api_uuid", Length = 128)]
        public string ApiUUID { get; set; }
        [SooColumn(Name = "mp_api_key", Length = 512)]
        public string ApiKey { get; set; }
        [SooColumn(Name = "mp_is_on")]
        public Boolean IsActive { get; set; }
        [SooColumn(Name = "mp_crt_time")]
        public DateTime? CreatedDateTime { get; set; }
        [SooColumn(Name = "mp_upd_time")]
        public DateTime? UpdatedDateTime { get; set; }
    }
}
