using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using mooSQL.data;

namespace saiflow.data
{
    /// <summary>
    /// 动态/裸 SQL 与存储过程：走 mooSQL <see cref="SQLCmd"/> + <see cref="DBInstance"/>，
    /// 有 kit 时绑 <see cref="SQLBuilder.Executor"/> 同事务。
    /// </summary>
    public static class SailCmdRunner
    {
        public static SQLCmd Build(string sql, object param = null, CommandType? cmdType = null)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL / procedure name is required.", nameof(sql));
            return new SQLCmd(sql, SailParamConverter.ToParas(param)) { cmdType = cmdType };
        }

        /// <summary>kit 同事务执行 Text SQL。</summary>
        public static int Execute(SQLBuilder kit, string sql, object param = null)
        {
            if (kit == null) throw new ArgumentNullException(nameof(kit));
            return kit.DBLive.ExeNonQuery(Build(sql, param, CommandType.Text), kit.Executor);
        }

        public static int ExecuteProc(SQLBuilder kit, string procName, object param = null)
        {
            if (kit == null) throw new ArgumentNullException(nameof(kit));
            return kit.DBLive.ExeNonQuery(Build(procName, param, CommandType.StoredProcedure), kit.Executor);
        }


        public static DataTable Query(string sql, object param = null, CommandType? cmdType = null)
            => SailDbAccess.GetRequired().ExeQuery(Build(sql, param, cmdType));

        public static IList<T> QueryEntities<T>(string sql, object param = null, CommandType? cmdType = null) where T : class
            => MapRows<T>(Query(sql, param, cmdType));



        public static IList<T> MapRows<T>(DataTable table) where T : class
        {
            if (table == null || table.Rows.Count == 0)
                return new List<T>();

            var list = new List<T>(table.Rows.Count);
            var maps = SailEntityMapper.GetColumnMaps<T>();

            foreach (DataRow row in table.Rows)
            {
                var item = Activator.CreateInstance<T>();
                foreach (var map in maps)
                {
                    if (!map.Property.CanWrite)
                        continue;
                    DataColumn col = null;
                    if (table.Columns.Contains(map.ColumnName))
                        col = table.Columns[map.ColumnName];
                    else if (table.Columns.Contains(map.Property.Name))
                        col = table.Columns[map.Property.Name];
                    else
                    {
                        foreach (DataColumn c in table.Columns)
                        {
                            if (string.Equals(c.ColumnName, map.ColumnName, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(c.ColumnName, map.Property.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                col = c;
                                break;
                            }
                        }
                    }
                    if (col == null)
                        continue;
                    var raw = row[col];
                    if (raw == null || raw == DBNull.Value)
                        continue;
                    try
                    {
                        var targetType = Nullable.GetUnderlyingType(map.Property.PropertyType) ?? map.Property.PropertyType;
                        if (targetType.IsEnum)
                            map.Property.SetValue(item, Enum.ToObject(targetType, raw));
                        else
                            map.Property.SetValue(item, Convert.ChangeType(raw, targetType));
                    }
                    catch
                    {
                        // skip unconvertible
                    }
                }
                list.Add(item);
            }
            return list;
        }
    }
}
