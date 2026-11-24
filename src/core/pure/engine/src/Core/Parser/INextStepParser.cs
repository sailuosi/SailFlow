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
using sailflow.engine.Assignee;
using sailflow.engine.Common;
using sailflow.engine.Hooks;
using sailflow.engine.Xpdl;
using sailflow.engine.Result;
using sailflow.engine.Client;
using sailflow.engine.Logging;
using sailflow.run;
using sailflow.engine.Factories;
using sailflow.WebUtility;

namespace sailflow.run;
/// <summary>契约 · NextStepParser（实现位于 sailflow.runtime）。</summary>
public interface INextStepParser
{
    NextStepInfo GetNextStepInfo(IResourceService resourceService, FlowCommandContext runner, IDictionary<string, string> condition = null);

    NextActivityListViewResult GetNextActivityRoleUserTree(IResourceService resourceService, FlowCommandContext context, IDictionary<string, string> condition);
}
