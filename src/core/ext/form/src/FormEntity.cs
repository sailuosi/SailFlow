using System;
using mooSQL.data;

namespace saiflow.hub.Form
{
    [SooTable(Name = "FbForm", Caption = "表单", Type = DBTableType.Table, IsColumnAttributeRequired = false)]
    public class FormEntity
    {
        [SooColumn(Name = "Id", IsPrimaryKey = true, IsIdentity = true)]
        public int? Id { get; set; }
        public string FormName { get; set; }
        public string FormCode { get; set; }
        public string Version { get; set; }
        public string FieldSummary { get; set; }
        public string TemplateContent { get; set; }
        public string HTMLContent { get; set; }
        public string Description { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
    }
}
