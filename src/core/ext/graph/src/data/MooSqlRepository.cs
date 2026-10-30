using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using mooSQL.data;

namespace saiflow.data
{
    /// <summary>
    /// 任务 1 Phase C：基于 mooSQL 的仓储。
    /// 实体 CRUD / 动态 SQL / Proc 均不经 Dapper <see cref="Repository"/> 实例（F2-g1）。
    /// </summary>
    public class MooSqlRepository : IRepository
    {
        private static DBInstance Db => SailDbAccess.GetRequired();

        #region helpers

        private static Paras ToParas(object param) => SailParamConverter.ToParas(param);

        #endregion

        #region select

        public T GetById<T>(dynamic primaryId) where T : class
            => SailEntityKit.PrepareSelectById<T>((object)primaryId).queryUnique<T>();

        public T GetById<T>(IDbConnection conn, dynamic primaryId, IDbTransaction trans) where T : class
            => SailEntityKit.QueryFirstOrDefault<T>(conn, SailEntityKit.PrepareSelectById<T>((object)primaryId).toSelect(), trans);

        public T GetDefaultByName<T>(string colName, string value) where T : class
            => SailEntityKit.PrepareSelectByName<T>(colName, value).queryUnique<T>();

        [Obsolete(SailRepoObsolete.RawSql)]
        public T GetFirst<T>(string sql, dynamic param = null, bool buffered = true) where T : class
            => SailCmdRunner.QueryEntities<T>(sql, param as object).FirstOrDefault();

        [Obsolete(SailRepoObsolete.RawSql)]
        public T GetFirst<T>(IDbConnection conn, string sql, dynamic param = null, IDbTransaction trans = null, bool buffered = true) where T : class
            => SailCmdRunner.QueryEntitiesOnConn<T>(conn, sql, param as object, trans).FirstOrDefault();

        public IEnumerable<T> GetByIds<T>(IList<dynamic> ids) where T : class
        {
            if (ids == null || ids.Count == 0)
                return Enumerable.Empty<T>();
            return SailEntityKit.PrepareSelectByIds<T>(ids.Cast<object>()).query<T>();
        }

        public IEnumerable<T> GetByIds<T>(IDbConnection conn, IList<dynamic> ids, IDbTransaction trans = null, bool buffered = true) where T : class
        {
            if (ids == null || ids.Count == 0)
                return Enumerable.Empty<T>();
            return SailEntityKit.Query<T>(conn, SailEntityKit.PrepareSelectByIds<T>(ids.Cast<object>()).toSelect(), trans);
        }

        public IEnumerable<T> GetAll<T>() where T : class
            => SailEntityKit.PrepareSelectAll<T>().query<T>();

        public IEnumerable<T> GetAll<T>(IDbConnection conn, IDbTransaction trans) where T : class
            => SailEntityKit.Query<T>(conn, SailEntityKit.PrepareSelectAll<T>().toSelect(), trans);

        [Obsolete(SailRepoObsolete.RawSql)]
        public IEnumerable<T> Query<T>(string sql, dynamic param = null, bool buffered = true) where T : class
            => SailCmdRunner.QueryEntities<T>(sql, param as object);

        [Obsolete(SailRepoObsolete.RawSql)]
        public IEnumerable<T> Query<T>(IDbConnection conn, string sql, dynamic param = null, IDbTransaction trans = null, bool buffered = true) where T : class
            => SailCmdRunner.QueryEntitiesOnConn<T>(conn, sql, param as object, trans);

        [Obsolete(SailRepoObsolete.RawSql)]
        public IEnumerable<dynamic> Query(IDbConnection conn, string sql, dynamic param = null, IDbTransaction trans = null, bool buffered = true)
        {
            var table = SailCmdRunner.QueryOnConn(conn, sql, param as object, trans);
            return table.AsEnumerable().Select(row =>
            {
                IDictionary<string, object> bag = new System.Dynamic.ExpandoObject();
                foreach (DataColumn col in table.Columns)
                    bag[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                return (dynamic)bag;
            });
        }

        #endregion

        #region count / page

        [Obsolete(SailRepoObsolete.RawSql)]
        public int Count<T>(IDbConnection conn, string sql, bool buffered = false) where T : class
        {
            var table = SailCmdRunner.QueryOnConn(conn, sql);
            if (table == null || table.Rows.Count == 0)
                return 0;
            return Convert.ToInt32(table.Rows[0][0]);
        }

        [Obsolete(SailRepoObsolete.RawSql)]
        public int Count(string sql, object parameters = null)
        {
            var dt = Db.ExeQuery(sql, ToParas(parameters));
            if (dt == null || dt.Rows.Count == 0)
                return 0;
            return Convert.ToInt32(dt.Rows[0][0]);
        }

        [Obsolete("Use SQLBuilder setPage instead of pr_sys_QueryPaged.")]
        public IEnumerable<T> GetPaged<T>(IDbConnection conn, Pager pager, bool buffered = false) where T : class
        {
            throw new NotSupportedException(
                "GetPaged(Pager) via pr_sys_QueryPaged is retired. Use SQLBuilder setPage / SailCmdRunner instead.");
        }

        #endregion

        #region execute

        [Obsolete(SailRepoObsolete.RawSql)]
        public Int32 Execute(IDbConnection conn, string sql, dynamic param = null, IDbTransaction transaction = null)
            => SailCmdRunner.ExecuteOnConn(conn, sql, param as object, transaction);

        public Int32 ExecuteProc(string procName, object param = null)
            => SailCmdRunner.Execute(procName, param, CommandType.StoredProcedure);

        public Int32 ExecuteProc(IDbConnection conn, string procName, object param = null)
            => SailCmdRunner.ExecuteOnConn(conn, procName, param, null, CommandType.StoredProcedure);

        public Int32 ExecuteProc(IDbConnection conn, string procName, object param = null, IDbTransaction transaction = null)
            => SailCmdRunner.ExecuteOnConn(conn, procName, param, transaction, CommandType.StoredProcedure);

        public IList<T> ExecProcQuery<T>(IDbConnection conn, string procName, object param) where T : class
            => SailCmdRunner.QueryEntitiesOnConn<T>(conn, procName, param, null, CommandType.StoredProcedure);

        public IList<T> ExecProcQuery<T>(string procName, object param) where T : class
            => SailCmdRunner.QueryEntities<T>(procName, param, CommandType.StoredProcedure);

        #endregion

        #region insert / update / delete

        public dynamic Insert<T>(T entity) where T : class
        {
            var prepared = SailEntityKit.PrepareInsert(entity);
            prepared.Kit.doInsert();
            // 身份回填（RETURNING）暂略；调用方若依赖 newId 需后续按方言补齐
            return prepared.KeyValue ?? 0;
        }

        public dynamic Insert<T>(IDbConnection conn, T entity, IDbTransaction transaction = null) where T : class
        {
            var prepared = SailEntityKit.PrepareInsert(entity);
            SailEntityKit.Execute(conn, prepared.Kit.toInsert(), transaction);
            return prepared.KeyValue ?? 0;
        }

        public void InsertBatch<T>(IDbConnection conn, IEnumerable<T> entityList, IDbTransaction transaction = null) where T : class
        {
            if (entityList == null)
                return;
            foreach (var entity in entityList)
                Insert(conn, entity, transaction);
        }

        public bool Update<T>(T entity) where T : class
            => SailEntityKit.PrepareUpdate(entity).doUpdate() > 0;

        public bool Update<T>(IDbConnection conn, T entity, IDbTransaction transaction = null) where T : class
            => SailEntityKit.Execute(conn, SailEntityKit.PrepareUpdate(entity).toUpdate(), transaction) > 0;

        public bool UpdateBatch<T>(IDbConnection conn, IEnumerable<T> entityList, IDbTransaction transaction = null) where T : class
        {
            if (entityList == null)
                return false;
            var ok = true;
            foreach (var entity in entityList)
                ok = Update(conn, entity, transaction) && ok;
            return ok;
        }

        public bool Delete<T>(dynamic primaryId) where T : class
            => SailEntityKit.PrepareDeleteById<T>((object)primaryId).doDelete() > 0;

        public bool Delete<T>(IDbConnection conn, dynamic primaryId, IDbTransaction transaction = null) where T : class
            => SailEntityKit.Execute(conn, SailEntityKit.PrepareDeleteById<T>((object)primaryId).toDelete(), transaction) > 0;

        public int DeleteBatch<T>(IDbConnection conn, IEnumerable<dynamic> ids, IDbTransaction transaction = null) where T : class
        {
            if (ids == null)
                return 0;
            var list = ids.Cast<object>().ToList();
            if (list.Count == 0)
                return 0;
            return SailEntityKit.Execute(conn, SailEntityKit.PrepareDeleteByIds<T>(list).toDelete(), transaction);
        }

        #endregion
    }
}
