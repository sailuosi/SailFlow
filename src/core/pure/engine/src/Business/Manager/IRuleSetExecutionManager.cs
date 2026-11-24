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
using sailflow.utils;
using sailflow.hooks;
using sailflow.xml;
using sailflow.engine.Result;
using sailflow.engine.Client;
using sailflow.log;
using sailflow.run;
using sailflow.engine.Factories;

namespace sailflow.data;

/// <summary>契约 · RuleSetExecutionManager（实现位于 sailflow.runtime）。</summary>
public interface IRuleSetExecutionManager
{
    RuleExecutionResultEntity Execute(RuleSetEntity ruleSet, IDictionary<string, object> variables);

    bool ValidateRuleContent(string mode, string ruleContent, out string message);
}
