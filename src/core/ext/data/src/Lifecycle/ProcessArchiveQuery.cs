using System;
using System.Collections.Generic;
using System.Linq;
using sailflow.data;
using sailflow.hosting;
using sailflow.storage;

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

        IProcessArchiveStorage ArchiveStorage =>
            Sail.Storages.GetRequired<IProcessArchiveStorage>();

        public ProcessInstanceEntity GetProcessInstance(string id, int endedYear)
        {
            var table = HistTableCatalog.ResolveHistName(Options, HistTableCatalog.ProcessInstance, endedYear);
            try
            {
                using (var kit = Cold.UseColdSQL())
                    return ArchiveStorage.GetProcessInstance(kit, table, id);
            }
            catch
            {
                return null;
            }
        }

        public IList<TaskEntity> GetTasks(string processInstanceId, int endedYear)
        {
            var table = HistTableCatalog.ResolveHistName(Options, HistTableCatalog.Task, endedYear);
            try
            {
                using (var kit = Cold.UseColdSQL())
                    return ArchiveStorage.ListTasks(kit, table, processInstanceId);
            }
            catch
            {
                return new List<TaskEntity>();
            }
        }

        public IList<ActivityInstanceEntity> GetActivityInstances(string processInstanceId, int endedYear)
        {
            var table = HistTableCatalog.ResolveHistName(Options, HistTableCatalog.ActivityInstance, endedYear);
            try
            {
                using (var kit = Cold.UseColdSQL())
                    return ArchiveStorage.ListActivities(kit, table, processInstanceId);
            }
            catch
            {
                return new List<ActivityInstanceEntity>();
            }
        }

        public IList<TrackEntity> GetTransitions(string processInstanceId, int endedYear)
        {
            var table = HistTableCatalog.ResolveHistName(Options, HistTableCatalog.TransitionInstance, endedYear);
            try
            {
                using (var kit = Cold.UseColdSQL())
                    return ArchiveStorage.ListTransitions(kit, table, processInstanceId);
            }
            catch
            {
                return new List<TrackEntity>();
            }
        }

        public IList<ProcessVariableEntity> GetVariables(string processInstanceId, int endedYear)
        {
            var table = HistTableCatalog.ResolveHistName(Options, HistTableCatalog.ProcessVariable, endedYear);
            try
            {
                using (var kit = Cold.UseColdSQL())
                    return ArchiveStorage.ListVariables(kit, table, processInstanceId);
            }
            catch
            {
                return new List<ProcessVariableEntity>();
            }
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
    }
}
