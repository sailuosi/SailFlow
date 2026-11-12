using mooSQL.data;

namespace sailflow.hub.Form
{
    [SooTable(Name = "FbFormFieldActivityEdit", Caption = "表单字段节点编辑", Type = DBTableType.Table)]
    public class FieldActivityEditEntity
    {
        [SooColumn(Name = "Id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64)]
        public string FieldActivityEditId { get; set; }

        [SooColumn(Name = "ProcessDefId", Length = 64)]
        public string ProcessDefId { get; set; }

        [SooColumn(Name = "ProcessId", Length = 64)]
        public string ProcessId { get; set; }

        [SooColumn(Name = "ProcessName", Length = 255)]
        public string ProcessName { get; set; }

        [SooColumn(Name = "ProcessVersion", Length = 32)]
        public string ProcessVersion { get; set; }

        [SooColumn(Name = "ActivityId", Length = 64)]
        public string ActivityId { get; set; }

        [SooColumn(Name = "ActivityName", Length = 255)]
        public string ActivityName { get; set; }

        [SooColumn(Name = "FormId", Length = 64)]
        public string FormId { get; set; }

        [SooColumn(Name = "FormName", Length = 200)]
        public string FormName { get; set; }

        [SooColumn(Name = "FormVersion", Length = 32)]
        public string FormVersion { get; set; }

        [SooColumn(Name = "FieldsPermission")]
        public string FieldsPermission { get; set; }
    }
}
