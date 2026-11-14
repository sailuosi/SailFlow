using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using sailflow.data;
using sailflow.engine.Entity;
using sailflow.engine.Client;
using sailflow.engine.Common;

namespace sailflow.data.Lifecycle
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
                if (!string.IsNullOrWhiteSpace(pi.ProcessInstanceId) && ArchiveOne(pi.ProcessInstanceId))
                    ok++;
            }

            return ok;
        }

        /// <summary>归档单个实例（幂等：冷表已有主键则跳过插入，仍删热表）。</summary>
        public bool ArchiveOne(string processInstanceId)
        {
            if (Cold.IsSameDatabase)
                return ArchiveOneSameDb(processInstanceId);
            return ArchiveOneCrossDb(processInstanceId);
        }

        private bool ArchiveOneSameDb(string processInstanceId)
        {
            using (var kit = Cold.UseHotSQL())
            {
                kit.beginTransaction();
                try
                {
                    var pi = kit.findRowById<ProcessInstanceEntity>(processInstanceId);
                    if (pi == null || !!string.IsNullOrWhiteSpace(pi.ProcessInstanceId) || !IsArchivable(pi))
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

        private bool ArchiveOneCrossDb(string processInstanceId)
        {
            ProcessInstanceEntity pi;
            List<ActivityInstanceEntity> activities;
            List<TaskEntity> tasks;
            List<TransitionInstanceEntity> transitions;
            List<ProcessVariableEntity> variables;

            using (var hot = Cold.UseHotSQL())
            {
                pi = hot.findRowById<ProcessInstanceEntity>(processInstanceId);
                if (pi == null || !IsArchivable(pi))
                    return false;

                var id = pi.ProcessInstanceId;
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
            var clip = hot.useClip();
            clip.from<ProcessInstanceEntity>(out var pi)
                .whereIn(() => pi.ProcessState, new short?[]
                {
                    (short)ProcessStateEnum.Completed,
                    (short)ProcessStateEnum.Canceled,
                    (short)ProcessStateEnum.Discarded,
                    (short)ProcessStateEnum.Terminated
                })
                .whereIsNotNull(() => pi.EndedDateTime)
                .where(() => pi.EndedDateTime, cutoff, "<")
                .orderBy(() => pi.EndedDateTime);
            return clip.select(pi).queryList().Take(limit).ToList();
        }

        private void EnsureYearTables(SQLBuilder kit, int year)
        {
            var opts = Options;
            foreach (var spec in HistTableCatalog.Closure)
            {
                var hist = HistTableCatalog.ResolveHistName(opts, spec, year);
                HistTableEnsurer.Ensure(kit, spec, hist);
            }
        }

        private void CopyClosure(SQLBuilder hot, SQLBuilder cold, ProcessInstanceEntity pi, int year)
        {
            var id = pi.ProcessInstanceId;
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

        private static void Upsert<T>(SQLBuilder kit, string histTable, T entity) where T : class, new()
        {
            var keyMap = SailEntityMapper.GetColumnMaps<T>().FirstOrDefault(m => m.IsKey);
            var id = keyMap?.GetValue(entity) as string;
            if (!string.IsNullOrWhiteSpace(id) && ArchiveRowCopier.ExistsById<T>(kit, histTable, id))
                return;
            ArchiveRowCopier.InsertPreservingKey(kit, histTable, entity);
        }

        private static List<ActivityInstanceEntity> LoadActivities(SQLBuilder kit, string processInstanceId)
        {
            var clip = kit.useClip();
            clip.from<ActivityInstanceEntity>(out var a)
                .where(() => a.ProcessInstanceId, processInstanceId);
            return clip.select(a).queryList().ToList();
        }

        private static List<TaskEntity> LoadTasks(SQLBuilder kit, string processInstanceId)
        {
            var clip = kit.useClip();
            clip.from<TaskEntity>(out var t)
                .where(() => t.ProcessInstanceId, processInstanceId);
            return clip.select(t).queryList().ToList();
        }

        private List<TransitionInstanceEntity> LoadTransitions(SQLBuilder kit, string processInstanceId)
        {
            var processId = ResolveProcessId(kit, processInstanceId);
            return LoadByProcessInstanceWithFallback<TransitionInstanceEntity>(
                kit,
                processInstanceId,
                Sail.HotShard.ResolveTransitionTable(processId),
                Sail.HotShard.DefaultTransitionTable,
                Sail.HotShard.IsSharded(processId));
        }

        private List<ProcessVariableEntity> LoadVariables(SQLBuilder kit, string processInstanceId)
        {
            var processId = ResolveProcessId(kit, processInstanceId);
            return LoadByProcessInstanceWithFallback<ProcessVariableEntity>(
                kit,
                processInstanceId,
                Sail.HotShard.ResolveVariableTable(processId),
                Sail.HotShard.DefaultVariableTable,
                Sail.HotShard.IsSharded(processId));
        }

        private void DeleteHotClosure(SQLBuilder kit, string processInstanceId)
        {
            var processId = ResolveProcessId(kit, processInstanceId);

            var clip = kit.useClip();
            clip.setTable<TaskEntity>(out var t)
                .where(() => t.ProcessInstanceId, processInstanceId)
                .doDelete();

            DeleteTransitionsByProcessInstance(kit, Sail.HotShard.ResolveTransitionTable(processId), processInstanceId);
            if (Sail.HotShard.IsSharded(processId))
                DeleteTransitionsByProcessInstance(kit, Sail.HotShard.DefaultTransitionTable, processInstanceId);

            clip = kit.useClip();
            clip.setTable<ActivityInstanceEntity>(out var ai)
                .where(() => ai.ProcessInstanceId, processInstanceId)
                .doDelete();

            DeleteVariablesByProcessInstance(kit, Sail.HotShard.ResolveVariableTable(processId), processInstanceId);
            if (Sail.HotShard.IsSharded(processId))
                DeleteVariablesByProcessInstance(kit, Sail.HotShard.DefaultVariableTable, processInstanceId);

            kit.useRepo<ProcessInstanceEntity, string>().DeleteById(processInstanceId);
        }

        private static string ResolveProcessId(SQLBuilder kit, string processInstanceId)
        {
            try
            {
                var pi = kit.findRowById<ProcessInstanceEntity>(processInstanceId);
                return pi?.ProcessId;
            }
            catch
            {
                return null;
            }
        }

        private static List<T> LoadByProcessInstanceWithFallback<T>(
            SQLBuilder kit,
            string processInstanceId,
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

            var keyMap = SailEntityMapper.GetColumnMaps<T>().FirstOrDefault(m => m.IsKey);
            var seen = new HashSet<string>();
            foreach (var row in rows)
            {
                if (keyMap?.GetValue(row) is string id && !string.IsNullOrWhiteSpace(id))
                    seen.Add(id);
            }
            foreach (var row in legacy)
            {
                if (keyMap?.GetValue(row) is string id && !string.IsNullOrWhiteSpace(id) && seen.Contains(id))
                    continue;
                rows.Add(row);
            }
            return rows;
        }

        private static List<T> LoadByProcessInstance<T>(SQLBuilder kit, string table, string processInstanceId)
            where T : class, new()
        {
            try
            {
                if (typeof(T) == typeof(TransitionInstanceEntity))
                {
                    var clip = kit.useClip();
                    clip.from<TransitionInstanceEntity>(table, out var row)
                        .where(() => row.ProcessInstanceId, processInstanceId);
                    return clip.select(row).queryList().Cast<T>().ToList();
                }
                if (typeof(T) == typeof(ProcessVariableEntity))
                {
                    var clip = kit.useClip();
                    clip.from<ProcessVariableEntity>(table, out var row)
                        .where(() => row.ProcessInstanceId, processInstanceId);
                    return clip.select(row).queryList().Cast<T>().ToList();
                }

                throw new NotSupportedException("LoadByProcessInstance unsupported type: " + typeof(T).Name);
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch
            {
                return new List<T>();
            }
        }

        private static void DeleteTransitionsByProcessInstance(SQLBuilder kit, string physicalTable, string processInstanceId)
        {
            try
            {
                var clip = kit.useClip();
                clip.setTable<TransitionInstanceEntity>(out var t)
                    .useSQL(b => b.setTable(physicalTable))
                    .where(() => t.ProcessInstanceId, processInstanceId)
                    .doDelete();
            }
            catch
            {
                // 分表未创建时忽略
            }
        }

        private static void DeleteVariablesByProcessInstance(SQLBuilder kit, string physicalTable, string processInstanceId)
        {
            try
            {
                var clip = kit.useClip();
                clip.setTable<ProcessVariableEntity>(out var v)
                    .useSQL(b => b.setTable(physicalTable))
                    .where(() => v.ProcessInstanceId, processInstanceId)
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
