using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using sailflow.data;
using sailflow.hub.Localize;
using sailflow.hub.Resource;
using sailflow.assignee;
using sailflow.engine.Common;
using sailflow.hooks;
using sailflow.engine.Xpdl;
using sailflow.engine.Result;
using sailflow.engine.Client;
using sailflow.log;
using sailflow.run;
using sailflow.engine.Factories;
using sailflow.WebUtility;

namespace sailflow.data;

/// <summary>契约 · RuleSetManager（实现位于 sailflow.runtime）。</summary>
public interface IRuleSetManager
{
    IList<RuleSetEntity> GetAll();

    RuleSetEntity GetByCode(string ruleSetCode);

    string Insert(RuleSetEntity entity);

    string Insert(SQLBuilder kit, RuleSetEntity entity);

    void Update(RuleSetEntity entity);

    void Update(SQLBuilder kit, RuleSetEntity entity);

    void DeleteByCode(string ruleSetCode);

    void DeleteByCode(SQLBuilder kit, string ruleSetCode);
}
