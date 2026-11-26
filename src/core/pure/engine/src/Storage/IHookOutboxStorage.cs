using System;
using mooSQL.data;
using sailflow.data;

namespace sailflow.storage
{
    /// <summary>
    /// Hook Outbox 表纯库交互（kit 首参、无状态）。
    /// 状态机策略（Attempt/退避/跳过原因）留 Manager；本层只做列写与 CAS。
    /// </summary>
    public interface IHookOutboxStorage
    {
        void Insert(SQLBuilder kit, HookOutboxEntity entity);

        HookOutboxEntity GetById(SQLBuilder kit, string id);

        /// <summary>整行 Update（Manager 已填好 Status/Attempt/Lease 等列）。</summary>
        void Update(SQLBuilder kit, HookOutboxEntity entity);

        /// <summary>CAS Pending→Processing，并写 UpdatedAt/LeasedUntil；返回影响行数。</summary>
        int CasClaimPending(
            SQLBuilder kit,
            string id,
            DateTime updatedAtUtc,
            DateTime leasedUntilUtc);

        /// <summary>CAS Processing→Pending，清空 LeasedUntil；返回影响行数。</summary>
        int CasResetProcessingToPending(SQLBuilder kit, string id, DateTime updatedAtUtc);

        void DeleteById(SQLBuilder kit, string id);
    }
}
