using System;
using mooSQL.data;

namespace sailflow.hub.Form
{
    /// <summary>流程附属表单元数据（设计器/节点字段权限），非宿主组织主数据。</summary>
    [SooTable(Name = "FbForm", Caption = "表单", Type = DBTableType.Table)]
    public class FormEntity
    {
        [SooColumn(Name = "Id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64, Tag = "sail.id")]
        public string FormId { get; set; }

        [SooColumn(Name = "FormName", Length = 200)]
        public string FormName { get; set; }

        [SooColumn(Name = "FormCode", Length = 100)]
        public string FormCode { get; set; }

        [SooColumn(Name = "Version", Length = 32)]
        public string Version { get; set; }

        [SooColumn(Name = "FieldSummary", DataType = DataFam.Json, Tag = "sail.xml")]
        public string FieldSummary { get; set; }

        [SooColumn(Name = "TemplateContent", DataType = DataFam.Json, Tag = "sail.xml")]
        public string TemplateContent { get; set; }

        [SooColumn(Name = "HTMLContent", DataType = DataFam.Json, Tag = "sail.xml")]
        public string HTMLContent { get; set; }

        [SooColumn(Name = "Description", Length = 500)]
        public string Description { get; set; }

        [SooColumn(Name = "CreatedDate")]
        public DateTime? CreatedDate { get; set; }

        [SooColumn(Name = "LastUpdatedDate")]
        public DateTime? LastUpdatedDate { get; set; }
    }
}
