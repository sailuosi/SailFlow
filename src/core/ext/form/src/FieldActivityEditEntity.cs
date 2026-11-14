using mooSQL.data;

namespace sailflow.hub.Form
{
    [SooTable(Name = "fm_form_act_edit", Caption = "表单字段节点编辑", Type = DBTableType.Table)]
    public class FieldActivityEditEntity
    {
        [SooColumn(Name = "fa_id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64, Tag = "sail.id")]
        public string FieldActivityEditId { get; set; }

        /// <summary>流程定义主键（wf_process.p_id）。</summary>
        [SooColumn(Name = "fa_process_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ProcessDefId { get; set; }

        [SooColumn(Name = "fa_proc_biz_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }

        [SooColumn(Name = "fa_proc_name", Length = 255)]
        public string ProcessName { get; set; }

        [SooColumn(Name = "fa_proc_version", Length = 32)]
        public string ProcessVersion { get; set; }

        [SooColumn(Name = "fa_activity_id", Length = 64)]
        public string ActivityId { get; set; }

        [SooColumn(Name = "fa_activity_name", Length = 255)]
        public string ActivityName { get; set; }

        [SooColumn(Name = "fa_form_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string FormId { get; set; }

        [SooColumn(Name = "fa_form_name", Length = 200)]
        public string FormName { get; set; }

        [SooColumn(Name = "fa_form_version", Length = 32)]
        public string FormVersion { get; set; }

        [SooColumn(Name = "fa_fld_perm", DataType = DataFam.Json, Tag = "sail.xml")]
        public string FieldsPermission { get; set; }
    }
}
