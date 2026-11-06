using mooSQL.data;

namespace saiflow.data
{
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
