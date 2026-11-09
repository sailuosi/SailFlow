using mooSQL.data;

namespace saiflow.AI.Entity
{
    /// <summary>
    /// Knowledge Base Document Entity
    /// 知识库文档实体
    /// </summary>
    [SooTable(Name = "biz_documents", Caption = "知识库文档", Type = DBTableType.Table)]
    public class KbDocumentEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64)]
        public string Id { get; set; }

        [SooColumn(Name = "content")]
        public string Content { get; set; }

        [SooColumn(Name = "metadata")]
        public string Metadata { get; set; }

        [SooColumn(Name = "industry_id")]
        public long? IndustryId { get; set; }

        [SooColumn(Name = "embedding")]
        public string Embedding { get; set; }
    }
}
