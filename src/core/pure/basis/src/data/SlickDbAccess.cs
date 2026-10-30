using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using mooSQL.data;

namespace saiflow.data
{
    /// <summary>
    /// 宿主装配时注入 <see cref="DBInstance"/> 的入口（非业务 Cash）。
    /// 由 sfdapi 等应用层在构建 <c>SailClient</c> 后调用 <see cref="Use"/>。
    /// </summary>
    public static class SailDbAccess
    {
        private static Func<DBInstance> _getDb;

        public static bool IsConfigured => _getDb != null;

        public static void Use(Func<DBInstance> getDb)
        {
            _getDb = getDb ?? throw new ArgumentNullException(nameof(getDb));
        }

        public static void Use(DBInstance db)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            _getDb = () => db;
        }

        public static void Clear() => _getDb = null;

        public static DBInstance TryGetDb() => _getDb?.Invoke();

        public static DBInstance GetRequired()
        {
            var db = TryGetDb();
            if (db == null)
                throw new InvalidOperationException(
                    "SailDbAccess is not configured. Call SailDbAccess.Use(...) from the application host when assembling SailClient.");
            return db;
        }

        /// <summary>经 mooSQL 方言创建并打开 ADO 连接（仅遗留 <c>Repository.OpenConnection</c>）。</summary>
        public static IDbConnection CreateOpenConnection()
        {
            var db = GetRequired();
            if (db.dialect == null)
                throw new InvalidOperationException("DBInstance.dialect is null; ensure mooSQL.Ext DialectFactory is used (DBClientBuilder).");

            DbConnection conn = db.dialect.getConnection();
            if (conn.State == ConnectionState.Closed)
                conn.Open();
            return conn;
        }

        /// <summary>无会话：纯净实体仓储（任务 1 · SooRepository）。</summary>
        public static SooRepository<T> UseRepo<T>() where T : class, new()
            => GetRequired().useRepo<T>();

        /// <summary>无会话：带主键类型的纯净实体仓储。</summary>
        public static SooRepository<T, K> UseRepo<T, K>() where T : class, new()
            => GetRequired().useRepo<T, K>();
    }

    /// <summary>
    /// <see cref="IRepository"/> 工厂。默认 <see cref="MooSqlRepository"/>；宿主可显式切换。
    /// </summary>
    public static class RepositoryProvider
    {
        private static Func<IRepository> _factory = () => new MooSqlRepository();

        public static void Use(Func<IRepository> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public static void UseMooSql() => Use(() => new MooSqlRepository());

        [Obsolete("Dapper Repository 已去 Dapper 依赖；请用 UseMooSql()。")]
        public static void UseDapper() => Use(() => new Repository());

        public static IRepository Create() => _factory();
    }
}
