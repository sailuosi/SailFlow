using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using saiflow.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Client;
using saiflow.engine.Common;

namespace saiflow.data.Lifecycle
{
    /// <summary>
    /// 终态流程实例闭包：热表 → 按年冷表 → 删热表。读 <see cref="SailDataLifecycleOptions"/>；由 Host 定时或立刻 Hook 唤起。
    /// </summary>
    public sealed class ProcessArchiveService
    {
        public ProcessArchiveService(SailClient client, ISailColdStore coldStore = null)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
            Cold = coldStore ?? new PrimarySailColdStore(client);
        }

        public SailClient Sail { get; }
        public ISailColdStore Cold { get; }

        public SailDataLifecycleOptions Options =>
            Sail.DataLifecycleOptions ?? new SailDataLifecycleOptions();

        /// <summary>按保留窗批量归档；返回成功迁出实例数。</summary>
        public int RunBatch(int? batchSize = null, DateTime? nowUtc = null)
        {
            var opts = Options;
            var limit = batchSize ?? opts.ArchiveBatchSize;
            var now = nowUtc ?? DateTime.UtcNow;
            var cutoff = now.AddDays(-Math.Max(0, opts.ArchiveRetentionDays));

            List<ProcessInstanceEntity> candidates;
            using (var hot = Cold.UseHotSQL())
            {
                candidates = LoadCandidates(hot, cutoff, Math.Max(1, limit));
            }

            var ok = 0;
            foreach (var pi in candidates)
            {
                if (pi.Id.HasValue && ArchiveOne(pi.Id.Value))
                    ok++;
            }

            return ok;
        }

        /// <summary>归档单个实例（幂等：冷表已有主键则跳过插入，仍删热表）。</summary>
        public bool ArchiveOne(int processInstanceId)
        {
            if (Cold.IsSameDatabase)
                return ArchiveOneSameDb(processInstanceId);
            return ArchiveOneCrossDb(processInstanceId);
        }

        private bool ArchiveOneSameDb(int processInstanceId)
        {
            using (var kit = Cold.UseHotSQL())
            {
                kit.beginTransaction();
                try
                {
                    var pi = kit.useRepo<ProcessInstanceEntity, int>().GetById(processInstanceId);
                    if (pi == null || !pi.Id.HasValue || !IsArchivable(pi))
                    {
                        kit.commit();
                        return false;
                    }

                    var year = pi.EndedDateTime.Value.Year;
                    EnsureYearTables(kit, year);
                    CopyClosure(kit, kit, pi, year);
                    DeleteHotClosure(kit, processInstanceId);
                    kit.commit();
                    return true;
                }
                catch
                {
                    throw;
                }
            }
        }

        private bool ArchiveOneCrossDb(int processInstanceId)
        {
            ProcessInstanceEntity pi;
            List<ActivityInstanceEntity> activities;
            List<TaskEntity> tasks;
            List<TransitionInstanceEntity> transitions;
            List<ProcessVariableEntity> variables;

            using (var hot = Cold.UseHotSQL())
            {
                pi = hot.useRepo<ProcessInstanceEntity, int>().GetById(processInstanceId);
                if (pi == null || !IsArchivable(pi))
                    return false;

                var id = pi.Id.Value;
                activities = LoadActivities(hot, id);
                tasks = LoadTasks(hot, id);
                transitions = LoadTransitions(hot, id);
                variables = LoadVariables(hot, id);
            }

            var year = pi.EndedDateTime.Value.Year;
            using (var cold = Cold.UseColdSQL())
            {
                cold.beginTransaction();
                try
                {
                    EnsureYearTables(cold, year);
                    WriteClosure(cold, year, pi, activities, tasks, transitions, variables);
                    cold.commit();
                }
                catch
                {
                    throw;
                }
            }

            using (var hot = Cold.UseHotSQL())
            {
                hot.beginTransaction();
                try
                {
                    DeleteHotClosure(hot, processInstanceId);
                    hot.commit();
                }
                catch
                {
                    throw;
                }
            }

            return true;
        }

        private static List<ProcessInstanceEntity> LoadCandidates(SQLBuilder hot, DateTime cutoff, int limit)
        {
            var clip = SailSqlAccess.Sibling(hot).useClip();
            clip.from<ProcessInstanceEntity>(out var pi);
            clip.whereIn(() => pi.ProcessState, new short?[]
            {
                (short)ProcessStateEnum.Completed,
                (short)ProcessStateEnum.Canceled,
                (short)ProcessStateEnum.Discarded,
                (short)ProcessStateEnum.Terminated
            });
            clip.whereIsNotNull(() => pi.EndedDateTime);
            clip.where(() => pi.EndedDateTime, cutoff, "<");
            clip.orderBy(() => pi.EndedDateTime);
            return clip.select(pi).queryList().Take(limit).ToList();
        }

        private void EnsureYearTables(SQLBuilder kit, int year)
        {
            var opts = Options;
            foreach (var spec in HistTableCatalog.Closure)
            {
                var hist = HistTableCatalog.ResolveHistName(opts, spec, year);
                HistTableEnsurer.Ensure(kit, hist, spec.HotTableName);
            }
        }

        private void CopyClosure(SQLBuilder hot, SQLBuilder cold, ProcessInstanceEntity pi, int year)
        {
            var id = pi.Id.Value;
            WriteClosure(
                cold,
                year,
                pi,
                LoadActivities(hot, id),
                LoadTasks(hot, id),
                LoadTransitions(hot, id),
                LoadVariables(hot, id));
        }

        private void WriteClosure(
            SQLBuilder cold,
            int year,
            ProcessInstanceEntity pi,
            List<ActivityInstanceEntity> activities,
            List<TaskEntity> tasks,
            List<TransitionInstanceEntity> transitions,
            List<ProcessVariableEntity> variables)
        {
            var opts = Options;
            Upsert(cold, HistTableCatalog.ResolveHistName(opts, HistTableCatalog.ProcessInstance, year), pi);
            foreach (var row in activities)
                Upsert(cold, HistTableCatalog.ResolveHistName(opts, HistTableCatalog.ActivityInstance, year), row);
            foreach (var row in tasks)
                Upsert(cold, HistTableCatalog.ResolveHistName(opts, HistTableCatalog.Task, year), row);
            foreach (var row in transitions)
                Upsert(cold, HistTableCatalog.ResolveHistName(opts, HistTableCatalog.TransitionInstance, year), row);
            foreach (var row in variables)
                Upsert(cold, HistTableCatalog.ResolveHistName(opts, HistTableCatalog.ProcessVariable, year), row);
        }

        private static void Upsert<T>(SQLBuilder kit, string histTable, T entity) where T : class
        {
            var idProp = typeof(T).GetProperty("Id");
            var idObj = idProp?.GetValue(entity);
            if (idObj is int id && ArchiveRowCopier.ExistsById(kit, histTable, id))
                return;
            ArchiveRowCopier.InsertPreservingKey(kit, histTable, entity);
        }

        private static List<ActivityInstanceEntity> LoadActivities(SQLBuilder kit, int processInstanceId)
        {
            var clip = SailSqlAccess.Sibling(kit).useClip();
            clip.from<ActivityInstanceEntity>(out var a);
            clip.where(() => a.ProcessInstanceId, processInstanceId);
            return clip.select(a).queryList().ToList();
        }

        private static List<TaskEntity> LoadTasks(SQLBuilder kit, int processInstanceId)
        {
            var clip = SailSqlAccess.Sibling(kit).useClip();
            clip.from<TaskEntity>(out var t);
            clip.where(() => t.ProcessInstanceId, processInstanceId);
            return clip.select(t).queryList().ToList();
        }

        private List<TransitionInstanceEntity> LoadTransitions(SQLBuilder kit, int processInstanceId)
        {
            var processId = ResolveProcessId(kit, processInstanceId);
            return LoadByProcessInstanceWithFallback<TransitionInstanceEntity>(
                kit,
                processInstanceId,
                Sail.HotShard.ResolveTransitionTable(processId),
                Sail.HotShard.DefaultTransitionTable,
                Sail.HotShard.IsSharded(processId));
        }

        private List<ProcessVariableEntity> LoadVariables(SQLBuilder kit, int processInstanceId)
        {
            var processId = ResolveProcessId(kit, processInstanceId);
            return LoadByProcessInstanceWithFallback<ProcessVariableEntity>(
                kit,
                processInstanceId,
                Sail.HotShard.ResolveVariableTable(processId),
                Sail.HotShard.DefaultVariableTable,
                Sail.HotShard.IsSharded(processId));
        }

        private void DeleteHotClosure(SQLBuilder kit, int processInstanceId)
        {
            var processId = ResolveProcessId(kit, processInstanceId);

            var clip = SailSqlAccess.Sibling(kit).useClip();
            clip.setTable<TaskEntity>(out var t);
            clip.where(() => t.ProcessInstanceId, processInstanceId);
            clip.doDelete();

            DeleteByProcessInstance(kit, Sail.HotShard.ResolveTransitionTable(processId), processInstanceId);
            if (Sail.HotShard.IsSharded(processId))
                DeleteByProcessInstance(kit, Sail.HotShard.DefaultTransitionTable, processInstanceId);

            clip = SailSqlAccess.Sibling(kit).useClip();
            clip.setTable<ActivityInstanceEntity>(out var ai);
            clip.where(() => ai.ProcessInstanceId, processInstanceId);
            clip.doDelete();

            DeleteByProcessInstance(kit, Sail.HotShard.ResolveVariableTable(processId), processInstanceId);
            if (Sail.HotShard.IsSharded(processId))
                DeleteByProcessInstance(kit, Sail.HotShard.DefaultVariableTable, processInstanceId);

            kit.useRepo<ProcessInstanceEntity, int>().DeleteById(processInstanceId);
        }

        private static string ResolveProcessId(SQLBuilder kit, int processInstanceId)
        {
            try
            {
                var pi = SailSqlAccess.Sibling(kit).useRepo<ProcessInstanceEntity, int>().GetById(processInstanceId);
                return pi?.ProcessId;
            }
            catch
            {
                return null;
            }
        }

        private static List<T> LoadByProcessInstanceWithFallback<T>(
            SQLBuilder kit,
            int processInstanceId,
            string primaryTable,
            string defaultTable,
            bool mergeDefault) where T : class, new()
        {
            var rows = LoadByProcessInstance<T>(kit, primaryTable, processInstanceId);
            if (!mergeDefault || string.Equals(primaryTable, defaultTable, StringComparison.OrdinalIgnoreCase))
                return rows;

            var legacy = LoadByProcessInstance<T>(kit, defaultTable, processInstanceId);
            if (legacy.Count == 0)
                return rows;

            var idProp = typeof(T).GetProperty("Id");
            var seen = new HashSet<int>();
            foreach (var row in rows)
            {
                if (idProp?.GetValue(row) is int id)
                    seen.Add(id);
            }
            foreach (var row in legacy)
            {
                if (idProp?.GetValue(row) is int id && seen.Contains(id))
                    continue;
                rows.Add(row);
            }
            return rows;
        }

        private static List<T> LoadByProcessInstance<T>(SQLBuilder kit, string table, int processInstanceId)
            where T : class, new()
        {
            try
            {
                var clip = SailSqlAccess.Sibling(kit).useClip();
                clip.from<T>(table, out var row);
                clip.where("process_instance_id", processInstanceId);
                return clip.select(row).queryList().ToList();
            }
            catch
            {
                return new List<T>();
            }
        }

        private static void DeleteByProcessInstance(SQLBuilder kit, string physicalTable, int processInstanceId)
        {
            try
            {
                SailSqlAccess.Sibling(kit).setTable(physicalTable)
                    .where("process_instance_id", processInstanceId)
                    .doDelete();
            }
            catch
            {
                // 分表未创建时忽略
            }
        }

        private static bool IsArchivable(ProcessInstanceEntity pi) =>
            IsTerminal(pi) && pi.EndedDateTime.HasValue;

        private static bool IsTerminal(ProcessInstanceEntity pi)
        {
            if (!pi.ProcessState.HasValue)
                return false;
            var s = pi.ProcessState.Value;
            return s == (short)ProcessStateEnum.Completed
                || s == (short)ProcessStateEnum.Canceled
                || s == (short)ProcessStateEnum.Discarded
                || s == (short)ProcessStateEnum.Terminated;
        }
    }
}
