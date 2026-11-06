using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using mooSQL.data;

namespace saiflow.data
{
    /// <summary>
    /// 用 <see cref="SQLBuilder"/> 构建实体 CRUD/按主键查询；
    /// 带外部 <see cref="IDbConnection"/> 时先 <c>toXxx()</c> 再在同 conn/trans 上执行（过渡）。
    /// </summary>
    public static class SailEntityKit
    {
        public static bool IsDefaultKeyValue(object val)
        {
            if (val == null)
                return true;
            if (val is int i)
                return i == 0;
            if (val is long l)
                return l == 0L;
            if (val is short s)
                return s == 0;
            if (val is byte b)
                return b == 0;
            if (val is string str)
                return string.IsNullOrEmpty(str);
            if (val is Guid g)
                return g == Guid.Empty;
            return false;
        }

        public static SQLBuilder NewKit() => SailDbAccess.GetRequired().useSQL();

        public static string KeyColumn<T>() where T : class
            => SailEntityMapper.GetColumnMaps<T>().FirstOrDefault(m => m.IsKey)?.ColumnName ?? "id";


        /// <summary>向指定物理表 INSERT（热分表 / 冷表）。</summary>
        public static (SQLBuilder Kit, object KeyValue) PrepareInsertOnTable<T>(
            T entity, string physicalTable, SQLBuilder kit = null) where T : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            if (string.IsNullOrWhiteSpace(physicalTable))
                throw new ArgumentException("Physical table is required.", nameof(physicalTable));

            var maps = SailEntityMapper.GetColumnMaps<T>();
            kit = (kit ?? NewKit()).setTable(physicalTable).newRow();

            object keyVal = null;
            foreach (var map in maps)
            {
                var val = map.GetValue(entity);
                if (map.IsKey)
                {
                    keyVal = val;
                    if (IsDefaultKeyValue(val))
                        continue;
                }
                if (val == null)
                    continue;
                kit.set(map.ColumnName, val);
            }
            return (kit, keyVal);
        }


        /// <summary>在指定物理表 UPDATE。</summary>
        public static SQLBuilder PrepareUpdateOnTable<T>(
            T entity, string physicalTable, SQLBuilder kit = null) where T : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            if (string.IsNullOrWhiteSpace(physicalTable))
                throw new ArgumentException("Physical table is required.", nameof(physicalTable));

            var maps = SailEntityMapper.GetColumnMaps<T>();
            kit = (kit ?? NewKit()).setTable(physicalTable);

            ColumnMap keyMap = null;
            object keyVal = null;
            foreach (var map in maps)
            {
                if (map.IsKey)
                {
                    keyMap = map;
                    keyVal = map.GetValue(entity);
                    continue;
                }
                kit.set(map.ColumnName, map.GetValue(entity));
            }

            if (keyMap == null || IsDefaultKeyValue(keyVal))
                throw new InvalidOperationException($"Update<{typeof(T).Name}> requires a primary key value.");

            return kit.where(keyMap.ColumnName, keyVal);
        }


        /// <summary>在指定物理表按主键 DELETE。</summary>
        public static SQLBuilder PrepareDeleteByIdOnTable<T>(
            object primaryId, string physicalTable, SQLBuilder kit = null) where T : class
        {
            if (string.IsNullOrWhiteSpace(physicalTable))
                throw new ArgumentException("Physical table is required.", nameof(physicalTable));
            return (kit ?? NewKit()).setTable(physicalTable).where(KeyColumn<T>(), primaryId);
        }


        /// <summary>在指定物理表按主键 SELECT。</summary>
        public static SQLBuilder PrepareSelectByIdOnTable<T>(
            object primaryId, string physicalTable, SQLBuilder kit = null) where T : class
        {
            if (string.IsNullOrWhiteSpace(physicalTable))
                throw new ArgumentException("Physical table is required.", nameof(physicalTable));
            return (kit ?? NewKit())
                .select("*")
                .from(physicalTable)
                .where(KeyColumn<T>(), primaryId);
        }






    }
}
