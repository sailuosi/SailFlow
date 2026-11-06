using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace saiflow.data
{
    /// <summary>
    /// 基于 mooSQL 的实体仓储（过渡面；新代码优先 <c>kit.useRepo</c>）。
    /// </summary>
    public class MooSqlRepository : IRepository
    {
        #region select

        public T GetById<T>(dynamic primaryId) where T : class
            => SailEntityKit.PrepareSelectById<T>((object)primaryId).queryUnique<T>();

        public T GetById<T>(IDbConnection conn, dynamic primaryId, IDbTransaction trans) where T : class
            => SailEntityKit.QueryFirstOrDefault<T>(conn, SailEntityKit.PrepareSelectById<T>((object)primaryId).toSelect(), trans);

        public T GetDefaultByName<T>(string colName, string value) where T : class
            => SailEntityKit.PrepareSelectByName<T>(colName, value).queryUnique<T>();

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
