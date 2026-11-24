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
using sailflow.xml;
using sailflow.engine.Result;
using sailflow.engine.Client;
using sailflow.log;
using sailflow.run;
using sailflow.engine.Factories;
using sailflow.WebUtility;

namespace sailflow.data;

/// <summary>契约 · VariableManager（实现位于 sailflow.runtime）。</summary>
public interface IVariableManager
{
    IList<VariableEntity> GetList(string processId, string version, string activityId);

    VariableEntity GetById(string id);

    string Insert(VariableEntity entity);

    string Insert(SQLBuilder kit, VariableEntity entity);

    void Update(VariableEntity entity);

    void Update(SQLBuilder kit, VariableEntity entity);

    void Delete(string id);

    void Delete(SQLBuilder kit, string id);

    void DeleteByProcessActivity(string processId, string version, string activityId);

    void SaveList(string processId, string version, string activityId, IList<VariableEntity> list);

    void SaveList(SQLBuilder kit, string processId, string version, string activityId, IList<VariableEntity> list);
}
