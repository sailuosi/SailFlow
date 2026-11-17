using System;
using sailflow.engine.Common;
using mooSQL.data;

namespace sailflow.engine.Entity
{
    /// <summary>
    /// 规则集定义实体（表 <c>sf_rule_set</c>）。
    /// Rule 节点经 RuleEngine 插槽执行；Mode 决定解析引擎（NRules Fluent / JSON DSL 等）。
    /// </summary>
    [SooTable(Name = "sf_rule_set", Caption = "规则集", Type = DBTableType.Table)]
    public class RuleSetEntity
    {
        /// <summary>
        /// 规则集主键（列 <c>rs_id</c>）。
        /// </summary>
        [SooColumn(Name = "rs_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string RuleSetId { get; set; }

        /// <summary>
        /// 规则集编码（列 <c>rs_code</c>），运行时按 Code 查找并执行。
        /// </summary>
        [SooColumn(Name = "rs_code", Length = 64)]
        public string RuleSetCode { get; set; }

        /// <summary>
        /// 规则集名称（列 <c>rs_name</c>）。
        /// </summary>
        [SooColumn(Name = "rs_name", Length = 255)]
        public string RuleSetName { get; set; }

        /// <summary>
        /// 规则集说明（列 <c>rs_description</c>）。
        /// </summary>
        [SooColumn(Name = "rs_description", Length = 500)]
        public string Description { get; set; }

        /// <summary>
        /// 规则正文（列 <c>rs_rule_content</c>，JSON/大字段）。
        /// 内容形态由 Mode 决定：Fluent 类型清单或 Bindings JSON DSL。
        /// </summary>
        [SooColumn(Name = "rs_rule_content", DataType = DataFam.Json, Tag = "sail.xml")]
        public string RuleContent { get; set; }

        /// <summary>
        /// 规则引擎模式（列 <c>rs_mode</c>）。
        /// 持久化字符串见 <see cref="RuleSetModeHelper"/>：
        /// <c>ruleTypes</c>（NRules Fluent）/ <c>bindingsJson</c>（JSON DSL）；空视为 ruleTypes。
        /// 自定义引擎可写其它 mode 字符串。
        /// </summary>
        [SooColumn(Name = "rs_mode", Length = 64)]
        public string Mode { get; set; }

        /// <summary>
        /// 是否启用（列 <c>rs_is_enabled</c>）：1=启用；0/空=停用。
        /// </summary>
        [SooColumn(Name = "rs_is_enabled")]
        public short? IsEnabled { get; set; }

        /// <summary>
        /// 创建时间（列 <c>rs_crt_time</c>）。
        /// </summary>
        [SooColumn(Name = "rs_crt_time")]
        public DateTime? CreatedDateTime { get; set; }

        /// <summary>
        /// 更新时间（列 <c>rs_upd_time</c>）。
        /// </summary>
        [SooColumn(Name = "rs_upd_time")]
        public DateTime? UpdatedDateTime { get; set; }
    }
}
