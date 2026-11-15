using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sailflow.engine.Common;
using mooSQL.data;

namespace sailflow.engine.Entity
{
    /// <summary>
    /// 流程变量运行时实体（表 <c>wf_proc_variable</c>）。
    /// 存实例/活动作用域下的变量当前值；定义元数据在 <see cref="VariableEntity"/>。
    /// </summary>
    [SooTable(Name = "wf_proc_variable", Caption = "流程变量运行时", Type = DBTableType.Table)]
    public class ProcessVariableEntity
    {
        /// <summary>
        /// 运行时变量行主键（列 <c>pv_id</c>）。
        /// </summary>
        [SooColumn(Name = "pv_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string ProcessVariableId { get; set; }

        /// <summary>
        /// 变量作用域（列 <c>pv_var_scope</c>），存枚举名字符串，见 <see cref="ProcessVariableScopeEnum"/>。
        /// System / Process / Activity；查询与写入按此 + 实例/活动 Id 定位。
        /// </summary>
        [SooColumn(Name = "pv_var_scope", Length = 64)]
        public string VariableScope { get; set; }

        /// <summary>
        /// 业务主键冗余（列 <c>pv_biz_pk</c>），便于按单据查变量。
        /// </summary>
        [SooColumn(Name = "pv_biz_pk", Length = 128)]
        public string BizPK { get; set; }

        /// <summary>
        /// 逻辑流程 Id（列 <c>pv_process_id</c>）。
        /// </summary>
        [SooColumn(Name = "pv_process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }

        /// <summary>
        /// 流程实例 Id（列 <c>pv_proc_instance_id</c>）；Process 作用域必填。
        /// </summary>
        [SooColumn(Name = "pv_proc_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ProcessInstanceId { get; set; }

        /// <summary>
        /// 活动实例 Id（列 <c>pv_act_instance_id</c>）；Activity 作用域必填。
        /// </summary>
        [SooColumn(Name = "pv_act_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ActivityInstanceId { get; set; }

        /// <summary>
        /// 活动定义 Id（列 <c>pv_activity_id</c>），活动作用域辅助定位。
        /// </summary>
        [SooColumn(Name = "pv_activity_id", Length = 64)]
        public string ActivityId { get; set; }

        /// <summary>
        /// 活动名称快照（列 <c>pv_activity_name</c>）。
        /// </summary>
        [SooColumn(Name = "pv_activity_name", Length = 255)]
        public string ActivityName { get; set; }

        /// <summary>
        /// 变量名（列 <c>pv_name</c>），与定义侧 Name 对应。
        /// </summary>
        [SooColumn(Name = "pv_name", Length = 128)]
        public string Name { get; set; }

        /// <summary>
        /// 变量值（列 <c>pv_value</c>，JSON/大字段），按 MediaType 解释。
        /// </summary>
        [SooColumn(Name = "pv_value", DataType = DataFam.Json, Tag = "sail.xml")]
        public string Value { get; set; }

        /// <summary>
        /// 值媒体类型（列 <c>pv_media_type</c>），如 text/json 等，指示 Value 如何反序列化。
        /// </summary>
        [SooColumn(Name = "pv_media_type", Length = 64)]
        public string MediaType { get; set; }

        /// <summary>
        /// 最后更新时间（列 <c>pv_upd_time</c>）。
        /// </summary>
        [SooColumn(Name = "pv_upd_time")]
        public DateTime? UpdatedDateTime { get; set; }
    }
}
