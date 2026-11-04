using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using saiflow.data;
using saiflow.engine.Entity;
using saiflow.engine.Client;

namespace saiflow.data.Lifecycle
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

        public ProcessInstanceEntity GetProcessInstance(int id, int endedYear)
        {
            var table = HistTableCatalog.ResolveHistName(Options, HistTableCatalog.ProcessInstance, endedYear);
            return LoadById<ProcessInstanceEntity>(table, id);
        }

        public IList<TaskEntity> GetTasks(int processInstanceId, int endedYear)
        {
            var table = HistTableCatalog.ResolveHistName(Options, HistTableCatalog.Task, endedYear);
            return LoadByPi<TaskEntity>(table, processInstanceId);
        }

        public IList<ActivityInstanceEntity> GetActivityInstances(int processInstanceId, int endedYear)
        {
            var table = HistTableCatalog.ResolveHistName(Options, HistTableCatalog.ActivityInstance, endedYear);
            return LoadByPi<ActivityInstanceEntity>(table, processInstanceId);
        }

        public IList<TransitionInstanceEntity> GetTransitions(int processInstanceId, int endedYear)
        {
            var table = HistTableCatalog.ResolveHistName(Options, HistTableCatalog.TransitionInstance, endedYear);
            return LoadByPi<TransitionInstanceEntity>(table, processInstanceId);
        }

        public IList<ProcessVariableEntity> GetVariables(int processInstanceId, int endedYear)
        {
            var table = HistTableCatalog.ResolveHistName(Options, HistTableCatalog.ProcessVariable, endedYear);
            return LoadByPi<ProcessVariableEntity>(table, processInstanceId);
        }

        /// <summary>热表未命中时按候选年份依次查冷表。</summary>
        public ProcessInstanceEntity FindProcessInstance(int id, IEnumerable<int> candidateYears)
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

        private T LoadById<T>(string table, int id) where T : class, new()
        {
            try
            {
                using (var kit = Cold.UseColdSQL())
                {
                    var clip = SailSqlAccess.Sibling(kit).useClip();
                    clip.from<T>(table, out var row);
                    clip.where("id", id);
                    return clip.select(row).queryList().FirstOrDefault();
                }
            }
            catch
            {
                return null;
            }
        }

        private IList<T> LoadByPi<T>(string table, int processInstanceId) where T : class, new()
        {
            try
            {
                using (var kit = Cold.UseColdSQL())
                {
                    var clip = SailSqlAccess.Sibling(kit).useClip();
                    clip.from<T>(table, out var row);
                    clip.where("process_instance_id", processInstanceId);
                    return clip.select(row).queryList().ToList();
                }
            }
            catch
            {
                return new List<T>();
            }
        }
    }
}
