using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using saiflow.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Client;

namespace saiflow.engine.Business.Manager
{
    public class HookOutboxManager
    {
        public HookOutboxManager(SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        public void InsertPending(SQLBuilder kit, HookOutboxEntity entity)
        {
            entity.Status = HookOutboxStatus.Pending;
            entity.Attempt = 0;
            entity.CreatedAt = DateTime.UtcNow;
            Sibling(kit).useRepo<HookOutboxEntity>().Insert(entity);
        }

        public IList<HookOutboxEntity> GetPending(SQLBuilder kit, int limit = 50)
        {
            var now = DateTime.UtcNow;
            var clip = Sibling(kit).useClip();
            clip.from<HookOutboxEntity>(out var o);
            clip.where(() => o.Status, HookOutboxStatus.Pending);
            return clip.select(o).queryList()
                .Where(x => x.NextRetryAt == null || x.NextRetryAt <= now)
                .OrderBy(x => x.Id)
                .Take(limit)
                .ToList();
        }

        public void MarkSucceeded(SQLBuilder kit, HookOutboxEntity row)
        {
            row.Status = HookOutboxStatus.Succeeded;
            row.UpdatedAt = DateTime.UtcNow;
            Sibling(kit).useRepo<HookOutboxEntity>().Update(row);
        }

        public void MarkFailed(SQLBuilder kit, HookOutboxEntity row, string error, int maxAttempts = 5)
        {
            row.Attempt++;
            row.LastError = Truncate(error, 2000);
            row.UpdatedAt = DateTime.UtcNow;
            if (row.Attempt >= maxAttempts)
            {
                row.Status = HookOutboxStatus.Dead;
            }
            else
            {
                row.Status = HookOutboxStatus.Pending;
                row.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, Math.Min(row.Attempt, 6)));
            }

            Sibling(kit).useRepo<HookOutboxEntity>().Update(row);
        }

        private static SQLBuilder Sibling(SQLBuilder kit) => SailSqlAccess.Sibling(kit);

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= max)
                return value;
            return value.Substring(0, max);
        }
    }
}
