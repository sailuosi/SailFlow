using System.Collections.Generic;
using mooSQL.data;
using sailflow.data;

namespace sailflow.storage
{
    /// <summary>规则集表纯库（<c>sf_rule_set</c>；kit 首参）。</summary>
    public interface IRuleSetStorage
    {
        IList<RuleSetEntity> GetAll(SQLBuilder kit);

        RuleSetEntity GetByCode(SQLBuilder kit, string ruleSetCode);

        string Insert(SQLBuilder kit, RuleSetEntity entity);

        void Update(SQLBuilder kit, RuleSetEntity entity);

        void DeleteByCode(SQLBuilder kit, string ruleSetCode);
    }
}
