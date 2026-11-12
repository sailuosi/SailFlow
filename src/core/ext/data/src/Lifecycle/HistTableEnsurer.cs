using System;
using System.Collections.Concurrent;
using mooSQL.data;

namespace sailflow.data.Lifecycle
{
    /// <summary>按年冷表 ENSURE（从热表零行拷结构；SQLite 友好，其它方言失败可重试/忽略）。</summary>
    public static class HistTableEnsurer
    {
        private static readonly ConcurrentDictionary<string, byte> Ensured =
            new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        public static void Ensure(SQLBuilder kit, string histTable, string hotTable)
        {
            if (kit == null)
                throw new ArgumentNullException(nameof(kit));
            if (string.IsNullOrWhiteSpace(histTable) || string.IsNullOrWhiteSpace(hotTable))
                return;
            if (string.Equals(histTable, hotTable, StringComparison.OrdinalIgnoreCase))
                return;
            if (!Ensured.TryAdd(histTable, 0))
                return;

            try
            {
                kit.exeNonQuery(
                    "CREATE TABLE IF NOT EXISTS " + histTable
                    + " AS SELECT * FROM " + hotTable + " WHERE 0=1");
            }
            catch
            {
                Ensured.TryRemove(histTable, out _);
                throw;
            }
        }
    }
}
