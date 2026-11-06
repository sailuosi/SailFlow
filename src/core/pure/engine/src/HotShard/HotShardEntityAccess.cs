using System;
using System.Collections.Generic;
using mooSQL.data;
using saiflow.data;
using saiflow.engine.Client;
using saiflow.engine.Entity;

namespace saiflow.engine.HotShard
{
    /// <summary>按物理表名对实体做 CRUD（热分表路由后使用）。</summary>
    public static class HotShardEntityAccess
    {
        public static int InsertReturnId<T>(SQLBuilder kit, string physicalTable, T entity) where T : class
        {
            var prepared = SailEntityKit.PrepareInsertOnTable(entity, physicalTable, kit);
            var idObj = prepared.Kit.doInsertReturnId(SailEntityKit.KeyColumn<T>());
            if (idObj == null || idObj == DBNull.Value)
                return 0;
            return Convert.ToInt32(idObj);
        }

        public static void Update<T>(SQLBuilder kit, string physicalTable, T entity) where T : class
        {
            SailEntityKit.PrepareUpdateOnTable(entity, physicalTable, kit).doUpdate();
        }

        public static T GetById<T>(SQLBuilder kit, string physicalTable, int id) where T : class
            => SailEntityKit.PrepareSelectByIdOnTable<T>(id, physicalTable, kit).queryUnique<T>();

        public static void DeleteById<T>(SQLBuilder kit, string physicalTable, int id) where T : class
        {
            SailEntityKit.PrepareDeleteByIdOnTable<T>(id, physicalTable, kit).doDelete();
        }

        /// <summary>按 LookupTables 顺序探测主键；命中即返回。</summary>
        public static T GetByIdProbing<T>(SQLBuilder kit, IReadOnlyList<string> tables, int id) where T : class
        {
            if (tables == null || tables.Count == 0)
                return null;
            foreach (var table in tables)
            {
                try
                {
                    var row = GetById<T>(kit, table, id);
                    if (row != null)
                        return row;
                }
                catch
                {
                    // 分表尚未创建时忽略
                }
            }
            return null;
        }

        /// <summary>按 LookupTables 顺序删除；删到行为止。</summary>
        public static bool DeleteByIdProbing<T>(SQLBuilder kit, IReadOnlyList<string> tables, int id) where T : class
        {
            if (tables == null || tables.Count == 0)
                return false;
            foreach (var table in tables)
            {
                try
                {
                    var row = GetById<T>(kit, table, id);
                    if (row == null)
                        continue;
                    DeleteById<T>(kit, table, id);
                    return true;
                }
                catch
                {
                    // ignore missing shard
                }
            }
            return false;
        }



        public static string ResolveProcessId(SQLBuilder kit, int processInstanceId)
        {
            if (kit == null || processInstanceId <= 0)
                return null;
            try
            {
                var next = kit.DBLive.useSQL();
                if (kit.Executor != null)
                    next.useTransaction(kit.Executor);
                var pi = next.useRepo<ProcessInstanceEntity, int>().GetById(processInstanceId);
                return pi?.ProcessId;
            }
            catch
            {
                return null;
            }
        }
    }
}
