using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using mooSQL.data;

namespace sailflow.data
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

    }
}
