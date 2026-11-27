using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using sailflow.data;
using sailflow.hosting;
using sailflow.storage;
using sailflow.utils;

namespace sailflow.data.Lifecycle
{
    /// <summary>
    /// 终态流程实例闭包：热表 → 按年冷表 → 删热表。读 <see cref="SailDataLifecycleOptions"/>；由 Host 定时或立刻 Hook 唤起。
    /// 冷表经 <see cref="IProcessArchiveStorage"/>；热表删读写复用 PI/Task/AI/Transition/Variable Storage。
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

        IProcessArchiveStorage ArchiveStorage =>
            Sail.Storages.GetRequired<IProcessArchiveStorage>();

        IProcessInstanceStorage ProcessInstances =>
            Sail.Storages.GetRequired<IProcessInstanceStorage>();

        ITaskStorage Tasks =>
            Sail.Storages.GetRequired<ITaskStorage>();

        IActivityInstanceStorage Activities =>
            Sail.Storages.GetRequired<IActivityInstanceStorage>();

        ITransitionInstanceStorage Transitions =>
            Sail.Storages.GetRequired<ITransitionInstanceStorage>();

        IProcessVariableStorage Variables =>
            Sail.Storages.GetRequired<IProcessVariableStorage>();

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
                candidates = ArchiveStorage.ListArchivableCandidates(hot, cutoff, Math.Max(1, limit)).ToList();
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
                    var pi = ProcessInstances.GetById(kit, processInstanceId);
                    if (pi == null || string.IsNullOrWhiteSpace(pi.ProcessInstanceId) || !IsArchivable(pi))
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
            List<TrackEntity> transitions;
            List<ProcessVariableEntity> variables;

            using (var hot = Cold.UseHotSQL())
            {
                pi = ProcessInstances.GetById(hot, processInstanceId);
                if (pi == null || !IsArchivable(pi))
                    return false;

                var id = pi.ProcessInstanceId;
                activities = Activities.ListByProcessInstance(hot, id).ToList();
                tasks = Tasks.ListByProcessInstance(hot, id).ToList();
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
                Activities.ListByProcessInstance(hot, id).ToList(),
                Tasks.ListByProcessInstance(hot, id).ToList(),
                LoadTransitions(hot, id),
                LoadVariables(hot, id));
        }

        private void WriteClosure(
            SQLBuilder cold,
            int year,
            ProcessInstanceEntity pi,
            List<ActivityInstanceEntity> activities,
            List<TaskEntity> tasks,
            List<TrackEntity> transitions,
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

        private void Upsert<T>(SQLBuilder kit, string histTable, T entity) where T : class, new()
        {
            var keyMap = SailEntityMapper.GetColumnMaps<T>().FirstOrDefault(m => m.IsKey);
            var id = keyMap?.GetValue(entity) as string;
            if (!string.IsNullOrWhiteSpace(id) && ArchiveRowCopier.ExistsById<T>(kit, histTable, id))
                return;
            ArchiveRowCopier.InsertPreservingKey(kit, histTable, entity);
        }

        private List<TrackEntity> LoadTransitions(SQLBuilder kit, string processInstanceId)
        {
            var processId = ResolveProcessId(kit, processInstanceId);
            return LoadByProcessInstanceWithFallback(
                kit,
                processInstanceId,
                Sail.HotShard.ResolveTransitionTable(processId),
                Sail.HotShard.DefaultTransitionTable,
                Sail.HotShard.IsSharded(processId),
                (k, table, pi) => ArchiveStorage.ListTransitions(k, table, pi).ToList());
        }

        private List<ProcessVariableEntity> LoadVariables(SQLBuilder kit, string processInstanceId)
        {
            var processId = ResolveProcessId(kit, processInstanceId);
            return LoadByProcessInstanceWithFallback(
                kit,
                processInstanceId,
                Sail.HotShard.ResolveVariableTable(processId),
                Sail.HotShard.DefaultVariableTable,
                Sail.HotShard.IsSharded(processId),
                (k, table, pi) => Variables.ListByProcessInstance(k, table, pi).ToList());
        }

        private void DeleteHotClosure(SQLBuilder kit, string processInstanceId)
        {
            var processId = ResolveProcessId(kit, processInstanceId);

            Tasks.DeleteByProcessInstance(kit, processInstanceId);

            Transitions.DeleteByProcessInstance(kit, Sail.HotShard.ResolveTransitionTable(processId), processInstanceId);
            if (Sail.HotShard.IsSharded(processId))
                Transitions.DeleteByProcessInstance(kit, Sail.HotShard.DefaultTransitionTable, processInstanceId);

            Activities.DeleteByProcessInstance(kit, processInstanceId);

            Variables.DeleteByProcessInstance(kit, Sail.HotShard.ResolveVariableTable(processId), processInstanceId);
            if (Sail.HotShard.IsSharded(processId))
                Variables.DeleteByProcessInstance(kit, Sail.HotShard.DefaultVariableTable, processInstanceId);

            ProcessInstances.DeleteById(kit, processInstanceId);
        }

        private string ResolveProcessId(SQLBuilder kit, string processInstanceId)
        {
            try
            {
                var pi = ProcessInstances.GetById(kit, processInstanceId);
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
            bool mergeDefault,
            Func<SQLBuilder, string, string, List<T>> load) where T : class, new()
        {
            var rows = load(kit, primaryTable, processInstanceId) ?? new List<T>();
            if (!mergeDefault || string.Equals(primaryTable, defaultTable, StringComparison.OrdinalIgnoreCase))
                return rows;

            var legacy = load(kit, defaultTable, processInstanceId) ?? new List<T>();
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
