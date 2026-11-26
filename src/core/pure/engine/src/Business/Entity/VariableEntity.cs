using System;
using mooSQL.data;

namespace sailflow.data
{
    /// <summary>
    /// 流程变量定义实体（表 <c>sf_variable</c>）。
    /// 对应 BPMN <c>sf:variables</c> 中 Variable 属性；仅存名称、类型、方向等定义，
    /// 运行时值在 <see cref="ProcessVariableEntity"/>（<c>sf_proc_variable</c>）。
    /// </summary>
    [SooTable(Name = "sf_variable", Caption = "流程变量定义", Type = DBTableType.Table)]
    public class VariableEntity
    {
        /// <summary>
        /// 变量定义主键（列 <c>v_id</c>）。
        /// </summary>
        [SooColumn(Name = "v_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string VariableId { get; set; }

        /// <summary>
        /// 所属逻辑流程 Id（列 <c>v_process_id</c>）。
        /// </summary>
        [SooColumn(Name = "v_process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }

        /// <summary>
        /// 所属流程版本（列 <c>v_version</c>），与定义版本对齐。
        /// </summary>
        [SooColumn(Name = "v_version", Length = 32)]
        public string Version { get; set; }

        /// <summary>
        /// 绑定活动定义 Id（列 <c>v_activity_id</c>）；流程级变量可为空。
        /// </summary>
        [SooColumn(Name = "v_activity_id", Length = 64)]
        public string ActivityId { get; set; }

        /// <summary>
        /// 变量名（列 <c>v_name</c>）。
        /// </summary>
        [SooColumn(Name = "v_name", Length = 128)]
        public string Name { get; set; }

        /// <summary>
        /// 数据类型名（列 <c>v_type</c>）。
        /// 与 Xpdl <c>VariableDataTypeEnum</c> 对应：String/Integer/Double/Boolean/DateTime/Object 等字符串。
        /// </summary>
        [SooColumn(Name = "v_type", Length = 64)]
        public string Type { get; set; }

        /// <summary>
        /// 变量方向（列 <c>v_direction</c>）。
        /// 持久化 "Input" / "Output"（见 <c>VariableDirectionTypeEnum</c>）；输出必填项由解析器校验。
        /// </summary>
        [SooColumn(Name = "v_direction", Length = 64)]
        public string Direction { get; set; }

        /// <summary>
        /// 默认值（列 <c>v_dflt_value</c>，JSON/大字段）。
        /// </summary>
        [SooColumn(Name = "v_dflt_value", DataType = DataFam.Text, Tag = "sail.text")]
        public string DefaultValue { get; set; }

        /// <summary>
        /// 是否必填（列 <c>v_is_required</c>）：1=必填；0/空=非必填。
        /// </summary>
        [SooColumn(Name = "v_is_required")]
        public short? IsRequired { get; set; }

        /// <summary>
        /// 是否引用型变量（列 <c>v_is_ref</c>）：1=引用其它源；需配合 SourceRef / SourceVariableName。
        /// </summary>
        [SooColumn(Name = "v_is_ref")]
        public short? IsReferenced { get; set; }

        /// <summary>
        /// 引用源标识（列 <c>v_src_ref</c>），如来源活动/表单路径；IsReferenced=1 时使用。
        /// </summary>
        [SooColumn(Name = "v_src_ref", Length = 128)]
        public string SourceRef { get; set; }

        /// <summary>
        /// 引用源变量名（列 <c>v_src_var_name</c>），从源取哪个变量名。
        /// </summary>
        [SooColumn(Name = "v_src_var_name", Length = 128)]
        public string SourceVariableName { get; set; }

        /// <summary>
        /// 排序号（列 <c>v_sort_order</c>），设计器/列表展示顺序。
        /// </summary>
        [SooColumn(Name = "v_sort_order")]
        public int? SortOrder { get; set; }

        /// <summary>
        /// 变量说明（列 <c>v_description</c>）。
        /// </summary>
        [SooColumn(Name = "v_description", Length = 500)]
        public string Description { get; set; }

        /// <summary>
        /// 创建时间（列 <c>v_crt_time</c>）。
        /// </summary>
        [SooColumn(Name = "v_crt_time")]
        public DateTime? CreatedDateTime { get; set; }

        /// <summary>
        /// 更新时间（列 <c>v_upd_time</c>）。
        /// </summary>
        [SooColumn(Name = "v_upd_time")]
        public DateTime? UpdatedDateTime { get; set; }
    }
}
