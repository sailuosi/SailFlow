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
        public static string InsertReturnId<T>(SQLBuilder kit, string physicalTable, T entity) where T : class
        {
            if (string.IsNullOrWhiteSpace(physicalTable))
                kit.insert(entity);
            else
                kit.insert(entity, physicalTable);
            var prop = typeof(T).GetProperty("Id");
            return prop?.GetValue(entity) as string;
        }

        public static void Update<T>(SQLBuilder kit, string physicalTable, T entity) where T : class
        {
            kit.update(entity, physicalTable);
        }

        public static T GetById<T>(SQLBuilder kit, string physicalTable, string id) where T : class
        {
            return kit.findRowById<T>(id, physicalTable);
        }
        //=> SailEntityKit.PrepareSelectByIdOnTable<T>(id, physicalTable, kit).queryUnique<T>();

        public static void DeleteById<T>(SQLBuilder kit, string physicalTable, string id) where T : class
        {
            kit.deleteById<T>(id, physicalTable);
        }

        /// <summary>按 LookupTables 顺序探测主键；命中即返回。</summary>
        public static T GetByIdProbing<T>(SQLBuilder kit, IReadOnlyList<string> tables, string id) where T : class
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
        public static bool DeleteByIdProbing<T>(SQLBuilder kit, IReadOnlyList<string> tables, string id) where T : class
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



        public static string ResolveProcessId(SQLBuilder kit, string processInstanceId)
        {
            if (kit == null || string.IsNullOrWhiteSpace(processInstanceId))
                return null;
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
    }
}
