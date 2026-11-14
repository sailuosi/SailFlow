using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using sailflow.data;
using sailflow.engine.Entity;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.engine.Event;
using sailflow.engine.HotShard;
using sailflow.engine.Xpdl;
using sailflow.hub.Localize;

namespace sailflow.engine.Manager
{
    /// <summary>
    /// 流程运行变量（wf_proc_variable / 白名单热分表）。
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
        internal ProcessVariableEntity GetVariableById(string processId, string variableId)
            => GetVariableById(Sail.UseRunSQL(), processId, variableId);

        internal ProcessVariableEntity GetVariableById(SQLBuilder kit, string processId, string variableId)
        {
            //此处不做特殊判断，由Router保证返回的表有效！
            var table = Router.ResolveVariableTable(processId);
  
            var row = kit.findRowById<ProcessVariableEntity>(variableId, table );
            return row;

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

        internal IList<ProcessVariableEntity> GetVariableList(string processInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetVariableList(kit, processInstanceId);
        }

        internal IList<ProcessVariableEntity> GetVariableList(SQLBuilder kit, string processInstanceId)
        {
            var processId = ResolveProcessId(kit, processInstanceId);
            return QueryWithFallback(kit, processId, table =>
            {
                var clip = kit.useClip();
                clip.from<ProcessVariableEntity>(table, out var v)
                    .where(() => v.ProcessInstanceId, processInstanceId)
                    .orderBy(() => v.ActivityId);
                return clip.select(v).queryList().ToList();
            });
        }

        internal IList<ProcessVariableEntity> GetVariableList(
            SQLBuilder kit, string processInstanceId, string activityInstanceId)
        {
            var processId = ResolveProcessId(kit, processInstanceId);
            return QueryWithFallback(kit, processId, table =>
            {
                var clip = kit.useClip();
                clip.from<ProcessVariableEntity>(table, out var v)
                    .where(() => v.ProcessInstanceId, processInstanceId)
                    .where(() => v.ActivityInstanceId, activityInstanceId)
                    .orderBy(() => v.ActivityId);
                return clip.select(v).queryList().ToList();
            });
        }

        internal ProcessVariableEntity GetVariableEntity(ProcessVariableQuery query)
        {
            var kit = Sail.UseRunSQL();
            return Query(kit, query);
        }


        private ProcessVariableEntity Query(SQLBuilder kit, ProcessVariableQuery query)
        {
            if (query.VariableSceope == ProcessVariableScopeEnum.Process)
                return GetVariableByProcess(kit, query.ProcessInstanceId, query.Name);
            if (query.VariableSceope == ProcessVariableScopeEnum.Activity)
                return GetVariableByActivity(kit, query.ProcessInstanceId, query.ActivityInstanceId, query.Name);
            return null;
        }


        internal ProcessVariableEntity GetVariableByProcess(SQLBuilder kit, string processInstanceId, string name)
        {
            var processId = ResolveProcessId(kit, processInstanceId);
            return QueryWithFallback(kit, processId, table =>
            {
                var clip = kit.useClip();
                clip.from<ProcessVariableEntity>(table, out var v)
                    .where(() => v.VariableScope, ProcessVariableScopeEnum.Process.ToString())
                    .where(() => v.ProcessInstanceId, processInstanceId)
                    .where(() => v.Name, name)
                    .orderBy(() => v.ActivityId);
                return clip.select(v).queryList().ToList();
            }).FirstOrDefault();
        }


        internal ProcessVariableEntity GetVariableByActivity(
            SQLBuilder kit, string processInstanceId, string activityInstanceId, string name)
        {
            var processId = ResolveProcessId(kit, processInstanceId);
            return QueryWithFallback(kit, processId, table =>
            {
                var clip = kit.useClip();
                clip.from<ProcessVariableEntity>(table, out var v)
                    .where(() => v.VariableScope, ProcessVariableScopeEnum.Activity.ToString())
                    .where(() => v.ProcessInstanceId, processInstanceId)
                    .where(() => v.ActivityInstanceId, activityInstanceId)
                    .where(() => v.Name, name)
                    .orderBy(() => v.Name);
                return clip.select(v).queryList().ToList();
            }).FirstOrDefault();
        }


        internal IList<ProcessVariableEntity> GetVariableListByActivity(
            SQLBuilder kit,
            string processInstanceId,
            string activityInstanceId,
            IList<string> variableNameList)
        {
            var processId = ResolveProcessId(kit, processInstanceId);
            return QueryWithFallback(kit, processId, table =>
            {
                var clip = kit.useClip();
                clip.from<ProcessVariableEntity>(table, out var v)
                    .where(() => v.VariableScope, ProcessVariableScopeEnum.Activity.ToString())
                    .where(() => v.ProcessInstanceId, processInstanceId)
                    .where(() => v.ActivityInstanceId, activityInstanceId);

                if (variableNameList != null && variableNameList.Count == 1)
                    clip.where(() => v.Name, variableNameList[0]);
                else if (variableNameList != null && variableNameList.Count > 1)
                    clip.whereIn(() => v.Name, variableNameList);

                clip.orderBy(() => v.Name);
                return clip.select(v).queryList().ToList();
            });
        }


        internal string SaveVariable(ProcessVariableEntity entity)
        {
            var kit = Sail.UseRunSQL();
            return SaveVariable(kit, entity);
        }

        internal string SaveVariable(SQLBuilder kit, ProcessVariableEntity entity)
        {
            if (string.IsNullOrEmpty(entity.Name) || string.IsNullOrEmpty(entity.Value))
                throw new WorkflowException(Sail.Messages.GetEngineMessage("processvariablemanager.savevariable.error"));

            if (string.IsNullOrWhiteSpace(entity.ProcessId) && !string.IsNullOrWhiteSpace(entity.ProcessInstanceId))
                entity.ProcessId = ResolveProcessId(kit, entity.ProcessInstanceId);

            ProcessVariableEntity existProcessVariable = null;
            // Id 未赋值（null/空）视为新建
            if (!string.IsNullOrWhiteSpace(entity.ProcessVariableId))
            {
                existProcessVariable = GetVariableById(kit, entity.ProcessId, entity.ProcessVariableId);
            }
            else if (!string.IsNullOrWhiteSpace(entity.ProcessInstanceId))
            {
                var isProcessScope = string.Equals(
                    entity.VariableScope,
                    ProcessVariableScopeEnum.Process.ToString(),
                    StringComparison.OrdinalIgnoreCase);
                if (isProcessScope)
                {
                    existProcessVariable = GetVariableByProcess(kit, entity.ProcessInstanceId, entity.Name);
                }
                else if (!string.IsNullOrWhiteSpace(entity.ActivityInstanceId))
                {
                    existProcessVariable = GetVariableByActivity(
                        kit, entity.ProcessInstanceId, entity.ActivityInstanceId, entity.Name);
                }
            }

            if (existProcessVariable != null)
            {
                existProcessVariable.Value = entity.Value;
                existProcessVariable.UpdatedDateTime = DateTime.UtcNow;
                if (string.IsNullOrWhiteSpace(existProcessVariable.ProcessId))
                    existProcessVariable.ProcessId = entity.ProcessId;
                Update(kit, existProcessVariable);
                return existProcessVariable.ProcessVariableId;
            }

            // 新建：清空 Id，由 AutoKey=SailId 发号
            entity.ProcessVariableId = null;
            entity.UpdatedDateTime = DateTime.UtcNow;
            return Insert(kit, entity);
        }

        internal bool ValidateProcessVariable(string processInstanceId, string expression)
        {
            var kit = Sail.UseRunSQL();
            return ValidateProcessVariable(kit, processInstanceId, expression);
        }

        internal bool ValidateProcessVariable(SQLBuilder kit, string processInstanceId, string expression)
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
                var pipeline = NextStep.NextStepConditionRequire.GetPipelineRequired(Sail);
                parsed = pipeline.Evaluate(new NextStep.ConditionEvaluateContext
                {
                    Sail = Sail,
                    Expression = expression,
                    Variables = keyValuePair,
                    Phase = NextStep.NextStepRoutePhase.Send
                });
            }
            return parsed;
        }

        private string Insert(SQLBuilder kit, ProcessVariableEntity entity)
        {
            var table = Router.ResolveVariableTable(entity?.ProcessId);
            var newId = kit.insertReturnId<ProcessVariableEntity,string>( entity, table);
            entity.ProcessVariableId = newId;
            return newId;
        }

        private void Update(SQLBuilder kit, ProcessVariableEntity entity)
        {
            var table = Router.ResolveVariableTable(entity?.ProcessId);
            // 旧行可能仍在默认表：先试解析表，失败再探测
            try
            {
                kit.update(entity, table);
            }
            catch
            {
                if (!Router.IsSharded(entity?.ProcessId))
                    throw;
                kit.update(entity, Router.DefaultVariableTable);
            }
        }

        internal void DeleteVariable(string processId, string variableId)
            => DeleteVariable(Sail.UseRunSQL(), processId, variableId);

        internal void DeleteVariable(SQLBuilder kit, string processId, string variableId)
        {
            //由router保证表，异常，自然抛出，便于业务侧调试期间就发现问题
            var table = Router.ResolveVariableTable(processId);
            kit.deleteById<ProcessVariableEntity>(variableId, table);
        }

        private string ResolveProcessId(SQLBuilder kit, string processInstanceId)
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

            var seen = new HashSet<string>(rows.Where(x => !string.IsNullOrWhiteSpace(x.ProcessVariableId)).Select(x => x.ProcessVariableId));
            foreach (var row in legacy)
            {
                if (!string.IsNullOrWhiteSpace(row.ProcessVariableId) && seen.Contains(row.ProcessVariableId))
                    continue;
                rows.Add(row);
            }
            return rows;
        }

    }
}
