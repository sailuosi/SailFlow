using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using mooSQL.data;
using saiflow.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.SqlProvider;
using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.engine.Event;
using saiflow.engine.Xpdl;
using saiflow.hub.Localize;

namespace saiflow.engine.Business.Manager
{
    /// <summary>
    /// 流程运行变量（wf_process_variable）。
    /// 终态：无事务从 Client 取 kit；有事务只用传入的同一个 kit。
    /// </summary>
    internal class ProcessVariableManager
    {
        /// <summary>同库同事务的新 kit，避免链式 select 状态互相污染。</summary>
        private static SQLBuilder Sibling(SQLBuilder kit)
        {
            var next = kit.DBLive.useSQL();
            if (kit.Executor != null)
                next.useTransaction(kit.Executor);
            return next;
        }

        internal ProcessVariableEntity GetVariableById(int variableId)
            => SailClientAccess.UseRunSQL().useRepo<ProcessVariableEntity, int>().GetById(variableId);

        internal ProcessVariableEntity GetVariableById(SQLBuilder kit, int variableId)
            => Sibling(kit).useRepo<ProcessVariableEntity, int>().GetById(variableId);

        internal string GetVariable(
            ProcessVariableScopeEnum variableType,
            string name,
            EventContext eventContext)
        {
            var kit = SailClientAccess.UseRunSQL();
            return GetVariable(kit, variableType, name, eventContext);
        }

        internal string GetVariable(
            SQLBuilder kit,
            ProcessVariableScopeEnum variableType,
            string name,
            EventContext eventContext)
        {
            if (variableType == ProcessVariableScopeEnum.Activity)
            {
                var activityVariableEntity = GetVariableByActivity(
                    kit, eventContext.ProcessInstanceId, eventContext.ActivityInstanceId, name);
                return activityVariableEntity?.Value ?? string.Empty;
            }

            var processVariableEntity = GetVariableByProcess(kit, eventContext.ProcessInstanceId, name);
            return processVariableEntity?.Value ?? string.Empty;
        }

        internal string GetVariableByScopePriority(string name, EventContext eventContext)
        {
            var kit = SailClientAccess.UseRunSQL();
            return GetVariableByScopePriority(kit, name, eventContext);
        }

        internal string GetVariableByScopePriority(SQLBuilder kit, string name, EventContext eventContext)
        {
            var activityVariableEntity = GetVariableByActivity(
                kit, eventContext.ProcessInstanceId, eventContext.ActivityInstanceId, name);
            if (activityVariableEntity != null)
                return activityVariableEntity.Value;

            var processVariableEntity = GetVariableByProcess(kit, eventContext.ProcessInstanceId, name);
            return processVariableEntity?.Value ?? string.Empty;
        }

        internal IDictionary<string, string> GetVariableByTask(
            TaskViewEntity taskView,
            IDictionary<string, string> condition)
        {
            var kit = SailClientAccess.UseRunSQL();
            return GetVariableByTask(kit, taskView, condition);
        }

        internal IDictionary<string, string> GetVariableByTask(
            SQLBuilder kit,
            TaskViewEntity taskView,
            IDictionary<string, string> condition)
        {
            var variableList = GetVariableList(kit, taskView.ProcessInstanceId, taskView.ActivityInstanceId);
            if (variableList == null || variableList.Count == 0)
                return condition;

            foreach (var variable in variableList)
            {
                if (!condition.ContainsKey(variable.Name))
                    condition.Add(variable.Name, variable.Value);
            }
            return condition;
        }

        internal IList<ProcessVariableEntity> GetVariableList(int processInstanceId)
        {
            var kit = SailClientAccess.UseRunSQL();
            return GetVariableList(kit, processInstanceId);
        }

        internal IList<ProcessVariableEntity> GetVariableList(SQLBuilder kit, int processInstanceId)
        {
            return Sibling(kit)
                .select("*")
                .from(SailSqlDialect.ProcessVariableTable)
                .where("process_instance_id", processInstanceId)
                .orderBy("activity_id")
                .query<ProcessVariableEntity>()
                .ToList();
        }

        internal IList<ProcessVariableEntity> GetVariableList(
            SQLBuilder kit, int processInstanceId, int activityInstanceId)
        {
            return Sibling(kit)
                .select("*")
                .from(SailSqlDialect.ProcessVariableTable)
                .where("process_instance_id", processInstanceId)
                .where("activity_instance_id", activityInstanceId)
                .orderBy("activity_id")
                .query<ProcessVariableEntity>()
                .ToList();
        }

        internal ProcessVariableEntity GetVariableEntity(ProcessVariableQuery query)
        {
            var kit = SailClientAccess.UseRunSQL();
            return Query(kit, query);
        }

        internal string GetVariableValue(ProcessVariableQuery query)
        {
            var kit = SailClientAccess.UseRunSQL();
            return GetVariableValue(kit, query);
        }

        internal string GetVariableValue(SQLBuilder kit, ProcessVariableQuery query)
        {
            var entity = Query(kit, query);
            return entity?.Value;
        }

        private ProcessVariableEntity Query(SQLBuilder kit, ProcessVariableQuery query)
        {
            if (query.VariableSceope == ProcessVariableScopeEnum.Process)
                return GetVariableByProcess(kit, query.ProcessInstanceId, query.Name);
            if (query.VariableSceope == ProcessVariableScopeEnum.Activity)
                return GetVariableByActivity(kit, query.ProcessInstanceId, query.ActivityInstanceId, query.Name);
            return null;
        }

        internal ProcessVariableEntity GetVariableByProcess(int processInstanceId, string name)
        {
            var kit = SailClientAccess.UseRunSQL();
            return GetVariableByProcess(kit, processInstanceId, name);
        }

        internal ProcessVariableEntity GetVariableByProcess(SQLBuilder kit, int processInstanceId, string name)
        {
            return Sibling(kit)
                .select("*")
                .from(SailSqlDialect.ProcessVariableTable)
                .where("variable_scope", ProcessVariableScopeEnum.Process.ToString())
                .where("process_instance_id", processInstanceId)
                .where("name", name)
                .orderBy("activity_id")
                .query<ProcessVariableEntity>()
                .FirstOrDefault();
        }

        internal ProcessVariableEntity GetVariableByActivity(
            int processInstanceId, int activityInstanceId, string name)
        {
            var kit = SailClientAccess.UseRunSQL();
            return GetVariableByActivity(kit, processInstanceId, activityInstanceId, name);
        }

        internal ProcessVariableEntity GetVariableByActivity(
            SQLBuilder kit, int processInstanceId, int activityInstanceId, string name)
        {
            return Sibling(kit)
                .select("*")
                .from(SailSqlDialect.ProcessVariableTable)
                .where("variable_scope", ProcessVariableScopeEnum.Activity.ToString())
                .where("process_instance_id", processInstanceId)
                .where("activity_instance_id", activityInstanceId)
                .where("name", name)
                .orderBy("name")
                .query<ProcessVariableEntity>()
                .FirstOrDefault();
        }

        internal IList<ProcessVariableEntity> GetVariableListByActivity(
            int processInstanceId,
            int activityInstanceId,
            IList<string> variableNameList)
        {
            var kit = SailClientAccess.UseRunSQL();
            return GetVariableListByActivity(kit, processInstanceId, activityInstanceId, variableNameList);
        }

        internal IList<ProcessVariableEntity> GetVariableListByActivity(
            SQLBuilder kit,
            int processInstanceId,
            int activityInstanceId,
            IList<string> variableNameList)
        {
            var q = Sibling(kit)
                .select("*")
                .from(SailSqlDialect.ProcessVariableTable)
                .where("variable_scope", ProcessVariableScopeEnum.Activity.ToString())
                .where("process_instance_id", processInstanceId)
                .where("activity_instance_id", activityInstanceId);

            if (variableNameList != null && variableNameList.Count == 1)
                q.where("name", variableNameList[0]);
            else if (variableNameList != null && variableNameList.Count > 1)
                q.whereIn("name", variableNameList.Cast<object>().ToList());

            return q.orderBy("name").query<ProcessVariableEntity>().ToList();
        }

        internal int SaveVariable(ProcessVariableEntity entity)
        {
            var kit = SailClientAccess.UseRunSQL();
            return SaveVariable(kit, entity);
        }

        internal int SaveVariable(SQLBuilder kit, ProcessVariableEntity entity)
        {
            if (string.IsNullOrEmpty(entity.Name) || string.IsNullOrEmpty(entity.Value))
                throw new WorkflowException(LocalizeHelper.GetEngineMessage("processvariablemanager.savevariable.error"));

            ProcessVariableEntity existProcessVariable;
            if (entity.Id != 0)
                existProcessVariable = GetVariableById(kit, entity.Id);
            else
                existProcessVariable = GetVariableByActivity(
                    kit, entity.ProcessInstanceId, entity.ActivityInstanceId, entity.Name);

            if (existProcessVariable != null)
            {
                existProcessVariable.Value = entity.Value;
                existProcessVariable.UpdatedDateTime = DateTime.UtcNow;
                Update(kit, existProcessVariable);
                return existProcessVariable.Id;
            }

            entity.UpdatedDateTime = DateTime.UtcNow;
            return Insert(kit, entity);
        }

        internal bool ValidateProcessVariable(int processInstanceId, string expression)
        {
            var kit = SailClientAccess.UseRunSQL();
            return ValidateProcessVariable(kit, processInstanceId, expression);
        }

        internal bool ValidateProcessVariable(SQLBuilder kit, int processInstanceId, string expression)
        {
            var parsed = false;
            var keyValuePair = new Dictionary<string, string>();
            var regex = new Regex("(?<=@)\\w+", RegexOptions.Compiled);
            var matches = regex.Matches(expression);
            foreach (var v in matches)
            {
                var value = GetVariableByProcess(kit, processInstanceId, v.ToString());
                if (value != null)
                    keyValuePair.Add(string.Format("{0}{1}", '@', v.ToString()), value.Value);
            }

            if (keyValuePair.Count > 0)
            {
                var replaced = ExpressionParser.ReplaceParameterToValue(expression, keyValuePair);
                parsed = ExpressionParser.Parse(replaced);
            }
            return parsed;
        }

        private static int Insert(SQLBuilder kit, ProcessVariableEntity entity)
        {
            var newId = Sibling(kit).useRepo<ProcessVariableEntity, int>().InsertWithPK(entity);
            entity.Id = newId;
            return newId;
        }

        private static void Update(SQLBuilder kit, ProcessVariableEntity entity)
            => Sibling(kit).useRepo<ProcessVariableEntity>().Update(entity);

        internal void DeleteVariable(int variableId)
            => SailClientAccess.UseRunSQL().useRepo<ProcessVariableEntity, int>().DeleteById(variableId);

        internal void DeleteVariable(SQLBuilder kit, int variableId)
            => Sibling(kit).useRepo<ProcessVariableEntity, int>().DeleteById(variableId);
    }
}
