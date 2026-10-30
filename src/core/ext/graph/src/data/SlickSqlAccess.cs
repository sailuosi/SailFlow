using System;
using mooSQL.data;

namespace saiflow.data
{
    /// <summary>
    /// SQLBuilder 事务兄弟实例（纯函数，无进程静态绑定）。
    /// </summary>
    public static class SailSqlAccess
    {
        /// <summary>
        /// 同库新 SQLBuilder，继承 <paramref name="kit"/> 的事务（若有）。
        /// </summary>
        public static SQLBuilder Sibling(SQLBuilder kit)
        {
            if (kit == null) throw new ArgumentNullException(nameof(kit));
            var next = kit.DBLive.useSQL();
            if (kit.Executor != null)
                next.useTransaction(kit.Executor);
            return next;
        }
    }

    /// <summary>
    /// SQLBuilder 上取带主键仓储（继承 kit 事务）。
    /// </summary>
    public static class SailSqlBuilderExtensions
    {
        public static SooRepository<T, K> useRepo<T, K>(this SQLBuilder kit) where T : class, new()
        {
            var repo = kit.DBLive.useRepo<T, K>();
            if (kit.Executor != null)
                repo.useTransaction(kit.Executor);
            return repo;
        }
    }
}
