using System;
using mooSQL.data;

namespace sailflow.hub.Form
{
    /// <summary>流程附属表单元数据（设计器/节点字段权限），非宿主组织主数据。</summary>
    [SooTable(Name = "fm_form", Caption = "表单", Type = DBTableType.Table)]
    public class FormEntity
    {
        [SooColumn(Name = "ff_id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64, Tag = "sail.id")]
        public string FormId { get; set; }

        [SooColumn(Name = "ff_name", Length = 200)]
        public string FormName { get; set; }

        [SooColumn(Name = "ff_code", Length = 100)]
        public string FormCode { get; set; }

        [SooColumn(Name = "ff_version", Length = 32)]
        public string Version { get; set; }

        [SooColumn(Name = "ff_fld_summary", DataType = DataFam.Json, Tag = "sail.xml")]
        public string FieldSummary { get; set; }

        [SooColumn(Name = "ff_template", DataType = DataFam.Json, Tag = "sail.xml")]
        public string TemplateContent { get; set; }

        [SooColumn(Name = "ff_html", DataType = DataFam.Json, Tag = "sail.xml")]
        public string HTMLContent { get; set; }

        [SooColumn(Name = "ff_description", Length = 500)]
        public string Description { get; set; }

        [SooColumn(Name = "ff_crt_time")]
        public DateTime? CreatedDate { get; set; }

        [SooColumn(Name = "ff_upd_time")]
        public DateTime? LastUpdatedDate { get; set; }
    }
}
