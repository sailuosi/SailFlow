using System;
using saiflow.engine.Common;
using mooSQL.data;

namespace saiflow.engine.Business.Entity
{
    /// <summary>
    /// Rule set definition entity (wf_rule_set)
    /// </summary>
    [SooTable(Name = "wf_rule_set", Caption = "规则集", Type = DBTableType.Table)]
    public class RuleSetEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int? Id { get; set; }

        [SooColumn(Name = "rule_set_code")]
        public string RuleSetCode { get; set; }

        [SooColumn(Name = "rule_set_name")]
        public string RuleSetName { get; set; }

        [SooColumn(Name = "description")]
        public string Description { get; set; }

        [SooColumn(Name = "rule_content")]
        public string RuleContent { get; set; }

        /// <summary>
        /// Persisted <c>wf_rule_set.mode</c> (<see cref="RuleSetModeHelper.PersistedRuleTypes"/> or <see cref="RuleSetModeHelper.PersistedBindingsJson"/>).
        /// </summary>
        [SooColumn(Name = "mode")]
        public string Mode { get; set; }

        [SooColumn(Name = "is_enabled")]
        public short? IsEnabled { get; set; }

        [SooColumn(Name = "created_datetime")]
        public DateTime? CreatedDateTime { get; set; }

        [SooColumn(Name = "updated_datetime")]
        public DateTime? UpdatedDateTime { get; set; }
    }
}

