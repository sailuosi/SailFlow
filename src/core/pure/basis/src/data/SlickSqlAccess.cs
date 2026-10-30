using System;
using mooSQL.data;

namespace saiflow.data
{
    /// <summary>
    /// 进程内取运行库 SQLBuilder（由宿主绑定 Client.UseRunSQL）。
    /// Module / Data 可用；业务不碰 DBInstance。
    /// </summary>
    public static class SailSqlAccess
    {
        private static Func<SQLBuilder> _useRunSql;

        public static bool IsConfigured => _useRunSql != null;

        public static void Use(Func<SQLBuilder> useRunSql)
        {
            _useRunSql = useRunSql ?? throw new ArgumentNullException(nameof(useRunSql));
        }

        public static void Clear() => _useRunSql = null;

        public static SQLBuilder UseRunSQL()
        {
            if (_useRunSql == null)
                throw new InvalidOperationException(
                    "SailSqlAccess is not bound. Call SailSqlAccess.Use(() => client.UseRunSQL()) when assembling the host.");
            return _useRunSql();
        }

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
    /// 无主键版请用 mooSQL 自带的 <c>kit.useRepo&lt;T&gt;()</c>。
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
