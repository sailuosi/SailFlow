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
using sailflow.hosting;
using sailflow.log;
using sailflow.run;
using sailflow.factory;

namespace sailflow.data;

/// <summary>契约 · ProcessVariableResolver（实现位于 sailflow.runtime）。</summary>
public interface IProcessVariableResolver
{
    bool CheckVariableRequired( SQLBuilder kit, TaskViewEntity taskView, Activity activity, IDictionary<string, string> conditions);
}
