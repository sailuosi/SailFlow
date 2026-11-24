using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using sailflow.data;
using sailflow.hub.Localize;
using sailflow.hub.Resource;
using sailflow.engine.Assignee;
using sailflow.engine.Common;
using sailflow.hooks;
using sailflow.engine.Xpdl;
using sailflow.engine.Result;
using sailflow.engine.Client;
using sailflow.engine.Logging;
using sailflow.run;
using sailflow.engine.Factories;
using sailflow.WebUtility;

namespace sailflow.data;

/// <summary>契约 · HookOutboxManager（实现位于 sailflow.runtime）。</summary>
public interface IHookOutboxManager
{
    void InsertPending(SQLBuilder kit, HookOutboxEntity entity);

    IList<HookOutboxEntity> GetPending(SQLBuilder kit, int limit = 50);

    IList<HookOutboxEntity> GetPending(SQLBuilder kit, int limit, string kind);

    IList<HookOutboxEntity> TryClaimPending( SQLBuilder kit, int limit, int maxAttempts = HookOutboxOptions.DefaultMaxAttempts, int leaseSeconds = HookOutboxOptions.DefaultLeaseSeconds);

    IList<HookOutboxEntity> TryClaimPending( SQLBuilder kit, int limit, int maxAttempts, int leaseSeconds, string kind);

    int ReclaimStaleProcessing(SQLBuilder kit, DateTime staleBeforeUtc, DateTime? nowUtc = null);

    void ResetProcessing(SQLBuilder kit, string id);

    void MarkSucceeded(SQLBuilder kit, HookOutboxEntity row);

    void MarkSkipped(SQLBuilder kit, HookOutboxEntity row, string note = null);

    int BindAssigneeAutoApproveActivityInstance( SQLBuilder kit, string processInstanceId, string activityId, string activityInstanceId);

    void Postpone(SQLBuilder kit, HookOutboxEntity row, TimeSpan delay, string note = null);

    void MarkFailed(SQLBuilder kit, HookOutboxEntity row, string error, int maxAttempts = HookOutboxOptions.DefaultMaxAttempts);

    void ResetDead(SQLBuilder kit, string id);

    IList<HookOutboxEntity> QueryByStatus(SQLBuilder kit, string status, int limit = 100);

    IList<HookOutboxEntity> QueryDead(SQLBuilder kit, int limit = 100);

    IList<HookOutboxEntity> QueryDead( SQLBuilder kit, int limit, string processCode, string handlerName);

    int CountByStatus(SQLBuilder kit, string status);

    int ResetDeadBatch(SQLBuilder kit, IEnumerable<string> ids);

    int ResetAllDead(SQLBuilder kit, int limit = 500);

    int PurgeExpiredTerminal( SQLBuilder kit, int succeededRetentionDays, int deadRetentionDays, int batchSize = HookOutboxOptions.DefaultPurgeBatchSize, DateTime? nowUtc = null);
}
