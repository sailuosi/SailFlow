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
using sailflow.engine.Entity;
using sailflow.engine.Xpdl;
using sailflow.engine.Result;
using sailflow.engine.Client;
using sailflow.engine.Logging;
using sailflow.engine.Runtime;
using sailflow.engine.Core;
using sailflow.engine.Manager;
using sailflow.engine.Factories;
using sailflow.WebUtility;

namespace sailflow.engine.Runtime;

/// <summary>契约 · SignForwardStepMaker（实现位于 sailflow.runtime）。</summary>
public interface ISignForwardStepMaker
{
    SignForwardStepInfo GetSignForwardStepInfo(FlowCommandContext runner);

    NextActivityListViewResult GetSignForwardRoleUserTree(FlowCommandContext runner);
}
