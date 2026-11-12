using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using sailflow.data;
using sailflow.engine.Entity;
using sailflow.engine.Client;
using sailflow.engine.Hooks;

namespace sailflow.engine.Manager
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
            kit.useRepo<HookOutboxEntity>().Insert(entity);
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
            var take = Math.Max(1, limit);
            var clip = kit.useClip();
            clip.from<HookOutboxEntity>(out var o)
                .where(() => o.Status, HookOutboxStatus.Pending)
                .where(() => o.NextRetryAt == null || o.NextRetryAt <= now);
            if (!string.IsNullOrEmpty(kind))
                clip.where(() => o.Kind, kind);
            else
                clip.where(() => o.Kind != HookOutboxKinds.AssigneeAutoApprove);
            clip.orderBy(() => o.CreatedAt);
            return clip.select(o).take(take).queryList()?.ToList()
                   ?? new List<HookOutboxEntity>();
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

                if (string.IsNullOrWhiteSpace(candidate.HookOutboxId))
                    continue;

                if (TryClaimOne(kit, candidate.HookOutboxId, leaseSeconds, out var row))
                    claimed.Add(row);
            }

            return claimed;
        }

        /// <summary>
        /// 回收僵尸 Processing：lease 过期，或无 lease 且 UpdatedAt 早于 staleBeforeUtc。
        /// SQL 侧先收窄候选，再逐行 CAS 回 Pending。
        /// </summary>
        public int ReclaimStaleProcessing(SQLBuilder kit, DateTime staleBeforeUtc, DateTime? nowUtc = null)
        {
            var now = nowUtc ?? DateTime.UtcNow;
            var clip = kit.useClip();
            clip.from<HookOutboxEntity>(out var o)
                .where(() => o.Status, HookOutboxStatus.Processing)
                .where(() =>
                (o.LeasedUntil != null && o.LeasedUntil < now)
                || (o.LeasedUntil == null && o.UpdatedAt != null && o.UpdatedAt < staleBeforeUtc))
                .orderBy(() => o.CreatedAt);
            // 单次回收上限，避免极端积压时一次事务过大
            var rows = clip.select(o).take(200).queryList() ?? new List<HookOutboxEntity>();
            var reclaimed = 0;

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.HookOutboxId))
                    continue;

                var update = kit.useClip();
                update.setTable<HookOutboxEntity>(out var u)
                    .set(() => u.Status, HookOutboxStatus.Pending)
                    .setToNull(() => u.LeasedUntil)
                    .set(() => u.UpdatedAt, now)
                    .where(() => u.HookOutboxId, row.HookOutboxId)
                    .where(() => u.Status, HookOutboxStatus.Processing);
                if (update.doUpdate() > 0)
                    reclaimed++;
            }

            return reclaimed;
        }

        public void ResetProcessing(SQLBuilder kit, string id)
        {
            var repo = kit.useRepo<HookOutboxEntity>();
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
            kit.useRepo<HookOutboxEntity>().Update(row);
        }

        /// <summary>
        /// 策略性终态跳过（如下一跳需人工选人 / 实例已非运行）：不进 Failed 重试梯。
        /// </summary>
        public void MarkSkipped(SQLBuilder kit, HookOutboxEntity row, string note = null)
        {
            row.Status = HookOutboxStatus.Skipped;
            row.LeasedUntil = null;
            row.NextRetryAt = null;
            row.UpdatedAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(note))
                row.LastError = Truncate(note, 2000);
            kit.useRepo<HookOutboxEntity>().Update(row);
        }

        /// <summary>
        /// 活动实例插入后回写零 Performer 锚点的 <see cref="HookOutboxEntity.ActivityInstanceId"/>，
        /// 避免 Job 在多 Ready 时误挑。Worker 优先读本列覆盖 Payload。
        /// </summary>
        public int BindAssigneeAutoApproveActivityInstance(
            SQLBuilder kit,
            string processInstanceId,
            string activityId,
            string activityInstanceId)
        {
            if (string.IsNullOrWhiteSpace(processInstanceId) || string.IsNullOrWhiteSpace(activityInstanceId) || string.IsNullOrWhiteSpace(activityId))
                return 0;

            var clip = kit.useClip();
            clip.from<HookOutboxEntity>(out var o)
                .where(() => o.Kind, HookOutboxKinds.AssigneeAutoApprove)
                .where(() => o.ProcessInstanceId, processInstanceId)
                .where(() => o.Status, HookOutboxStatus.Pending)
                .where(() => o.ActivityInstanceId == null)
                .orderBy(() => o.CreatedAt);
            var candidates = clip.select(o).take(50).queryList() ?? new List<HookOutboxEntity>();
            var n = 0;
            var repo = kit.useRepo<HookOutboxEntity>();
            foreach (var row in candidates)
            {
                if (!PayloadActivityIdMatches(row.PayloadJson, activityId))
                    continue;

                row.ActivityInstanceId = activityInstanceId;
                row.UpdatedAt = DateTime.UtcNow;
                repo.Update(row);
                n++;
            }

            return n;
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
            kit.useRepo<HookOutboxEntity>().Update(row);
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

            kit.useRepo<HookOutboxEntity>().Update(row);
        }

        public void ResetDead(SQLBuilder kit, string id)
        {
            var repo = kit.useRepo<HookOutboxEntity>();
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
            var take = Math.Max(1, limit);
            var clip = kit.useClip();
            clip.from<HookOutboxEntity>(out var o)
                .where(() => o.Status, status)
                .orderBy(() => o.CreatedAt);
            return clip.select(o).take(take).queryList()?.ToList()
                   ?? new List<HookOutboxEntity>();
        }

        public IList<HookOutboxEntity> QueryDead(SQLBuilder kit, int limit = 100) =>
            QueryDead(kit, limit, processCode: null, handlerName: null);

        public IList<HookOutboxEntity> QueryDead(
            SQLBuilder kit,
            int limit,
            string processCode,
            string handlerName)
        {
            var take = Math.Max(1, limit);
            var clip = kit.useClip();
            clip.from<HookOutboxEntity>(out var o)
                .where(() => o.Status, HookOutboxStatus.Dead);
            if (!string.IsNullOrEmpty(processCode))
                clip.where(() => o.ProcessCode, processCode);
            if (!string.IsNullOrEmpty(handlerName))
                clip.where(() => o.HandlerName, handlerName);
            clip.orderBy(() => o.CreatedAt);
            return clip.select(o).take(take).queryList()?.ToList()
                   ?? new List<HookOutboxEntity>();
        }

        public int CountByStatus(SQLBuilder kit, string status)
        {
            var clip = kit.useClip();
            clip.from<HookOutboxEntity>(out var o)
                .where(() => o.Status, status);
            return clip.select(o).count();
        }

        public int ResetDeadBatch(SQLBuilder kit, IEnumerable<string> ids)
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
                if (ResetDeadIfDead(kit, row.HookOutboxId))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// 按 TTL 删除终态行（Succeeded / Skipped / Dead）。以 <c>UpdatedAt</c>（空则 <c>CreatedAt</c>）与保留天数比较。
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
                if (deleted < limit)
                {
                    deleted += PurgeStatusOlderThan(
                        kit,
                        HookOutboxStatus.Skipped,
                        now.AddDays(-succeededRetentionDays),
                        limit - deleted);
                }
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

            var clip = kit.useClip();
            clip.from<HookOutboxEntity>(out var o)
                .where(() => o.Status, status);
            // UpdatedAt 优先；空则用 CreatedAt
            clip.where(() =>
                (o.UpdatedAt != null && o.UpdatedAt < olderThanUtc)
                || (o.UpdatedAt == null && o.CreatedAt != null && o.CreatedAt < olderThanUtc))
                .orderBy(() => o.CreatedAt);
            var candidates = clip.select(o).take(limit).queryList()
                             ?? new List<HookOutboxEntity>();

            var n = 0;
            var repo = kit.useRepo<HookOutboxEntity, string>();
            foreach (var row in candidates)
            {
                if (!!string.IsNullOrWhiteSpace(row.HookOutboxId))
                    continue;
                repo.DeleteById(row.HookOutboxId);
                n++;
            }

            return n;
        }

        private bool ResetDeadIfDead(SQLBuilder kit, string id)
        {
            var repo = kit.useRepo<HookOutboxEntity>();
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

        private bool TryClaimOne(SQLBuilder kit, string id, int leaseSeconds, out HookOutboxEntity row)
        {
            row = null;
            var now = DateTime.UtcNow;
            var leasedUntil = now.AddSeconds(Math.Max(1, leaseSeconds));
            var clip = kit.useClip();
            clip.setTable<HookOutboxEntity>(out var o)
                .set(() => o.Status, HookOutboxStatus.Processing)
                .set(() => o.UpdatedAt, now)
                .set(() => o.LeasedUntil, leasedUntil)
                .where(() => o.HookOutboxId, id)
                .where(() => o.Status, HookOutboxStatus.Pending);
            if (clip.doUpdate() <= 0)
                return false;

            row = kit.findRowById<HookOutboxEntity>(id);
            return row != null && row.Status == HookOutboxStatus.Processing;
        }
        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= max)
                return value;
            return value.Substring(0, max);
        }

        static bool PayloadActivityIdMatches(string payloadJson, string activityId)
        {
            if (string.IsNullOrWhiteSpace(payloadJson) || string.IsNullOrWhiteSpace(activityId))
                return false;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(payloadJson);
                if (!doc.RootElement.TryGetProperty("activityId", out var el)
                    && !doc.RootElement.TryGetProperty("ActivityId", out el))
                    return false;
                var id = el.GetString();
                return string.Equals(id, activityId, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
