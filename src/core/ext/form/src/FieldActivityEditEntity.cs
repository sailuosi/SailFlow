using mooSQL.data;

namespace saiflow.hub.Form
{
    [SooTable(Name = "FbFormFieldActivityEdit", Caption = "表单字段节点编辑", Type = DBTableType.Table, IsColumnAttributeRequired = false)]
    public class FieldActivityEditEntity
    {
        [SooColumn(Name = "Id", IsPrimaryKey = true, IsIdentity = true)]
        public int? Id { get; set; }
        public int? ProcessDefId { get; set; }
        public string ProcessId { get; set; }
        public string ProcessName { get; set; }
        public string ProcessVersion { get; set; }
        public string ActivityId { get; set; }
        public string ActivityName { get; set; }
        public int? FormId { get; set; }
        public string FormName { get; set; }
        public string FormVersion { get; set; }
        public string FieldsPermission { get; set; }
    }
}
