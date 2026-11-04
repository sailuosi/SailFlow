using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using saiflow.data;
using saiflow.engine.Entity;
using saiflow.engine.Client;
using saiflow.engine.Hooks;

namespace saiflow.engine.Manager
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

        public IList<HookOutboxEntity> GetPending(SQLBuilder kit, int limit = 50) =>
            GetPending(kit, limit, kind: null);

        /// <param name="kind">
        /// 非空时仅取该 Kind（如 <see cref="HookOutboxKinds.AssigneeAutoApprove"/>）。
        /// 为空时取 Hook/Notify 等，<b>排除</b> <see cref="HookOutboxKinds.AssigneeAutoApprove"/>（由 Assignee Job 单独认领）。
        /// </param>
        public IList<HookOutboxEntity> GetPending(SQLBuilder kit, int limit, string kind)
        {
            var now = DateTime.UtcNow;
            var clip = Sibling(kit).useClip();
            clip.from<HookOutboxEntity>(out var o);
            clip.where(() => o.Status, HookOutboxStatus.Pending);
            return clip.select(o).queryList()
                .Where(x =>
                {
                    if (!string.IsNullOrEmpty(kind))
                        return string.Equals(x.Kind, kind, StringComparison.OrdinalIgnoreCase);
                    // 默认 drain 不抢 Assignee 自动审批锚点
                    return !string.Equals(x.Kind, HookOutboxKinds.AssigneeAutoApprove, StringComparison.OrdinalIgnoreCase);
                })
                .Where(x => x.NextRetryAt == null || x.NextRetryAt <= now)
                .OrderBy(x => x.Id)
                .Take(limit)
                .ToList();
        }

        /// <summary>CAS Pending→Processing，返回成功认领的行（due 且未死信）。</summary>
        public IList<HookOutboxEntity> TryClaimPending(
            SQLBuilder kit,
            int limit,
            int maxAttempts = HookOutboxOptions.DefaultMaxAttempts,
            int leaseSeconds = HookOutboxOptions.DefaultLeaseSeconds) =>
            TryClaimPending(kit, limit, maxAttempts, leaseSeconds, kind: null);

        /// <summary>按 Kind 认领 Pending。</summary>
        public IList<HookOutboxEntity> TryClaimPending(
            SQLBuilder kit,
            int limit,
            int maxAttempts,
            int leaseSeconds,
            string kind)
        {
            _ = maxAttempts;
            var now = DateTime.UtcNow;
            var candidates = GetPending(kit, Math.Max(limit * 3, limit), kind);
            var claimed = new List<HookOutboxEntity>(limit);

            foreach (var candidate in candidates)
            {
                if (claimed.Count >= limit)
                    break;

                if (candidate.NextRetryAt != null && candidate.NextRetryAt > now)
                    continue;

                if (candidate.Id is not > 0)
                    continue;

                if (TryClaimOne(kit, candidate.Id.Value, leaseSeconds, out var row))
                    claimed.Add(row);
            }

            return claimed;
        }

        /// <summary>
        /// 回收僵尸 Processing：lease 过期，或无 lease 且 UpdatedAt 早于 staleBeforeUtc。
        /// </summary>
        public int ReclaimStaleProcessing(SQLBuilder kit, DateTime staleBeforeUtc, DateTime? nowUtc = null)
        {
            var now = nowUtc ?? DateTime.UtcNow;
            var clip = Sibling(kit).useClip();
            clip.from<HookOutboxEntity>(out var o);
            clip.where(() => o.Status, HookOutboxStatus.Processing);
            var rows = clip.select(o).queryList();
            var reclaimed = 0;

            foreach (var row in rows)
            {
                var isStale = row.LeasedUntil.HasValue
                    ? row.LeasedUntil < now
                    : row.UpdatedAt.HasValue && row.UpdatedAt < staleBeforeUtc;

                if (!isStale)
                    continue;

                var update = Sibling(kit).useClip();
                update.setTable<HookOutboxEntity>(out var u);
                update.set(() => u.Status, HookOutboxStatus.Pending);
                update.setToNull(() => u.LeasedUntil);
                update.set(() => u.UpdatedAt, now);
                update.where(() => u.Id, row.Id);
                update.where(() => u.Status, HookOutboxStatus.Processing);
                if (update.doUpdate() > 0)
                    reclaimed++;
            }

            return reclaimed;
        }

        public void ResetProcessing(SQLBuilder kit, int id)
        {
            var repo = Sibling(kit).useRepo<HookOutboxEntity>();
            var row = repo.GetById(id);
            if (row == null || row.Status != HookOutboxStatus.Processing)
                return;

            row.Status = HookOutboxStatus.Pending;
            row.LeasedUntil = null;
            row.UpdatedAt = DateTime.UtcNow;
            repo.Update(row);
        }

        public void MarkSucceeded(SQLBuilder kit, HookOutboxEntity row)
        {
            row.Status = HookOutboxStatus.Succeeded;
            row.UpdatedAt = DateTime.UtcNow;
            row.LeasedUntil = null;
            Sibling(kit).useRepo<HookOutboxEntity>().Update(row);
        }

        /// <summary>退回 Pending 并推迟（不增加 Attempt；用于任务尚未可见等软等待）。</summary>
        public void Postpone(SQLBuilder kit, HookOutboxEntity row, TimeSpan delay, string note = null)
        {
            row.Status = HookOutboxStatus.Pending;
            row.LeasedUntil = null;
            row.NextRetryAt = DateTime.UtcNow.Add(delay);
            row.UpdatedAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(note))
                row.LastError = Truncate(note, 2000);
            Sibling(kit).useRepo<HookOutboxEntity>().Update(row);
        }

        public void MarkFailed(SQLBuilder kit, HookOutboxEntity row, string error, int maxAttempts = HookOutboxOptions.DefaultMaxAttempts)
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
                row.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, Math.Min(row.Attempt.Value, 6)));
            }

            Sibling(kit).useRepo<HookOutboxEntity>().Update(row);
        }

        public void ResetDead(SQLBuilder kit, int id)
        {
            var repo = Sibling(kit).useRepo<HookOutboxEntity>();
            var row = repo.GetById(id);
            if (row == null || row.Status != HookOutboxStatus.Dead)
                return;

            row.Status = HookOutboxStatus.Pending;
            row.Attempt = 0;
            row.NextRetryAt = null;
            row.LastError = null;
            row.UpdatedAt = DateTime.UtcNow;
            repo.Update(row);
        }

        public IList<HookOutboxEntity> QueryByStatus(SQLBuilder kit, string status, int limit = 100)
        {
            var clip = Sibling(kit).useClip();
            clip.from<HookOutboxEntity>(out var o);
            clip.where(() => o.Status, status);
            clip.orderBy(() => o.Id);
            return clip.select(o).queryList().Take(limit).ToList();
        }

        public IList<HookOutboxEntity> QueryDead(SQLBuilder kit, int limit = 100) =>
            QueryDead(kit, limit, processCode: null, handlerName: null);

        public IList<HookOutboxEntity> QueryDead(
            SQLBuilder kit,
            int limit,
            string processCode,
            string handlerName)
        {
            var rows = QueryByStatus(kit, HookOutboxStatus.Dead, Math.Max(limit, 1) * 5);
            IEnumerable<HookOutboxEntity> filtered = rows;
            if (!string.IsNullOrEmpty(processCode))
            {
                filtered = filtered.Where(r =>
                    string.Equals(r.ProcessCode, processCode, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(handlerName))
            {
                filtered = filtered.Where(r =>
                    string.Equals(r.HandlerName, handlerName, StringComparison.OrdinalIgnoreCase));
            }

            return filtered.OrderBy(x => x.Id).Take(limit).ToList();
        }

        public int CountByStatus(SQLBuilder kit, string status)
        {
            var clip = Sibling(kit).useClip();
            clip.from<HookOutboxEntity>(out var o);
            clip.where(() => o.Status, status);
            return clip.select(o).queryList().Count();
        }

        public int ResetDeadBatch(SQLBuilder kit, IEnumerable<int> ids)
        {
            if (ids == null)
                return 0;

            var count = 0;
            foreach (var id in ids)
            {
                if (ResetDeadIfDead(kit, id))
                    count++;
            }

            return count;
        }

        public int ResetAllDead(SQLBuilder kit, int limit = 500)
        {
            var dead = QueryDead(kit, limit);
            var count = 0;
            foreach (var row in dead)
            {
                if (ResetDeadIfDead(kit, row.Id.Value))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// 按 TTL 删除终态行（Succeeded / Dead）。以 <c>UpdatedAt</c>（空则 <c>CreatedAt</c>）与保留天数比较。
        /// </summary>
        public int PurgeExpiredTerminal(
            SQLBuilder kit,
            int succeededRetentionDays,
            int deadRetentionDays,
            int batchSize = HookOutboxOptions.DefaultPurgeBatchSize,
            DateTime? nowUtc = null)
        {
            var now = nowUtc ?? DateTime.UtcNow;
            var limit = Math.Max(1, batchSize);
            var deleted = 0;

            if (succeededRetentionDays > 0)
            {
                deleted += PurgeStatusOlderThan(
                    kit,
                    HookOutboxStatus.Succeeded,
                    now.AddDays(-succeededRetentionDays),
                    limit);
            }

            if (deadRetentionDays > 0 && deleted < limit)
            {
                deleted += PurgeStatusOlderThan(
                    kit,
                    HookOutboxStatus.Dead,
                    now.AddDays(-deadRetentionDays),
                    limit - deleted);
            }

            return deleted;
        }

        private int PurgeStatusOlderThan(SQLBuilder kit, string status, DateTime olderThanUtc, int limit)
        {
            if (limit <= 0)
                return 0;

            var clip = Sibling(kit).useClip();
            clip.from<HookOutboxEntity>(out var o);
            clip.where(() => o.Status, status);
            clip.orderBy(() => o.Id);
            var candidates = clip.select(o).queryList()
                .Where(r =>
                {
                    var ts = r.UpdatedAt ?? r.CreatedAt;
                    return ts.HasValue && ts.Value < olderThanUtc;
                })
                .Take(limit)
                .ToList();

            var n = 0;
            var repo = Sibling(kit).useRepo<HookOutboxEntity, int>();
            foreach (var row in candidates)
            {
                if (!row.Id.HasValue)
                    continue;
                repo.DeleteById(row.Id.Value);
                n++;
            }

            return n;
        }

        private bool ResetDeadIfDead(SQLBuilder kit, int id)
        {
            var repo = Sibling(kit).useRepo<HookOutboxEntity>();
            var row = repo.GetById(id);
            if (row == null || row.Status != HookOutboxStatus.Dead)
                return false;

            row.Status = HookOutboxStatus.Pending;
            row.Attempt = 0;
            row.NextRetryAt = null;
            row.LastError = null;
            row.UpdatedAt = DateTime.UtcNow;
            repo.Update(row);
            return true;
        }

        private bool TryClaimOne(SQLBuilder kit, int id, int leaseSeconds, out HookOutboxEntity row)
        {
            row = null;
            var now = DateTime.UtcNow;
            var leasedUntil = now.AddSeconds(Math.Max(1, leaseSeconds));
            var clip = Sibling(kit).useClip();
            clip.setTable<HookOutboxEntity>(out var o);
            clip.set(() => o.Status, HookOutboxStatus.Processing);
            clip.set(() => o.UpdatedAt, now);
            clip.set(() => o.LeasedUntil, leasedUntil);
            clip.where(() => o.Id, id);
            clip.where(() => o.Status, HookOutboxStatus.Pending);
            if (clip.doUpdate() <= 0)
                return false;

            row = Sibling(kit).useRepo<HookOutboxEntity>().GetById(id);
            return row != null && row.Status == HookOutboxStatus.Processing;
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
