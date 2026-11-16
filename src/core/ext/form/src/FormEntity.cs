using System;
using mooSQL.data;

namespace sailflow.hub.Form
{
    /// <summary>流程附属表单元数据（设计器/节点字段权限），非宿主组织主数据。</summary>
    [SooTable(Name = "fm_form", Caption = "表单", Type = DBTableType.Table)]
    public class FormEntity
    {
        [SooColumn(Name = "ff_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
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

        /// <summary>授权类型（列 <c>ff_auth_type</c>）。业务侧权限过滤预留；可空非必填。</summary>
        [SooColumn(Name = "ff_auth_type", Length = 20)]
        public string AuthType { get; set; }

        /// <summary>授权编码（列 <c>ff_auth_code</c>）。业务侧权限过滤预留；可空非必填。</summary>
        [SooColumn(Name = "ff_auth_code", Length = 200)]
        public string AuthCode { get; set; }

        /// <summary>授权主体 Id（列 <c>ff_auth_id</c>）。业务侧权限过滤预留；可空非必填。</summary>
        [SooColumn(Name = "ff_auth_id", Length = 64)]
        public string AuthId { get; set; }

        /// <summary>密级（列 <c>ff_auth_level</c>）。业务侧权限过滤预留；可空非必填。</summary>
        [SooColumn(Name = "ff_auth_level")]
        public int? AuthLevel { get; set; }

        [SooColumn(Name = "ff_creator", Length = 64)]
        public string Creator { get; set; }

        [SooColumn(Name = "ff_creator_name", Length = 128)]
        public string CreatorName { get; set; }

        /// <summary>逻辑删除：0=有效，1=已删。</summary>
        [SooColumn(Name = "ff_deleted")]
        public int Deleted { get; set; }

        [SooColumn(Name = "ff_delete_time")]
        public DateTime? DeleteTime { get; set; }

        [SooColumn(Name = "ff_crt_time")]
        public DateTime? CreatedDate { get; set; }

        [SooColumn(Name = "ff_upd_time")]
        public DateTime? LastUpdatedDate { get; set; }
    }
}
