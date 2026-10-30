using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using saiflow.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.SqlProvider;
using saiflow.engine.Client;

namespace saiflow.engine.Business.Manager
{
    /// <summary>
    /// 流程变量定义管理类（wf_variable）。
    /// 终态：无事务从 Client 取 kit；有事务只用传入的同一个 kit。
    /// </summary>
    public class VariableManager
    {
        public VariableManager(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }
        private const string VariableColumns =
            "id, process_id, version, activity_id, name, type, direction, default_value, " +
            "is_required, is_referenced, source_ref, source_variable_name, sort_order, description, " +
            "created_datetime, updated_datetime";

        public IList<VariableEntity> GetList(string processId, string version, string activityId)
        {
            if (string.IsNullOrEmpty(processId) || string.IsNullOrEmpty(version) || string.IsNullOrEmpty(activityId))
                return new List<VariableEntity>();

            var kit = Sail.UseRunSQL();
            return kit
                .select(VariableColumns)
                .from(SailSqlDialect.VariableTable)
                .where("process_id", processId)
                .where("version", version)
                .where("activity_id", activityId)
                .orderBy("sort_order, id")
                .query<VariableEntity>()
                .ToList();
        }

        public VariableEntity GetById(int id)
            => Sail.UseRunSQL().useRepo<VariableEntity, int>().GetById(id);

        public int Insert(VariableEntity entity)
        {
            var kit = Sail.UseRunSQL();
            return Insert(kit, entity);
        }

        public int Insert(SQLBuilder kit, VariableEntity entity)
        {
            entity.CreatedDateTime = DateTime.UtcNow;
            var newId = kit.useRepo<VariableEntity, int>().InsertWithPK(entity);
            entity.Id = newId;
            return newId;
        }

        public void Update(VariableEntity entity)
        {
            var kit = Sail.UseRunSQL();
            Update(kit, entity);
        }

        public void Update(SQLBuilder kit, VariableEntity entity)
        {
            entity.UpdatedDateTime = DateTime.UtcNow;
            kit.useRepo<VariableEntity>().Update(entity);
        }

        public void Delete(int id)
            => Sail.UseRunSQL().useRepo<VariableEntity, int>().DeleteById(id);

        public void Delete(SQLBuilder kit, int id)
            => kit.useRepo<VariableEntity, int>().DeleteById(id);

        public void DeleteByProcessActivity(string processId, string version, string activityId)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    DeleteByProcessActivity(kit, processId, version, activityId);
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
            DeleteByProcessActivity(kit, processId, version, activityId);
            if (list == null)
                return;
            var order = 0;
            foreach (var item in list)
            {
                item.ProcessId = processId;
                item.Version = version;
                item.ActivityId = activityId;
                item.SortOrder = order++;
                item.Id = 0;
                Insert(kit, item);
            }
        }

        private static void DeleteByProcessActivity(SQLBuilder kit, string processId, string version, string activityId)
        {
            kit.setTable(SailSqlDialect.VariableTable)
                .where("process_id", processId)
                .where("version", version)
                .where("activity_id", activityId)
                .doDelete();
        }
    }
}
