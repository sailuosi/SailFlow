using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using mooSQL.data;

namespace sailflow.data
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

        /// <summary>配置 INSERT 字段；调用方再 <c>doInsert</c> 或 <c>toInsert</c>。</summary>
        public static (SQLBuilder Kit, object KeyValue) PrepareInsert<T>(T entity, SQLBuilder kit = null) where T : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var table = SailEntityMapper.ResolveTableName<T>();
            var maps = SailEntityMapper.GetColumnMaps<T>();
            kit = (kit ?? NewKit()).setTable(table).newRow();

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

        public static SQLBuilder PrepareUpdate<T>(T entity, SQLBuilder kit = null) where T : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var table = SailEntityMapper.ResolveTableName<T>();
            var maps = SailEntityMapper.GetColumnMaps<T>();
            kit = (kit ?? NewKit()).setTable(table);

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

        public static SQLBuilder PrepareDeleteById<T>(object primaryId, SQLBuilder kit = null) where T : class
        {
            var table = SailEntityMapper.ResolveTableName<T>();
            return (kit ?? NewKit()).setTable(table).where(KeyColumn<T>(), primaryId);
        }

        public static SQLBuilder PrepareSelectById<T>(object primaryId, SQLBuilder kit = null) where T : class
        {
            var table = SailEntityMapper.ResolveTableName<T>();
            return (kit ?? NewKit())
                .select("*")
                .from(table)
                .where(KeyColumn<T>(), primaryId);
        }

        public static SQLBuilder PrepareSelectAll<T>(SQLBuilder kit = null) where T : class
        {
            var table = SailEntityMapper.ResolveTableName<T>();
            return (kit ?? NewKit()).select("*").from(table);
        }

        public static SQLBuilder PrepareSelectByIds<T>(IEnumerable<object> ids, SQLBuilder kit = null) where T : class
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));
            var table = SailEntityMapper.ResolveTableName<T>();
            var list = ids as IList<object> ?? ids.ToList();
            return (kit ?? NewKit())
                .select("*")
                .from(table)
                .whereIn(KeyColumn<T>(), list);
        }

        public static SQLBuilder PrepareSelectByName<T>(string colName, object value, SQLBuilder kit = null) where T : class
        {
            var table = SailEntityMapper.ResolveTableName<T>();
            return (kit ?? NewKit()).select("*").from(table).where(colName, value);
        }

        public static SQLBuilder PrepareDeleteByIds<T>(IEnumerable<object> ids, SQLBuilder kit = null) where T : class
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));
            var table = SailEntityMapper.ResolveTableName<T>();
            var list = ids as IList<object> ?? ids.ToList();
            return (kit ?? NewKit()).setTable(table).whereIn(KeyColumn<T>(), list);
        }

        public static string ResolveSql(SQLCmd cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException(nameof(cmd));
            if (cmd.para != null)
                return cmd.para.ResolveDelayParas(cmd.sql) ?? cmd.sql;
            return cmd.sql;
        }

        /// <summary>同 conn/trans 执行（ADO；不经 Dapper）。</summary>
        public static int Execute(IDbConnection conn, SQLCmd cmd, IDbTransaction transaction = null)
            => SailCmdRunner.ExecuteOnConn(conn, ResolveSql(cmd), cmd?.para, transaction);

        /// <summary>同 conn/trans 查询实体（ADO + SailEntityMapper）。</summary>
        public static IEnumerable<T> Query<T>(IDbConnection conn, SQLCmd cmd, IDbTransaction transaction = null) where T : class
            => SailCmdRunner.QueryEntitiesOnConn<T>(conn, ResolveSql(cmd), cmd?.para, transaction);

        public static T QueryFirstOrDefault<T>(IDbConnection conn, SQLCmd cmd, IDbTransaction transaction = null) where T : class
            => Query<T>(conn, cmd, transaction).FirstOrDefault();
    }
}
