using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using sailflow.data;
using sailflow.engine.Entity;
using sailflow.engine.Client;

namespace sailflow.data.Lifecycle
{
    /// <summary>
    /// 冷表只读查询骨架：按办结年定位 <c>*_hist_YYYY</c>。
    /// Host 已办/详情：先热表，未命中再按 ended 年（或候选年份列表）调用本类。
    /// </summary>
    public sealed class ProcessArchiveQuery
    {
        public ProcessArchiveQuery(SailClient client, ISailColdStore coldStore = null)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
            Cold = coldStore ?? new PrimarySailColdStore(client);
        }

        public SailClient Sail { get; }
        public ISailColdStore Cold { get; }

        public SailDataLifecycleOptions Options =>
            Sail.DataLifecycleOptions ?? new SailDataLifecycleOptions();

        public ProcessInstanceEntity GetProcessInstance(string id, int endedYear)
        {
            var table = HistTableCatalog.ResolveHistName(Options, HistTableCatalog.ProcessInstance, endedYear);
            try
            {
                using (var kit = Cold.UseColdSQL())
                {
                    var clip = kit.useClip();
                    clip.from<ProcessInstanceEntity>(table, out var row)
                        .where(() => row.ProcessInstanceId, id);
                    return clip.select(row).queryList().FirstOrDefault();
                }
            }
            catch
            {
                return null;
            }
        }

        public IList<TaskEntity> GetTasks(string processInstanceId, int endedYear)
        {
            var table = HistTableCatalog.ResolveHistName(Options, HistTableCatalog.Task, endedYear);
            return LoadByPi<TaskEntity>(table, processInstanceId, (clip, tb, pi) =>
            {
                clip.from<TaskEntity>(tb, out var row)
                    .where(() => row.ProcessInstanceId, pi);
                return clip.select(row).queryList().ToList();
            });
        }

        public IList<ActivityInstanceEntity> GetActivityInstances(string processInstanceId, int endedYear)
        {
            var table = HistTableCatalog.ResolveHistName(Options, HistTableCatalog.ActivityInstance, endedYear);
            return LoadByPi<ActivityInstanceEntity>(table, processInstanceId, (clip, tb, pi) =>
            {
                clip.from<ActivityInstanceEntity>(tb, out var row)
                    .where(() => row.ProcessInstanceId, pi);
                return clip.select(row).queryList().ToList();
            });
        }

        public IList<TransitionInstanceEntity> GetTransitions(string processInstanceId, int endedYear)
        {
            var table = HistTableCatalog.ResolveHistName(Options, HistTableCatalog.TransitionInstance, endedYear);
            return LoadByPi<TransitionInstanceEntity>(table, processInstanceId, (clip, tb, pi) =>
            {
                clip.from<TransitionInstanceEntity>(tb, out var row)
                    .where(() => row.ProcessInstanceId, pi);
                return clip.select(row).queryList().ToList();
            });
        }

        public IList<ProcessVariableEntity> GetVariables(string processInstanceId, int endedYear)
        {
            var table = HistTableCatalog.ResolveHistName(Options, HistTableCatalog.ProcessVariable, endedYear);
            return LoadByPi<ProcessVariableEntity>(table, processInstanceId, (clip, tb, pi) =>
            {
                clip.from<ProcessVariableEntity>(tb, out var row)
                    .where(() => row.ProcessInstanceId, pi);
                return clip.select(row).queryList().ToList();
            });
        }

        /// <summary>热表未命中时按候选年份依次查冷表。</summary>
        public ProcessInstanceEntity FindProcessInstance(string id, IEnumerable<int> candidateYears)
        {
            if (candidateYears == null)
                return null;
            foreach (var y in candidateYears.Distinct().OrderByDescending(x => x))
            {
                var row = GetProcessInstance(id, y);
                if (row != null)
                    return row;
            }

            return null;
        }

        private IList<T> LoadByPi<T>(
            string table,
            string processInstanceId,
            Func<SQLClip, string, string, IList<T>> query) where T : class, new()
        {
            try
            {
                using (var kit = Cold.UseColdSQL())
                {
                    return query(kit.useClip(), table, processInstanceId);
                }
            }
            catch
            {
                return new List<T>();
            }
        }
    }
}
