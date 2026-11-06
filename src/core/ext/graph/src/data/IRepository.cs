using System.Collections.Generic;
using System.Data;

namespace saiflow.data
{
    /// <summary>
    /// 实体 CRUD 仓储（过渡面）。裸 SQL / 分页 SP / Proc 已迁 <c>UseRunSQL()</c> / <c>SailCmdRunner</c>。
    /// </summary>
    public interface IRepository
    {
        T GetById<T>(dynamic primaryId) where T : class;
        T GetById<T>(IDbConnection conn, dynamic primaryId, IDbTransaction trans) where T : class;
        T GetDefaultByName<T>(string colName, string value) where T : class;
        IEnumerable<T> GetByIds<T>(IList<dynamic> ids) where T : class;
        IEnumerable<T> GetByIds<T>(IDbConnection conn, IList<dynamic> ids, IDbTransaction trans = null, bool buffered = true) where T : class;
        IEnumerable<T> GetAll<T>() where T : class;
        IEnumerable<T> GetAll<T>(IDbConnection conn, IDbTransaction trans) where T : class;

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
}
