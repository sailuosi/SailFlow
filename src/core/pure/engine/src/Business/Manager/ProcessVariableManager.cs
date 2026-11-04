using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using mooSQL.data;
using saiflow.data;
using saiflow.engine.Entity;
using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.engine.Event;
using saiflow.engine.HotShard;
using saiflow.engine.Xpdl;
using saiflow.hub.Localize;

namespace saiflow.engine.Manager
{
    /// <summary>
    /// 流程运行变量（wf_process_variable / 白名单热分表）。
    /// 终态：无事务从 Client 取 kit；有事务只用传入的同一个 kit。
    /// </summary>
    internal class ProcessVariableManager
    {
        public ProcessVariableManager(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        ISailHotShardRouter Router => Sail.HotShard;

        /// <summary>同库同事务的新 kit，避免链式 select 状态互相污染。</summary>
        private static SQLBuilder Sibling(SQLBuilder kit)
        {
            var next = kit.DBLive.useSQL();
            if (kit.Executor != null)
                next.useTransaction(kit.Executor);
            return next;
        }

        internal ProcessVariableEntity GetVariableById(int variableId)
            => GetVariableById(Sail.UseRunSQL(), variableId);

        internal ProcessVariableEntity GetVariableById(SQLBuilder kit, int variableId)
            => HotShardEntityAccess.GetByIdProbing<ProcessVariableEntity>(
                Sibling(kit), Router.VariableLookupTables, variableId);

        internal string GetVariable(
            ProcessVariableScopeEnum variableType,
            string name,
            EventContext eventContext)
        {
            var kit = Sail.UseRunSQL();
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
            var kit = Sail.UseRunSQL();
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
            var kit = Sail.UseRunSQL();
            return GetVariableByTask(kit, taskView, condition);
        }

        internal IDictionary<string, string> GetVariableByTask(
            SQLBuilder kit,
            TaskViewEntity taskView,
            IDictionary<string, string> condition)
        {
            var variableList = GetVariableList(kit, taskView.ProcessInstanceId.Value, taskView.ActivityInstanceId.Value);
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
            var kit = Sail.UseRunSQL();
            return GetVariableList(kit, processInstanceId);
        }

        internal IList<ProcessVariableEntity> GetVariableList(SQLBuilder kit, int processInstanceId)
        {
            var processId = ResolveProcessId(kit, processInstanceId);
            return QueryWithFallback(kit, processId, table =>
            {
                var clip = Sibling(kit).useClip();
                clip.from<ProcessVariableEntity>(table, out var v);
                clip.where(() => v.ProcessInstanceId, processInstanceId);
                clip.orderBy(() => v.ActivityId);
                return clip.select(v).queryList().ToList();
            });
        }

        internal IList<ProcessVariableEntity> GetVariableList(
            SQLBuilder kit, int processInstanceId, int activityInstanceId)
        {
            var processId = ResolveProcessId(kit, processInstanceId);
            return QueryWithFallback(kit, processId, table =>
            {
                var clip = Sibling(kit).useClip();
                clip.from<ProcessVariableEntity>(table, out var v);
                clip.where(() => v.ProcessInstanceId, processInstanceId);
                clip.where(() => v.ActivityInstanceId, activityInstanceId);
                clip.orderBy(() => v.ActivityId);
                return clip.select(v).queryList().ToList();
            });
        }

        internal ProcessVariableEntity GetVariableEntity(ProcessVariableQuery query)
        {
            var kit = Sail.UseRunSQL();
            return Query(kit, query);
        }

        internal string GetVariableValue(ProcessVariableQuery query)
        {
            var kit = Sail.UseRunSQL();
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
            var kit = Sail.UseRunSQL();
            return GetVariableByProcess(kit, processInstanceId, name);
        }

        internal ProcessVariableEntity GetVariableByProcess(SQLBuilder kit, int processInstanceId, string name)
        {
            var processId = ResolveProcessId(kit, processInstanceId);
            return QueryWithFallback(kit, processId, table =>
            {
                var clip = Sibling(kit).useClip();
                clip.from<ProcessVariableEntity>(table, out var v);
                clip.where(() => v.VariableScope, ProcessVariableScopeEnum.Process.ToString());
                clip.where(() => v.ProcessInstanceId, processInstanceId);
                clip.where(() => v.Name, name);
                clip.orderBy(() => v.ActivityId);
                return clip.select(v).queryList().ToList();
            }).FirstOrDefault();
        }

        internal ProcessVariableEntity GetVariableByActivity(
            int processInstanceId, int activityInstanceId, string name)
        {
            var kit = Sail.UseRunSQL();
            return GetVariableByActivity(kit, processInstanceId, activityInstanceId, name);
        }

        internal ProcessVariableEntity GetVariableByActivity(
            SQLBuilder kit, int processInstanceId, int activityInstanceId, string name)
        {
            var processId = ResolveProcessId(kit, processInstanceId);
            return QueryWithFallback(kit, processId, table =>
            {
                var clip = Sibling(kit).useClip();
                clip.from<ProcessVariableEntity>(table, out var v);
                clip.where(() => v.VariableScope, ProcessVariableScopeEnum.Activity.ToString());
                clip.where(() => v.ProcessInstanceId, processInstanceId);
                clip.where(() => v.ActivityInstanceId, activityInstanceId);
                clip.where(() => v.Name, name);
                clip.orderBy(() => v.Name);
                return clip.select(v).queryList().ToList();
            }).FirstOrDefault();
        }

        internal IList<ProcessVariableEntity> GetVariableListByActivity(
            int processInstanceId,
            int activityInstanceId,
            IList<string> variableNameList)
        {
            var kit = Sail.UseRunSQL();
            return GetVariableListByActivity(kit, processInstanceId, activityInstanceId, variableNameList);
        }

        internal IList<ProcessVariableEntity> GetVariableListByActivity(
            SQLBuilder kit,
            int processInstanceId,
            int activityInstanceId,
            IList<string> variableNameList)
        {
            var processId = ResolveProcessId(kit, processInstanceId);
            return QueryWithFallback(kit, processId, table =>
            {
                var clip = Sibling(kit).useClip();
                clip.from<ProcessVariableEntity>(table, out var v);
                clip.where(() => v.VariableScope, ProcessVariableScopeEnum.Activity.ToString());
                clip.where(() => v.ProcessInstanceId, processInstanceId);
                clip.where(() => v.ActivityInstanceId, activityInstanceId);

                if (variableNameList != null && variableNameList.Count == 1)
                    clip.where(() => v.Name, variableNameList[0]);
                else if (variableNameList != null && variableNameList.Count > 1)
                    clip.whereIn(() => v.Name, variableNameList);

                clip.orderBy(() => v.Name);
                return clip.select(v).queryList().ToList();
            });
        }

        internal int SaveVariable(ProcessVariableEntity entity)
        {
            var kit = Sail.UseRunSQL();
            return SaveVariable(kit, entity);
        }

        internal int SaveVariable(SQLBuilder kit, ProcessVariableEntity entity)
        {
            if (string.IsNullOrEmpty(entity.Name) || string.IsNullOrEmpty(entity.Value))
                throw new WorkflowException(Sail.Messages.GetEngineMessage("processvariablemanager.savevariable.error"));

            if (string.IsNullOrWhiteSpace(entity.ProcessId) && entity.ProcessInstanceId.HasValue)
                entity.ProcessId = ResolveProcessId(kit, entity.ProcessInstanceId.Value);

            ProcessVariableEntity existProcessVariable;
            if (entity.Id != 0)
                existProcessVariable = GetVariableById(kit, entity.Id.Value);
            else
                existProcessVariable = GetVariableByActivity(
                    kit, entity.ProcessInstanceId.Value, entity.ActivityInstanceId.Value, entity.Name);

            if (existProcessVariable != null)
            {
                existProcessVariable.Value = entity.Value;
                existProcessVariable.UpdatedDateTime = DateTime.UtcNow;
                if (string.IsNullOrWhiteSpace(existProcessVariable.ProcessId))
                    existProcessVariable.ProcessId = entity.ProcessId;
                Update(kit, existProcessVariable);
                return existProcessVariable.Id.Value;
            }

            entity.UpdatedDateTime = DateTime.UtcNow;
            return Insert(kit, entity);
        }

        internal bool ValidateProcessVariable(int processInstanceId, string expression)
        {
            var kit = Sail.UseRunSQL();
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

        private int Insert(SQLBuilder kit, ProcessVariableEntity entity)
        {
            var table = Router.ResolveVariableTable(entity?.ProcessId);
            var newId = HotShardEntityAccess.InsertReturnId(Sibling(kit), table, entity);
            entity.Id = newId;
            return newId;
        }

        private void Update(SQLBuilder kit, ProcessVariableEntity entity)
        {
            var table = Router.ResolveVariableTable(entity?.ProcessId);
            // 旧行可能仍在默认表：先试解析表，失败再探测
            try
            {
                HotShardEntityAccess.Update(Sibling(kit), table, entity);
            }
            catch
            {
                if (!Router.IsSharded(entity?.ProcessId))
                    throw;
                HotShardEntityAccess.Update(Sibling(kit), Router.DefaultVariableTable, entity);
            }
        }

        internal void DeleteVariable(int variableId)
            => DeleteVariable(Sail.UseRunSQL(), variableId);

        internal void DeleteVariable(SQLBuilder kit, int variableId)
            => HotShardEntityAccess.DeleteByIdProbing<ProcessVariableEntity>(
                Sibling(kit), Router.VariableLookupTables, variableId);

        private string ResolveProcessId(SQLBuilder kit, int processInstanceId)
            => HotShardEntityAccess.ResolveProcessId(kit, processInstanceId);

        private List<ProcessVariableEntity> QueryWithFallback(
            SQLBuilder kit,
            string processId,
            Func<string, List<ProcessVariableEntity>> query)
        {
            var primary = Router.ResolveVariableTable(processId);
            List<ProcessVariableEntity> rows;
            try
            {
                rows = query(primary) ?? new List<ProcessVariableEntity>();
            }
            catch
            {
                rows = new List<ProcessVariableEntity>();
            }

            if (!Router.IsSharded(processId))
                return rows;

            var def = Router.DefaultVariableTable;
            if (string.Equals(primary, def, StringComparison.OrdinalIgnoreCase))
                return rows;

            List<ProcessVariableEntity> legacy;
            try
            {
                legacy = query(def) ?? new List<ProcessVariableEntity>();
            }
            catch
            {
                return rows;
            }

            if (legacy.Count == 0)
                return rows;

            var seen = new HashSet<int>(rows.Where(x => x.Id.HasValue).Select(x => x.Id.Value));
            foreach (var row in legacy)
            {
                if (row.Id.HasValue && seen.Contains(row.Id.Value))
                    continue;
                rows.Add(row);
            }
            return rows;
        }
    }
}
