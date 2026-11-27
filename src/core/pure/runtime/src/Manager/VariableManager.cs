using System;
using System.Collections.Generic;
using mooSQL.data;
using sailflow.data;
using sailflow.hosting;
using sailflow.run;
using sailflow.storage;

namespace sailflow.data
{
    /// <summary>
    /// 流程变量定义管理类（sf_variable）。
    /// 终态：无事务从 Client 取 kit；有事务只用传入的同一个 kit。
    /// </summary>
    public class VariableManager : IVariableManager
    {
        public VariableManager(SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        protected IVariableDefinitionStorage Storage =>
            Sail.Storages.GetRequired<IVariableDefinitionStorage>();

        public IList<VariableEntity> GetList(string processId, string version, string activityId)
        {
            if (string.IsNullOrEmpty(processId) || string.IsNullOrEmpty(version) || string.IsNullOrEmpty(activityId))
                return new List<VariableEntity>();
            return Storage.ListByActivity(Sail.UseRunSQL(), processId, version, activityId);
        }

        public VariableEntity GetById(string id)
            => Storage.GetById(Sail.UseRunSQL(), id);

        public string Insert(VariableEntity entity)
        {
            var kit = Sail.UseRunSQL();
            return Insert(kit, entity);
        }

        public string Insert(SQLBuilder kit, VariableEntity entity)
        {
            entity.CreatedDateTime = DateTime.UtcNow;
            return Storage.Insert(kit, entity);
        }

        public void Update(VariableEntity entity)
        {
            var kit = Sail.UseRunSQL();
            Update(kit, entity);
        }

        public void Update(SQLBuilder kit, VariableEntity entity)
        {
            entity.UpdatedDateTime = DateTime.UtcNow;
            Storage.Update(kit, entity);
        }

        public void Delete(string id)
            => Storage.DeleteById(Sail.UseRunSQL(), id);

        public void Delete(SQLBuilder kit, string id)
            => Storage.DeleteById(kit, id);

        public void DeleteByProcessActivity(string processId, string version, string activityId)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    Storage.DeleteByProcessActivity(kit, processId, version, activityId);
                    kit.commit();
                }
                catch
                {
                    throw;
                }
            }
        }

        public void SaveList(string processId, string version, string activityId, IList<VariableEntity> list)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    SaveList(kit, processId, version, activityId, list);
                    kit.commit();
                }
                catch
                {
                    throw;
                }
            }
        }

        /// <summary>有事务：使用上游灌入的同一个 kit。</summary>
        public void SaveList(SQLBuilder kit, string processId, string version, string activityId, IList<VariableEntity> list)
        {
            Storage.DeleteByProcessActivity(kit, processId, version, activityId);
            if (list == null)
                return;
            var order = 0;
            foreach (var item in list)
            {
                item.ProcessId = processId;
                item.Version = version;
                item.ActivityId = activityId;
                item.SortOrder = order++;
                item.VariableId = null;
                Insert(kit, item);
            }
        }
    }
}
