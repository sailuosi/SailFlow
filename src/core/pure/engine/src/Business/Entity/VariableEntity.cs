using System;
using mooSQL.data;

namespace sailflow.engine.Entity
{
    /// <summary>
    /// 流程变量定义实体（对应 BPMN sf:variables 中 Variable 属性）
    /// 表 wf_variable，仅存储变量定义（名称、类型、方向等），运行时值在 wf_proc_variable
    /// </summary>
    [SooTable(Name = "wf_variable", Caption = "流程变量定义", Type = DBTableType.Table)]
    public class VariableEntity
    {
        [SooColumn(Name = "v_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string VariableId { get; set; }
        [SooColumn(Name = "v_process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }
        [SooColumn(Name = "v_version", Length = 32)]
        public string Version { get; set; }
        [SooColumn(Name = "v_activity_id", Length = 64)]
        public string ActivityId { get; set; }
        [SooColumn(Name = "v_name", Length = 128)]
        public string Name { get; set; }
        [SooColumn(Name = "v_type", Length = 64)]
        public string Type { get; set; }
        [SooColumn(Name = "v_direction", Length = 64)]
        public string Direction { get; set; }
        [SooColumn(Name = "v_dflt_value", DataType = DataFam.Json, Tag = "sail.xml")]
        public string DefaultValue { get; set; }
        [SooColumn(Name = "v_is_required")]
        public short? IsRequired { get; set; }
        [SooColumn(Name = "v_is_ref")]
        public short? IsReferenced { get; set; }
        [SooColumn(Name = "v_src_ref", Length = 128)]
        public string SourceRef { get; set; }
        [SooColumn(Name = "v_src_var_name", Length = 128)]
        public string SourceVariableName { get; set; }
        [SooColumn(Name = "v_sort_order")]
        public int? SortOrder { get; set; }
        [SooColumn(Name = "v_description", Length = 500)]
        public string Description { get; set; }
        [SooColumn(Name = "v_crt_time")]
        public DateTime? CreatedDateTime { get; set; }
        [SooColumn(Name = "v_upd_time")]
        public DateTime? UpdatedDateTime { get; set; }
    }
}