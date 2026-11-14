using mooSQL.data;
using Newtonsoft.Json;

namespace sailflow.AI.Entity
{
    /// <summary>
    /// 知识库文档（Supabase / mooSQL）。表 <c>kb_document</c>，字段前缀 <c>kd_</c>。
    /// </summary>
    [SooTable(Name = "kb_document", Caption = "知识库文档", Type = DBTableType.Table)]
    public class KbDocumentEntity
    {
        [JsonProperty("kd_id")]
        [SooColumn(Name = "kd_id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64)]
        public string Id { get; set; }

        [JsonProperty("kd_content")]
        [SooColumn(Name = "kd_content")]
        public string Content { get; set; }

        [JsonProperty("kd_metadata")]
        [SooColumn(Name = "kd_metadata")]
        public string Metadata { get; set; }

        [JsonProperty("kd_industry_id")]
        [SooColumn(Name = "kd_industry_id")]
        public long? IndustryId { get; set; }

        [JsonProperty("kd_embedding")]
        [SooColumn(Name = "kd_embedding")]
        public string Embedding { get; set; }
    }
}
