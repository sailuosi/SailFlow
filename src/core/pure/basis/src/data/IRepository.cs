using System;
using System.Collections.Generic;
using System.Data;

namespace saiflow.data
{
    /// <summary>
    /// Data Repository Interface（Phase F-1：已去掉 IPredicate / ISort）。
    /// 实体 CRUD 走 SQLBuilder；带裸 SQL 字符串的重载已 Obsolete，请改 <c>UseSQL()</c>。
    /// </summary>
    public interface IRepository
    {
        //select — 实体路径（推荐）
        T GetById<T>(dynamic primaryId) where T : class;
        T GetById<T>(IDbConnection conn, dynamic primaryId, IDbTransaction trans) where T : class;
        T GetDefaultByName<T>(string colName, string value) where T : class;
        IEnumerable<T> GetByIds<T>(IList<dynamic> ids) where T : class;
        IEnumerable<T> GetByIds<T>(IDbConnection conn, IList<dynamic> ids, IDbTransaction trans = null, bool buffered = true) where T : class;
        IEnumerable<T> GetAll<T>() where T : class;
        IEnumerable<T> GetAll<T>(IDbConnection conn, IDbTransaction trans) where T : class;

        /// <summary>过渡：裸 SQL。请改 <c>SailDbAccess.GetRequired().useSQL()</c> / <c>SailSqlAccess.UseRunSQL()</c>。</summary>
        [Obsolete(SailRepoObsolete.RawSql)]
        T GetFirst<T>(string sql, dynamic param = null, bool buffered = true) where T : class;

        /// <summary>过渡：裸 SQL。</summary>
        [Obsolete(SailRepoObsolete.RawSql)]
        T GetFirst<T>(IDbConnection conn, string sql, dynamic param = null, IDbTransaction trans = null, bool buffered = true) where T : class;

        /// <summary>过渡：裸 SQL。</summary>
        [Obsolete(SailRepoObsolete.RawSql)]
        IEnumerable<T> Query<T>(string sql, dynamic param = null, bool buffered = true) where T : class;

        /// <summary>过渡：裸 SQL。</summary>
        [Obsolete(SailRepoObsolete.RawSql)]
        IEnumerable<T> Query<T>(IDbConnection conn, string sql, dynamic param = null, IDbTransaction trans = null, bool buffered = true) where T : class;

        /// <summary>过渡：裸 SQL。</summary>
        [Obsolete(SailRepoObsolete.RawSql)]
        IEnumerable<dynamic> Query(IDbConnection conn, string sql, dynamic param = null, IDbTransaction trans = null, bool buffered = true);

        /// <summary>过渡：裸 SQL。</summary>
        [Obsolete(SailRepoObsolete.RawSql)]
        int Count<T>(IDbConnection conn, string sql, bool buffered = false) where T : class;

        /// <summary>过渡：裸 SQL。</summary>
        [Obsolete(SailRepoObsolete.RawSql)]
        int Count(string sql, object parameters = null);

        /// <summary>存储过程分页（过渡）。请改用 SQLBuilder <c>setPage</c>。</summary>
        [Obsolete("Use SQLBuilder setPage instead of pr_sys_QueryPaged.")]
        IEnumerable<T> GetPaged<T>(IDbConnection conn, Pager pager, bool buffered = false) where T : class;

        /// <summary>过渡：裸 SQL。</summary>
        [Obsolete(SailRepoObsolete.RawSql)]
        Int32 Execute(IDbConnection conn, string sql, dynamic param = null, IDbTransaction transaction = null);

        // 存储过程：F2-f 再迁；暂不 Obsolete，避免与动作引擎冲突过多
        Int32 ExecuteProc(string procName, object param = null);
        Int32 ExecuteProc(IDbConnection conn, string procName, object param = null);
        Int32 ExecuteProc(IDbConnection conn, string procName, object param = null, IDbTransaction transaction = null);
        IList<T> ExecProcQuery<T>(IDbConnection conn, string procName, object param) where T : class;
        IList<T> ExecProcQuery<T>(string procName, object param) where T : class;

        //insert, update, delete
        dynamic Insert<T>(T entity) where T : class;
        dynamic Insert<T>(IDbConnection conn, T entity, IDbTransaction transaction = null) where T : class;
        void InsertBatch<T>(IDbConnection conn, IEnumerable<T> entityList, IDbTransaction transaction = null) where T : class;
        bool Update<T>(T entity) where T : class;
        bool Update<T>(IDbConnection conn, T entity, IDbTransaction transaction = null) where T : class;
        bool UpdateBatch<T>(IDbConnection conn, IEnumerable<T> entityList, IDbTransaction transaction = null) where T : class;
        bool Delete<T>(dynamic primaryId) where T : class;
        bool Delete<T>(IDbConnection conn, dynamic primaryId, IDbTransaction transaction = null) where T : class;
        int DeleteBatch<T>(IDbConnection conn, IEnumerable<dynamic> ids, IDbTransaction transaction = null) where T : class;
    }

    /// <summary>IRepository 废弃提示文案。</summary>
    public static class SailRepoObsolete
    {
        public const string RawSql =
            "Prefer SQLBuilder: SailDbAccess.GetRequired().useSQL() or SailSqlAccess.UseRunSQL(). Raw SQL overloads are transitional (Phase F-2).";
    }
}
