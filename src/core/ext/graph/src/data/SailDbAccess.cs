using System;
using mooSQL.data;

namespace sailflow.data
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

        /// <summary>无会话：纯净实体仓储（任务 1 · SooRepository）。</summary>
        public static SooRepository<T> UseRepo<T>() where T : class, new()
            => GetRequired().useRepo<T>();

        /// <summary>无会话：带主键类型的纯净实体仓储。</summary>
        public static SooRepository<T, K> UseRepo<T, K>() where T : class, new()
            => GetRequired().useRepo<T, K>();
    }
}
