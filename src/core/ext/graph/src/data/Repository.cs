
using System;
using System.ComponentModel;
using System.Data;
using System.Dynamic;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace saiflow.data
{
    /// <summary>
    /// Repository Class
    /// Repository基类（遗留实现；已不依赖 Dapper；默认请用 MooSqlRepository）
    /// </summary>
    public class Repository : IRepository
    {
        public Repository()
        {

        }

        /// <summary>
        /// Data 内部建连：仅经 mooSQL（宿主须先 <see cref="SailDbAccess.Use"/>）。
        /// </summary>
        private static IDbConnection OpenConnection()
            => SailDbAccess.CreateOpenConnection();

        /// <summary>
        /// Retrieve records based on primary key Id
        /// 根据主键ID获取记录
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="primaryId"></param>
        /// <returns></returns>
        public T GetById<T>(dynamic primaryId) where T : class
        {
            using (IDbConnection conn = OpenConnection())
            {
                return GetById<T>(conn, primaryId, null);
            }
        }

        /// <summary>
        /// Obtain entity based on Id
        /// 根据Id获取实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="conn">Connection</param>
        /// <param name="primaryId">Primary Key Id</param>
        /// <param name="trans">Transaction</param>
        /// <returns></returns>
        public T GetById<T>(IDbConnection conn, dynamic primaryId, IDbTransaction trans) where T : class
            => SailEntityKit.QueryFirstOrDefault<T>(conn, SailEntityKit.PrepareSelectById<T>((object)primaryId).toSelect(), trans);

        /// <summary>
        /// Retrieve entities based on field column names
        /// 根据字段列名称获取实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="colName">Field Name</param>
        /// <param name="value">Value</param>
        /// <returns></returns>
        public T GetDefaultByName<T>(string colName, string value) where T : class
        {
            var dataList = GetByName<T>(colName, value).ToList<T>();

            if (dataList.Count() > 0)
                return dataList.FirstOrDefault<T>();
            else
                return null;
        }

        /// <summary>
        /// Get table name
        /// 获取表名称
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public string GetTableName<T>() where T : class
            => SailEntityMapper.ResolveTableName<T>();

        /// <summary>
        /// Retrieve entities based on field column names
        /// 根据字段列名称获取实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="colName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public IList<T> GetByName<T>(string colName, string value) where T : class
        {
            using (IDbConnection conn = OpenConnection())
            {
                return SailEntityKit.Query<T>(conn, SailEntityKit.PrepareSelectByName<T>(colName, value).toSelect()).ToList();
            }
        }

        /// <summary>
        /// Obtain multiple entities based on multiple IDs
        /// 根据多个Id获取多个实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ids">id list</param>
        /// <returns>entity list</returns>
        public IEnumerable<T> GetByIds<T>(IList<dynamic> ids) where T : class
        {
            using (IDbConnection conn = OpenConnection())
            {
                return GetByIds<T>(conn, ids);
            }
        }

        /// <summary>
        /// Obtain multiple entities based on multiple IDs
        /// 根据多个Id获取多个实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="conn"></param>
        /// <param name="ids"></param>
        /// <param name="trans"></param>
        /// <param name="buffered"></param>
        /// <returns></returns>
        public IEnumerable<T> GetByIds<T>(IDbConnection conn, IList<dynamic> ids, IDbTransaction trans = null, bool buffered = true) where T : class
        {
            if (ids == null || ids.Count == 0)
                return Enumerable.Empty<T>();
            return SailEntityKit.Query<T>(conn, SailEntityKit.PrepareSelectByIds<T>(ids.Cast<object>()).toSelect(), trans);
        }

        /// <summary>
        /// Obtain all data sets
        /// 获取全部数据集合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetAll<T>() where T : class
        {
            using (IDbConnection conn = OpenConnection())
            {
                return GetAll<T>(conn, null).ToList();
            }
        }

        /// <summary>
        /// Obtain all data sets
        /// 获取全部数据集合
        /// </summary>
        public IEnumerable<T> GetAll<T>(IDbConnection conn, IDbTransaction trans) where T : class
            => SailEntityKit.Query<T>(conn, SailEntityKit.PrepareSelectAll<T>().toSelect(), trans);

        /// <summary>
        /// Search for a matching piece of data
        /// 查询匹配的一条数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="buffered"></param>
        /// <returns></returns>
        [Obsolete(SailRepoObsolete.RawSql)]
        public T GetFirst<T>(string sql, dynamic param = null, bool buffered = true) where T : class
            => SailCmdRunner.QueryEntities<T>(sql, param as object).FirstOrDefault();

        /// <summary>
        /// Obtain the first data queried
        /// </summary>
        [Obsolete(SailRepoObsolete.RawSql)]
        public T GetFirst<T>(IDbConnection conn, string sql, dynamic param = null, IDbTransaction trans = null,
            bool buffered = true) where T : class
            => SailCmdRunner.QueryEntitiesOnConn<T>(conn, sql, param as object, trans).FirstOrDefault();

        /// <summary>
        /// Filter the dataset based on the conditions
        /// </summary>
        [Obsolete(SailRepoObsolete.RawSql)]
        public IEnumerable<T> Query<T>(string sql, dynamic param = null, bool buffered = true) where T : class
            => SailCmdRunner.QueryEntities<T>(sql, param as object);

        /// <summary>
        /// Filter the dataset based on the conditions
        /// </summary>
        [Obsolete(SailRepoObsolete.RawSql)]
        public IEnumerable<T> Query<T>(IDbConnection conn, string sql, dynamic param = null, IDbTransaction trans = null, bool buffered = true) where T : class
            => SailCmdRunner.QueryEntitiesOnConn<T>(conn, sql, param as object, trans);

        /// <summary>
        /// Filter the dataset based on the conditions
        /// </summary>
        [Obsolete(SailRepoObsolete.RawSql)]
        public IEnumerable<dynamic> Query(IDbConnection conn, string sql, dynamic param = null, IDbTransaction trans = null, bool buffered = true)
        {
            var table = SailCmdRunner.QueryOnConn(conn, sql, param as object, trans);
            return table.AsEnumerable().Select(row =>
            {
                IDictionary<string, object> bag = new ExpandoObject();
                foreach (DataColumn col in table.Columns)
                    bag[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                return (dynamic)bag;
            });
        }

        /// <summary>
        /// Paging query (stored procedure) — obsolete; prefer SQLBuilder setPage.
        /// </summary>
        [Obsolete("Use SQLBuilder setPage instead of pr_sys_QueryPaged.")]
        public IEnumerable<T> GetPaged<T>(IDbConnection conn, Pager pager, bool buffered = false) where T : class
        {
            var tblName = string.IsNullOrEmpty(pager.TableName) ? GetTableName<T>() : pager.TableName;
            var keyFieldName = string.IsNullOrEmpty(pager.KeyFieldName) ? "id" : pager.KeyFieldName;
            var p = new Dictionary<string, object>
            {
                ["@pageIndex"] = pager.PageIndex,
                ["@pageSize"] = pager.PageSize,
                ["@tblName"] = tblName,
                ["@fldName"] = keyFieldName,
                ["@isDesc"] = pager.IsDesc,
                ["@strWhere"] = pager.StrWhere,
                ["@fldOrder"] = pager.FieldOrder
            };
            return SailCmdRunner.QueryEntitiesOnConn<T>(conn, "pr_sys_QueryPaged", p, null, CommandType.StoredProcedure);
        }

        /// <summary>
        /// Total number of statistical records
        /// </summary>
        [Obsolete(SailRepoObsolete.RawSql)]
        public int Count<T>(IDbConnection conn, string sql, bool buffered = false) where T : class
        {
            var table = SailCmdRunner.QueryOnConn(conn, sql);
            if (table == null || table.Rows.Count == 0)
                return 0;
            return Convert.ToInt32(table.Rows[0][0]);
        }

        /// <summary>
        /// Sum of Count in SQL with Parameters
        /// </summary>
        [Obsolete(SailRepoObsolete.RawSql)]
        public int Count(string sql, object parameters = null)
        {
            var dt = SailCmdRunner.Query(sql, parameters);
            if (dt == null || dt.Rows.Count == 0)
                return 0;
            return Convert.ToInt32(dt.Rows[0][0]);
        }

        /// <summary>
        /// Execute SQL operations
        /// </summary>
        [Obsolete(SailRepoObsolete.RawSql)]
        public int Execute(IDbConnection conn, string sql, dynamic param = null, IDbTransaction transaction = null)
            => SailCmdRunner.ExecuteOnConn(conn, sql, param as object, transaction);

        /// <summary>
        /// Execute SQL Procedure
        /// </summary>
        public int ExecuteProc(string procName, object param = null)
            => SailCmdRunner.Execute(procName, param, CommandType.StoredProcedure);

        /// <summary>
        /// Execute SQL Procedure
        /// </summary>
        public int ExecuteProc(IDbConnection conn, string procName, object param = null)
            => SailCmdRunner.ExecuteOnConn(conn, procName, param, null, CommandType.StoredProcedure);

        /// <summary>
        /// Execute SQL Procedure
        /// </summary>
        public int ExecuteProc(IDbConnection conn, string procName, object param = null, IDbTransaction transaction = null)
            => SailCmdRunner.ExecuteOnConn(conn, procName, param, transaction, CommandType.StoredProcedure);

        /// <summary>
        /// Execute SQL Procedure
        /// </summary>
        public IList<T> ExecProcQuery<T>(IDbConnection conn, string procName, object param)
            where T : class
            => SailCmdRunner.QueryEntitiesOnConn<T>(conn, procName, param, null, CommandType.StoredProcedure);

        /// <summary>
        /// Execute SQL Procedure
        /// </summary>
        public IList<T> ExecProcQuery<T>(string procName, object param)
            where T : class
            => SailCmdRunner.QueryEntities<T>(procName, param, CommandType.StoredProcedure);

        /// <summary>
        /// Execute SQL statements and return query results
        /// 执行SQL语句，返回查询结果
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="sql"></param>
        /// <param name="buffered"></param>
        /// <returns></returns>
        public object ExecuteScalar(IDbConnection conn, string sql, bool buffered = false)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;

            return ExecuteScalar(conn, cmd);
        }

        /// <summary>
        /// Execute SQL statements and return query value
        /// 执行SQL语句，并返回数值
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="conn"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public object ExecuteScalar(IDbConnection conn, IDbCommand cmd)
        {
            try
            {
                bool wasClosed = conn.State == ConnectionState.Closed;
                if (wasClosed) conn.Open();

                return cmd.ExecuteScalar();
            }
            catch (System.Exception)
            {
                throw;
            }
            finally
            {
                if (cmd != null)
                    cmd.Dispose();
            }
        }

        /// <summary>
        /// Insert Entity
        /// 插入实体
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="entity">实体</param>
        /// <returns>主键自增值</returns>
        public dynamic Insert<T>(T entity) where T : class
        {
            using (var kit = SailEntityKit.NewKit())
            {
                kit.beginTransaction();
                try
                {
                    var prepared = SailEntityKit.PrepareInsert(entity, kit);
                    prepared.Kit.doInsert();
                    kit.commit();
                    return prepared.KeyValue ?? 0;
                }
                catch
                {
                    throw;
                }
            }
        }


        /// <summary>
        /// Insert Single Record
        /// 插入单条记录
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public dynamic Insert<T>(IDbConnection conn, T entity, IDbTransaction transaction = null) where T : class
        {
            var prepared = SailEntityKit.PrepareInsert(entity);
            SailEntityKit.Execute(conn, prepared.Kit.toInsert(), transaction);
            return prepared.KeyValue ?? 0;
        }

        /// <summary>
        /// Update
        /// 修改
        /// </summary>
        public bool Update<T>(T entity) where T : class
        {
            using (var kit = SailEntityKit.NewKit())
            {
                kit.beginTransaction();
                try
                {
                    var ok = SailEntityKit.PrepareUpdate(entity, kit).doUpdate() > 0;
                    kit.commit();
                    return ok;
                }
                catch
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Update Single Record
        /// 更新单条记录
        /// </summary>
        public bool Update<T>(IDbConnection conn, T entity, IDbTransaction transaction = null) where T : class
            => SailEntityKit.Execute(conn, SailEntityKit.PrepareUpdate(entity).toUpdate(), transaction) > 0;

        /// <summary>
        /// Delete
        /// 删除
        /// </summary>
        public bool Delete<T>(dynamic id) where T : class
        {
            using (var kit = SailEntityKit.NewKit())
            {
                kit.beginTransaction();
                try
                {
                    var ok = SailEntityKit.PrepareDeleteById<T>((object)id, kit).doDelete() > 0;
                    kit.commit();
                    return ok;
                }
                catch
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Delete Single Record by primary id
        /// </summary>
        public bool Delete<T>(IDbConnection conn, dynamic primaryId, IDbTransaction transaction = null) where T : class
            => SailEntityKit.Execute(conn, SailEntityKit.PrepareDeleteById<T>((object)primaryId).toDelete(), transaction) > 0;

        /// <summary>
        /// Batchly Insert
        /// 批量插入功能
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entityList"></param>
        public void InsertBatch<T>(IDbConnection conn, IEnumerable<T> entityList, IDbTransaction transaction = null) where T : class
        {
            if (entityList == null)
                return;
            foreach (var entity in entityList)
                Insert(conn, entity, transaction);
        }

        /// <summary>
        /// Update Multiple
        /// 批量更新
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entityList"></param>
        /// <returns></returns>
        public bool UpdateBatch<T>(IDbConnection conn, IEnumerable<T> entityList, IDbTransaction transaction = null) where T : class
        {
            bool isOk = false;
            foreach (var item in entityList)
            {
                Update<T>(conn, item, transaction);
            }
            isOk = true;
            return isOk;
        }

        /// <summary>
        /// Delete Multiple
        /// 批量删除
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ids"></param>
        /// <returns></returns>
        public int DeleteBatch<T>(IDbConnection conn, IEnumerable<dynamic> ids, IDbTransaction transaction = null) where T : class
        {
            if (ids == null)
                return 0;
            var list = ids.Cast<object>().ToList();
            if (list.Count == 0)
                return 0;
            return SailEntityKit.Execute(conn, SailEntityKit.PrepareDeleteByIds<T>(list).toDelete(), transaction);
        }
    }
}