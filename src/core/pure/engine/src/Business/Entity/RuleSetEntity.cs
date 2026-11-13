using System;
using sailflow.engine.Common;
using mooSQL.data;

namespace sailflow.engine.Entity
{
    /// <summary>
    /// Rule set definition entity (wf_rule_set)
    /// </summary>
    [SooTable(Name = "wf_rule_set", Caption = "规则集", Type = DBTableType.Table)]
    public class RuleSetEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64, Tag = "sail.id")]
        public string RuleSetId { get; set; }

        [SooColumn(Name = "rule_set_code", Length = 64)]
        public string RuleSetCode { get; set; }

        [SooColumn(Name = "rule_set_name", Length = 255)]
        public string RuleSetName { get; set; }

        [SooColumn(Name = "description", Length = 500)]
        public string Description { get; set; }

        [SooColumn(Name = "rule_content", DataType = DataFam.Json, Tag = "sail.xml")]
        public string RuleContent { get; set; }

        /// <summary>
        /// Persisted <c>wf_rule_set.mode</c> (<see cref="RuleSetModeHelper.PersistedRuleTypes"/> or <see cref="RuleSetModeHelper.PersistedBindingsJson"/>).
        /// </summary>
        [SooColumn(Name = "mode", Length = 64)]
        public string Mode { get; set; }

        [SooColumn(Name = "is_enabled")]
        public short? IsEnabled { get; set; }

        [SooColumn(Name = "created_datetime")]
        public DateTime? CreatedDateTime { get; set; }

        [SooColumn(Name = "updated_datetime")]
        public DateTime? UpdatedDateTime { get; set; }
    }
}
